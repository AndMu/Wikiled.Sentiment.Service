import json
import os
import time
import uuid
from os import path

from paho.mqtt.client import Client

import pysenti.helpers.utilities as ut
from requests import Session
from pysenti.service import logger


class Document(object):

    def __init__(self, document_id: str, text: str):
        self.id = document_id
        self.text = text
        self.author = None
        self.isPositive = None
        self.date = None


class SentimentConnection(object):

    def __init__(self, client_id: str):
        if client_id is None or len(client_id) < 10:
            raise ValueError('Client id is too short. Minimum 10 symbols')

        self.client_id = client_id
        self.host = 'sentiment.wikiled.com'
        self.host = 'localhost:5000'
        self.batch_size = 200
        self.broker_url = "localhost"
        self.broker_port = 1883
        self._load();

    def _load(self):
        with Session() as session:
            url = f'http://{self.host}/api/sentiment/version'
            self.version = session.get(url).content
            url = f'http://{self.host}/api/sentiment/domains'
            self.supported_domains = json.loads(session.get(url).content)

    def save_documents(self, name: str, documents: Document):
        with Session() as session:
            url = f'http://{self.host}/api/documents/save'
            for documents_batch in ut.batch(documents, self.batch_size):
                session.headers['Content-Type'] = 'application/json'
                request = {}
                request['User'] = self.client_id
                request['Name'] = name
                request['Documents'] = documents_batch
                result = session.post(url, data=json.dumps(request, default=vars, indent=4))
                if result.status_code != 200:
                    raise ConnectionError()


class SentimentStream(object):

    def __init__(self, connection: SentimentConnection):
        self.connection = connection
        self.messages = {}
        logger.info(f'Setting up connection to {self.connection.host} for {self.connection.client_id}')
        self.sentiment_result_topic = f'Sentiment/Result/{self.connection.client_id}'
        self.message_topic = f'Message/{self.connection.client_id}'
        self.error_topic = f'Error/{self.connection.client_id}'
        self.done_topic = f'Sentiment/Done/{self.connection.client_id}'

        self.client = Client(client_id=self.connection.client_id)
        self.client.on_message = self._on_message
        self.client.on_disconnect = self._on_disconnect

    def __enter__(self):
        self.connection = self.client.connect(self.connection.broker_url, self.connection.broker_port)
        self.client.subscribe(self.sentiment_result_topic)
        self.client.subscribe(self.error_topic, qos=0)
        self.client.subscribe(self.message_topic, qos=0)
        self.client.loop_start()
        return self

    def __exit__(self, type, value, traceback):
        self.client.loop_stop()
        self.client.disconnect()
        return isinstance(value, TypeError)

    def _on_disconnect(self, client, userdata, rc=0):
        logger.info("Disconnected result code " + str(rc))
        client.loop_stop()

    def _on_message(self, client, userdata, message):
        if message.topic not in self.messages:
            self.messages[message.topic] = []
        payload = str(message.payload.decode("utf-8"))
        logger.debug("message topic=", message.topic)
        logger.debug("message qos=", message.qos)
        logger.debug("message retain flag=", message.retain)
        self.messages[message.topic].append((message, payload))
        if message.topic == self.error_topic:
            logger.error(payload)
        elif message.topic == self.message_topic:
            logger.info(payload)


