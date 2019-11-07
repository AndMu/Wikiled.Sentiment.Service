import json
import time
import uuid
from requests import Session
from . import logger
from sklearn.preprocessing import OneHotEncoder

import paho.mqtt.client as mqtt


class SentimentConnection(object):

    def __init__(self, clientId):
        if clientId is None or len(clientId) < 10:
            raise ValueError('Client id is too short. Minimum 10 symbols')

        self.clientId = clientId
        self.host = 'sentiment.wikiled.com'
        # self.host = 'localhost:63804'

        broker_url = "localhost"
        broker_port = 1883

        self.messages = {}
        logger.info(f'Setting up connection to {self.host} for {clientId}')
        self.sentiment_result_topic = 'Sentiment/Result/' + clientId
        self.message_topic = 'Message/' + clientId
        self.error_topic = 'Error/' + clientId

        self.client = mqtt.Client(client_id=clientId)
        self.connection = self.client.connect(broker_url, broker_port)
        self.client.on_message = self._on_message
        self.client.on_disconnect = self._on_disconnect
        self.client.subscribe(self.sentiment_result_topic, qos=0)
        self.client.subscribe(self.error_topic, qos=0)
        self.client.subscribe(self.message_topic, qos=0)

        self.client.loop_start()
        self._load()


    def _load(self):
        with Session() as session:
            url = 'http://{}/api/sentiment/version'.format(self.host)
            self.version = session.get(url).content
            url = 'http://{}/api/sentiment/domains'.format(self.host)
            self.supported_domains = json.loads(session.get(url).content)

    def _on_disconnect(self, client, userdata, rc=0):
        logger.info("Disconnected result code " + str(rc))ord
        client.loop_stop()

    def _on_message(self, client, userdata, message):
        if message.topic not in self.messages:
            self.messages[message.topic] = []
        payload = str(message.payload.decode("utf-8"))
        logger.debug("message topic=", message.topic)
        logger.debug("message qos=", message.qos)
        logger.debug("message retain flag=", message.retain)
        self.messages[message.topic].append((message, payload))


class SentimentAnalysis(object):

    def __init__(self, connection, documents, domain, lexicon, clean):

        if domain is not None and domain.lower() not in [x.lower() for x in connection.supported_domains]:
             raise ValueError("Not supported domain:" + domain)
        self.connection = connection
        self.documents = documents
        self.domain = domain
        self.lexicon = lexicon
        self.clean = clean

    def __iter__(self):
        index = 0
        processed_ids = {}
        batch_request_documents = []
        for document in self.documents:
            id = str(uuid.uuid4())
            batch_request_documents.append({'text': document, 'id': id})
            processed_ids[id] = index
            index += 1
            if len(batch_request_documents) >= 200:
                yield from self._process_on_server(batch_request_documents, processed_ids)
                batch_request_documents = []
                print('Processed {}'.format(index))

        # Process outstanding documents
        if len(batch_request_documents) > 0:
            yield from self._process_on_server(batch_request_documents, processed_ids)

    def _process_on_server(self, batch_request_documents, processed_ids):
        document_request = self._create_batch(batch_request_documents)
        self.client.publish('Sentiment/Analysis', document_request)
        timeout = 30
        waited = 0
        while len(processed_ids) > 0:
            waited += 1
            if (waited > timeout):
                raise TimeoutError()
            if self.connection.sentiment_result_topic in self.connection.messages:
                waited = 0
                messages = self.connection.messages[self.connection.sentiment_result_topic]
                for message in messages:
                    document = json.loads(message.payload)
                    processed_ids.remove(document.id)
                    messages.remove(message)
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



if __name__ == "__main__":
    documents = ['I like this bool :)', 'short it baby']
    # with standard lexicon
    sentiment = SentimentAnalysis(documents)
    for result in sentiment:
        print(result)

    dictionary = {}
    dictionary['like'] = -1
    dictionary['BOOL'] = 1

    # with custom lexicon and Twitter type cleaning
    sentiment = SentimentAnalysis(documents, dictionary, domain='market', clean=True)
    for result in sentiment:
        print(result)

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