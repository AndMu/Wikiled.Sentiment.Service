import json
import logging
import time
import uuid

from paho.mqtt.client import Client, MQTTv311

from pypsenti.service.mqtt import Callbacks
from ..helpers.utilities import batch
from requests import Session
from ..service import logger

logger_on = False


def add_logger():
    global logger_on
    if logger_on:
        return

    logger_on = True
    # create logger
    logger.setLevel(logging.DEBUG)

    # create console handler and set level to debug
    ch = logging.StreamHandler()
    ch.setLevel(logging.DEBUG)

    # create formatter
    formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')

    # add formatter to ch
    ch.setFormatter(formatter)
    logger.addHandler(ch)


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
            'Id': self.id,
            'Text': self.text,
            'Author': self.author,
            'isPositive': self.isPositive,
            'date': self.date,
        }


class SentimentConnection(object):

    def __init__(self, host: str, port: int, client_id: str):
        if client_id is None or len(client_id) < 4:
            raise ValueError('Client id is too short. Minimum 4 symbols')

        self.client_id = client_id
        self.host = host
        self.host = f'{host}:{port}'
        self.batch_size = 200
        self.broker_url = host
        self.broker_port = port
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
        global logger_on
        self.connection = connection
        self.messages = {}
        logger.info(f'Setting up connection to {self.connection.host} for {self.connection.client_id}')
        self.sentiment_result_topic = f'Sentiment/Result/{self.connection.client_id}'
        self.message_topic = f'Message/{self.connection.client_id}'
        self.error_topic = f'Error/{self.connection.client_id}'
        self.done_topic = f'Sentiment/Done/{self.connection.client_id}'
        self.done_flag = False

        self.client = Client(client_id=self.connection.client_id, protocol=MQTTv311, transport='websockets')
        self.callback = Callbacks()
        self.callback.register(self.client)
        if logger_on:
            self.client.enable_logger(logger)

    def __enter__(self):
        logger.debug('Opening Connection...')
        self.client.connect(self.connection.broker_url, self.connection.broker_port)
        self.client.loop_start()
        self.callback.wait_connected()
        self._subscribe(self.sentiment_result_topic)
        self._subscribe(self.done_topic)
        self._subscribe(self.error_topic)
        self._subscribe(self.message_topic)
        logger.debug(f'Ready!!!')
        return self

    def __exit__(self, type, value, traceback):
        logger.debug('Closing Connection...')
        self.client.disconnect()
        self.client.loop_stop()
        return isinstance(value, TypeError)

    def publish(self, topic, request):
        self.client.publish(topic, json.dumps(request, indent=2), qos=1)
        self.callback.wait_published()

    def wait_message(self):
        logger.debug('wait_message')
        while True:
            message = self._process_message(self.callback.wait_messages())
            if message != None:
                return message

    def _subscribe(self, topic, qos_level=1):
        logger.debug("subscribing to topic" + topic)
        sub_rc = self.client.subscribe(topic, int(qos_level))
        logger.debug("subscribe returned " + str(sub_rc))
        self.callback.wait_subscribed()

    def _process_message(self, message):
        message = message['message']
        logger.debug(f'message topic={message.topic} qos={message.qos} retain flag={message.retain}')
        payload = str(message.payload.decode('utf-8-sig'))
        if message.topic == self.error_topic:
            raise ConnectionError(payload)
        elif message.topic == self.done_topic:
            logger.info('Received Done!')
            self.done_flag = True
        elif message.topic == self.message_topic:
            logger.info(payload)
        elif message.topic == self.sentiment_result_topic:
            return payload
        else:
            raise ConnectionError('Unknown message: ' + message.topic)

        return None


class SentimentAnalysis(object):

    def __init__(self, connection: SentimentConnection, domain: str = None, lexicon: dict = None, clean: bool = False,
                 model: str = None):
        if domain is not None and domain.lower() not in [x.lower() for x in connection.supported_domains]:
             raise ValueError('Not supported domain:' + domain)
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

            logger.info('Sending Train Command...')
            stream.publish('Sentiment/Train', request)
            stream.wait_message()

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

    def _process_on_server(self, stream, batch_request_documents, processed_ids):
        document_request = self._create_batch(batch_request_documents)
        logger.info('Sending Analysis Command...')
        stream.publish('Sentiment/Analysis', document_request)
        logger.debug('Processing...')
        while len(processed_ids) > 0:
            message = stream.wait_message()
            if message is not None:
                documents = json.loads(message, encoding='utf-8')
                for document in documents:
                    document_id = document['Id']
                    del processed_ids[document_id]
                    yield document

    def _create_batch(self, documents):
        data = {}
        data['CleanText'] = self.clean
        if self.lexicon is not None:
            data['dictionary'] = self.lexicon
        if self.domain is not None:
            data['domain'] =self.domain
        data['documents'] = documents
        data['model'] = self.model
        return data