class SentimentAnalysis(object):

    def __init__(self, connection: SentimentConnection, documents: Document = None, domain: str = None,
                 lexicon: dict = None, clean: bool = False, model: str = None):
        if domain is not None and domain.lower() not in [x.lower() for x in connection.supported_domains]:
             raise ValueError("Not supported domain:" + domain)
        self.connection = connection
        self.documents = documents
        self.domain = domain
        self.lexicon = lexicon
        self.clean = clean
        self.model = model

    def train(self, name):
        with SentimentStream(self.connection) as stream:
            request = {}
            request['name'] = name
            request['domain'] = self.domain
            request['model'] = self.model
            request['CleanText'] = self.clean
            stream.client.publish('Sentiment/Train', json.dumps(request, indent=2))
            # wait 15 minutes
            timeout = 15 * 60
            waited = 0
            while stream.error_topic not in stream.messages and stream.done_topic not in stream.messages:
                waited += 1
                if (waited > timeout):
                    raise TimeoutError()
                time.sleep(1)
            if stream.error_topic in stream.messages:
                raise ConnectionError(stream.messages[stream.error_topic][0].payload)

    def __iter__(self):
        index = 0
        processed_ids = {}
        with SentimentStream(self.connection) as stream:
            for document_batch in ut.batch(self.documents, self.connection.batch_size):
                batch_request_documents = []
                for document in document_batch:
                    if document.id is None:
                        document.id = str(uuid.uuid4())
                    batch_request_documents.append(document)
                    processed_ids[id] = index
                    index += 1
                yield from self._process_on_server(stream, batch_request_documents, processed_ids)

    def _process_on_server(self, stream, batch_request_documents, processed_ids):
        document_request = self._create_batch(batch_request_documents)
        stream.client.publish('Sentiment/Analysis', document_request)
        timeout = 30
        waited = 0
        while len(processed_ids) > 0:
            waited += 1
            if (waited > timeout):
                raise TimeoutError()
            if stream.sentiment_result_topic in stream.messages:
                waited = 0
                messages = stream.messages[stream.sentiment_result_topic]
                for message in messages:
                    documents = json.loads(message[0].payload)
                    messages.remove(message)
                    for document in documents:
                        id = document["Id"]
                        del processed_ids[id]
                        yield document

            time.sleep(1)

    def _create_batch(self, documents):
        data = {}
        data['CleanText'] = self.clean
        if self.lexicon is not None:
            data['dictionary'] = self.lexicon
        if self.domain is not None:
            data['domain'] =self. domain
        data['documents'] = documents
        return json.dumps(data, indent=2)


def sentiment_analysis():
    documents = ['I like this bool :)', 'short it baby']
    dictionary = {}
    dictionary['like'] = -1
    dictionary['BOOL'] = 1

    # with custom lexicon and Twitter type cleaning
    analysis = SentimentAnalysis(SentimentConnection('TestConnection17'), documents, 'market', dictionary, clean=True)
    for result in analysis:
        print(result)


def read_documents(path_folder: str, class_type: bool):
    directory = os.fsencode(path_folder)
    all_documents = []
    for file in os.listdir(directory):
        filename = os.fsdecode(file)
        id = os.path.splitext(filename)[0]
        full_name = path.join(path_folder, filename)
        with open(full_name, "r", encoding='utf8') as reader:
            text = reader.read()
            doc = Document(id, text)
            doc.isPositive = class_type
            all_documents.append(doc)
    return all_documents


def save_documents():
    connection = SentimentConnection('TestConnection17')
    all_documents = read_documents('E:/DataSets/aclImdb/All/Train/neg', False)
    connection.save_documents('Test', all_documents)

    all_documents = read_documents('E:/DataSets/aclImdb/All/Train/pos', True)
    connection.save_documents('Test', all_documents)


def train():
    analysis = SentimentAnalysis(SentimentConnection('TestConnection17'), domain='market', clean=True)
    analysis.train('Test')


if __name__ == "__main__":
    # save_documents()
    train()


    # def process_sentiment(self, documents, domain, lexicon, clean):
    #
    #
    #     index = 0
    #     processed_ids = {}
    #     batch_request_documents = []
    #     for document in self.documents:
    #         id = str(uuid.uuid4())
    #         batch_request_documents.append({'text': document, 'id': id})
    #         processed_ids[id] = index
    #         index += 1
    #         if len(batch_request_documents) >= 200:
    #             self.client.publish('Sentiment/Analysis', json.dumps(save_batch, indent=2))
    #             batch_request_documents = []
    #             print('Processed {}'.format(index))
    #
    #
    #     for document in self.documents:
    #         id = str(uuid.uuid4())
    #         batch_request_documents.append({'text': document, 'id': id})
    #         processed_ids[id] = index
    #
    #     # save_batch = {}
    #     # save_batch['documents'] = batch_request_documents
    #     # save_batch['Name'] = 'Test'

    # with Session() as session:
    #     url = 'http://{}/api/sentiment/parsestream'.format(self.host)
    #     data['documents'] = batch_request_documents
    #     json_object = json.dumps(data, indent=2)
    #     headers = {'Content-type': 'application/json', 'Accept': 'text/plain'}
    #     with session.post(url, json_object, headers=headers, stream=True) as r:
    #         for line in r.iter_lines():
    #             # filter out keep-alive new lines
    #             if line:
    #                 decoded_line = line.decode('utf-8')
    #                 sentimen_result = json.loads(decoded_line)
    #                 sentiment_class = 0
    #                 if sentimen_result['Stars'] is not None:
    #                     if sentimen_result['Stars'] > 3:
    #                         sentiment_class = 1
    #                     else:
    #                         sentiment_class = -1
    #                 id = processed_ids[sentimen_result['Id']]
    #                 yield (id, sentiment_class, sentimen_result)


