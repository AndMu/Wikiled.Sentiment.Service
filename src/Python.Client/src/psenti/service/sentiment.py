import asyncio
import json

import websockets

from ..service.request import ConnectMessage, SentimentMessage, Document, TrainMessage
from ..helpers.utilities import batch, EventHandler
from requests import Session
from ..service import logger


class SentimentConnection(object):

    def __init__(self, host: str, port: int, client_id: str):
        if client_id is None or len(client_id) < 4:
            raise ValueError('Client id is too short. Minimum 4 symbols')

        self.client_id = client_id
        self.host = host
        self.host = f'{host}:{port}'
        self.stream_url = f'ws://{self.host}/stream'
        self.batch_size = 100
        self._load()

    def delete_documents(self, name: str):
        logger.info(f'Deleting document [{name}]...')
        with Session() as session:
            url = f'http://{self.host}/api/documents/delete/{self.client_id}/{name}'
            result = session.post(url)
            if result.status_code != 200:
                raise ConnectionError(result.reason)

    def save_documents(self, name: str, documents: Document):
        logger.info(f'Saving document [{name}]: {len(documents)}...')
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

    def _load(self):
        with Session() as session:
            url = f'http://{self.host}/api/sentiment/version'
            self.version = session.get(url).content
            url = f'http://{self.host}/api/sentiment/domains'
            self.supported_domains = json.loads(session.get(url).content)


class SentimentAnalysis(object):

    def __init__(self, connection: SentimentConnection, domain: str = None, lexicon: dict = None, clean: bool = False,
                 model: str = None, adjust_lexicon=False, extract_emotions=False):
        if domain is not None and domain.lower() not in [x.lower() for x in connection.supported_domains]:
             raise ValueError('Not supported domain:' + domain)
        self.connection = connection
        self.domain = domain
        self.lexicon = lexicon
        self.clean = clean
        self.model = model
        self.adjust_lexicon = adjust_lexicon
        self.extract_emotions = extract_emotions
        self.on_message = EventHandler()

    def train(self, name):
        asyncio.run(self.train_async(name))

    async def train_async(self, name):
        try:
            async with websockets.connect(self.connection.stream_url) as websocket:
                connect = ConnectMessage(self.connection.client_id).get_json()
                await websocket.send(connect)
                logger.info('Training Sentiment...')

                async for message in websocket:
                    logger.debug('Message Received')
                    message = json.loads(message, encoding='utf-8')
                    if message['MessageType'] == 'HeartbeatMessage':
                        logger.debug('Heartbeat!')
                    elif message['MessageType'] == 'ConnectedMessage':
                        logger.info('Connected!')
                        logger.info('Sending train request')
                        train_message = TrainMessage(name).get_json()
                        await websocket.send(train_message)
                    elif message['MessageType'] == 'CompletedMessage':
                        if message['IsError']:
                            raise ConnectionError(message['Message'])
                        break
            logger.info('Completed!')
        except Exception as e:
            logger.error(f'Failed: {str(e)}')

    def detect_sentiment_text(self, documents: list):
        document_pack = [Document(text=item) for item in documents]
        self.detect_sentiment(document_pack)

    def detect_sentiment(self, documents: list):
        asyncio.run(self.detect_sentiment_async(documents))

    async def detect_sentiment_async(self, documents: list):
        logger.info(f'Detecting sentiment in {len(documents)} documents; Domain [{self.domain}]; Cleaning '
                    f'[{self.clean}]; Model: [{self.model}] Lexicon: [{self.lexicon}]')
        index = 0
        processed_ids = {}
        try:
            async with websockets.connect(self.connection.stream_url) as websocket:
                connect = ConnectMessage(self.connection.client_id).get_json()
                await websocket.send(connect)
                connected = False
                for document_batch in batch(documents, self.connection.batch_size):
                    logger.debug('Processing batch...')
                    for document in document_batch:
                        processed_ids[document.Id] = index
                        index += 1
                    document_request = self._create_batch(document_batch).get_json()
                    if connected:
                        logger.info(f'Sending document batch: {len(document_batch)}')
                        await websocket.send(document_request)
                    async for message in websocket:
                        logger.debug('Message Received')
                        message = json.loads(message, encoding='utf-8')
                        if message['MessageType'] == 'HeartbeatMessage':
                            logger.debug('Heartbeat!')
                        elif message['MessageType'] == 'ConnectedMessage':
                            logger.info('Connected!')
                            connected = True
                            logger.info('Sending first document batch')
                            await websocket.send(document_request)
                        elif message['MessageType'] == 'DataUpdate':
                            logger.debug('Data Received')
                            for document in message['Data']:
                                document_id = document['Id']
                                del processed_ids[document_id]
                                self.on_message(document)
                            if len(processed_ids) == 0:
                                break
            logger.info('Completed!')
        except Exception as e:
            logger.exception(f'Processing failed')

    def _create_batch(self, documents):
        message = SentimentMessage()
        message.Request.CleanText = self.clean
        message.Request.AdjustDomain = self.adjust_lexicon
        message.Request.Emotions = self.extract_emotions
        if self.lexicon is not None:
            message.Request.Dictionary = self.lexicon
        if self.domain is not None:
            message.Request.Domain = self.domain
        message.Request.Documents = documents
        message.Request.Model = self.model
        return message




