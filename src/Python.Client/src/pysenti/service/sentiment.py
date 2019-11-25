import json
import time
import uuid

from paho.mqtt.client import Client

from ..helpers.utilities import batch
from requests import Session
from ..service import logger


class Document(object):

    def __init__(self, document_id: str, text: str):
        if document_id is None:
            document_id = uuid.uuid4()
        self.id = str(document_id)
        self.text = text
        self.author = None
        self.isPositive = None
        self.date = None

    def get_dict(self):
        return {
            "Id": self.id,
            "Text": self.text,
            "Author": self.author,
            "isPositive": self.isPositive,
            "date": self.date,
        }


class SentimentConnection(object):

    def __init__(self, client_id: str):
        if client_id is None or len(client_id) < 4:
            raise ValueError('Client id is too short. Minimum 4 symbols')

        self.client_id = client_id
        self.host = 'sentiment.wikiled.com'
        self.host = 'localhost:5000'
        self.batch_size = 200
        self.broker_url = "localhost"
        self.broker_port = 1883
        # 100 ms
        self.step = 0.01
        # 15 minutes
        self.train_timeout = 15 * 60 * (1 / self.step)
        # 30 seconds
        self.analysis_timeout = 30 * (1 / self.step)
        self._load()

    def _load(self):
        with Session() as session:
            url = f'http://{self.host}/api/sentiment/version'
            self.version = session.get(url).content
            url = f'http://{self.host}/api/sentiment/domains'
            self.supported_domains = json.loads(session.get(url).content)

    def save_documents(self, name: str, documents: Document):
        with Session() as session:
            url = f'http://{self.host}/api/documents/save'
            for documents_batch in batch(documents, self.batch_size):
                session.headers['Content-Type'] = 'application/json'
                request = {}
                request['User'] = self.client_id
                request['Name'] = name
                request['Documents'] = documents_batch
                result = session.post(url, data=json.dumps(request, default=vars, indent=4))
                if result.status_code != 200:
                    raise ConnectionError(result.reason)


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
        connection_result = self.client.connect(self.connection.broker_url, self.connection.broker_port)
        self.client.subscribe(self.sentiment_result_topic)
        self.client.subscribe(self.done_topic)
        self.client.subscribe(self.error_topic)
        self.client.subscribe(self.message_topic)
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

    def has_error(self):
        return self.error_topic in self.messages

    def is_done(self):
        return self.done_topic in self.messages

    def has_messages(self):
        return self.done_topic in self.messages


class SentimentAnalysis(object):

    def __init__(self, connection: SentimentConnection, domain: str = None, lexicon: dict = None, clean: bool = False,
                 model: str = None):
        if domain is not None and domain.lower() not in [x.lower() for x in connection.supported_domains]:
             raise ValueError("Not supported domain:" + domain)
        self.connection = connection
        self.domain = domain
        self.lexicon = lexicon
        self.clean = clean
        self.model = model

    def train(self, name):
        with SentimentStream(self.connection) as stream:
            request = {
                'name': name,
                'domain': self.domain,
                'CleanText': self.clean
            }

            stream.client.publish('Sentiment/Train', json.dumps(request, indent=2))
            waited = 0
            while not stream.has_error() and not stream.is_done():
                waited += 1
                if (waited >= stream.connection.train_timeout):
                    raise TimeoutError()
                time.sleep(1)
            if stream.has_error():
                raise ConnectionError(stream.messages[stream.error_topic][0][1])

    def detect_sentiment_text(self, documents: list):
        document_pack = [Document(None, item) for item in documents]
        return self.detect_sentiment(document_pack)

    def detect_sentiment(self, documents: list):
        index = 0
        processed_ids = {}
        with SentimentStream(self.connection) as stream:
            for document_batch in batch(documents, self.connection.batch_size):
                batch_request_documents = []
                for document in document_batch:
                    batch_request_documents.append(document.get_dict())
                    processed_ids[document.id] = index
                    index += 1
                yield from self._process_on_server(stream, batch_request_documents, processed_ids)
            if stream.has_error():
                raise ConnectionError(stream.messages[stream.error_topic][0][1])

    def _process_on_server(self, stream, batch_request_documents, processed_ids):
        document_request = self._create_batch(batch_request_documents)
        stream.client.publish('Sentiment/Analysis', document_request)
        waited = 0
        while len(processed_ids) > 0 and not stream.has_error():
            waited += 1
            if (waited >= stream.connection.analysis_timeout):
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
            elif stream.is_done():
                raise TimeoutError('Processing error')

            time.sleep(0.01)

    def _create_batch(self, documents):
        data = {}
        data['CleanText'] = self.clean
        if self.lexicon is not None:
            data['dictionary'] = self.lexicon
        if self.domain is not None:
            data['domain'] =self.domain
        data['documents'] = documents
        data['model'] = self.model
        return json.dumps(data, indent=2)



