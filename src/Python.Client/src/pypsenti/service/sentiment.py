import json
import logging
import websockets
import uuid

from pypsenti.service.request import ConnectMessage, SentimentMessage, SingleDocument
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
        self.stream_url = f'ws://{self.host}/stream'
        self.batch_size = 200

        # # 100 ms
        # self.step = 0.01
        # # 15 minutes
        # self.train_timeout = 15 * 60 * (1 / self.step)
        # # 30 seconds
        # self.analysis_timeout = 30 * (1 / self.step)
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

    async def detect_sentiment_text(self, documents: list):
        document_pack = [Document(None, item) for item in documents]
        async for document in self.detect_sentiment(document_pack):
            yield document

    async def detect_sentiment(self, documents: list):
        index = 0
        processed_ids = {}
        async with websockets.connect(self.connection.stream_url) as websocket:
            connect = ConnectMessage(self.connection.client_id).get_json()
            await websocket.send(connect)
            for document_batch in batch(documents, self.connection.batch_size):
                document_request = self._create_batch(document_batch).get_json()
                async for message in websocket:
                    message = json.loads(message, encoding='utf-8')
                    if message['MessageType'] == 'HeartbeatMessage':
                        logger.debug('Heartbeat received!')
                    elif message['MessageType'] == 'ConnectedMessage':
                        await websocket.send(document_request)

                    print(message)
                    yield message
                    # await websocket.send(input())
        # with SentimentStream(self.connection) as stream:
        #     for document_batch in batch(documents, self.connection.batch_size):
        #         batch_request_documents = []
        #         for document in document_batch:
        #             batch_request_documents.append(document.get_dict())
        #             processed_ids[document.id] = index
        #             index += 1
        #         yield from self._process_on_server(stream, batch_request_documents, processed_ids)



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
        message = SentimentMessage()
        message.Request.CleanText = self.clean
        if self.lexicon is not None:
            message.Request.Dictionary = self.lexicon
        if self.domain is not None:
            message.Request.Domain = self.domain
        message.Request.Documents = [SingleDocument(document) for document in documents]
        message.Request.Mode = self.model
        return message


