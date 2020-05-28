import abc
import json
import logging
import queue
import random
from concurrent.futures.thread import ThreadPoolExecutor

import websocket
import threading
from typing import Type

from bpsenti.refinitiv.pipeline import PipelineStream

from .rooms import RefiRooms
from .security import TokenManagement
from ..helpers.utilities import EventHandler

LOG = logging.getLogger(__name__)


class MessageProcessor(abc.ABC):

    def __init__(self):
        self._queue = queue.Queue()
        self.running = True
        self.on_message = EventHandler()
        self.queue_changed = EventHandler()

    @property
    def queue_size(self):
        return self._queue.qsize()

    def stop(self):
        # None in Queue stops processing
        self.running = False
        self._queue.put(None)

    def get_items(self, block=True, timeout=None):
        while self.running:
            item = self._queue.get(block=block, timeout=timeout)
            self._on_changed()
            if item is None:
                pass

            yield item

    def process_message(self, message):
        try:
            result = self._process_message(message)
            if result is not None:
                self._queue.put(result)
                self._on_message(result)
                self._on_changed()
        except Exception as error:
            LOG.error('Process message error :', error)

    def _on_changed(self):
        try:
            self.queue_changed()
        except:
            LOG.error('On Changed error')

    def _on_message(self, messages):
        for message in messages:
            try:
                self.on_message(message)
            except:
                LOG.error('On Message error')

    @abc.abstractmethod
    def _process_message(self, message):
        pass


class RefiMessageProcessor(MessageProcessor):

    def __init__(self, rooms: RefiRooms, pipeline: PipelineStream):
        self.rooms = rooms
        self._pipeline = pipeline
        super(RefiMessageProcessor, self).__init__()

    def _process_message(self, message):

        message_json = json.loads(message)
        LOG.debug(json.dumps(message_json, sort_keys=True, indent=2, separators=(',', ':')))

        message_event = message_json['event']
        LOG.debug(f'Process Message: [{message_event}]')
        if message_event == 'chatroomPost':
            incoming_msg = message_json['post']['message']
            chatroom_name = self.rooms.get_room_name(message_json['post']['chatroomId'])
            sender_email = message_json['post']['sender']['email']
            LOG.debug(f'Receive chatroom message: {incoming_msg}')
            return list(self._pipeline.process((chatroom_name, sender_email, incoming_msg)))
        elif message_event == 'message':
            incoming_msg = message_json['message']['message']
            chatroom_name = 'Message'
            sender_email = message_json['message']['sender']['email']
            LOG.debug(f'Receive text message: {incoming_msg}')
            return list(self._pipeline.process((chatroom_name, sender_email, incoming_msg)))
        return None


class PingManager:

    def __init__(self, token_manager: TokenManagement):
        self.token_manager = token_manager

    def generate_handshake(self) -> str:
        req_id = str(random.randint(0, 1000000))

        connect_request_msg = {
            'reqId': str(random.randint(0, 1000000)),
            'command': 'connect',
            'payload': {
                'stsToken': self.token_manager.access_token
            }
        }

        return req_id, json.dumps(connect_request_msg)

    def generate_ping(self):
        self.token_manager.reset_token()
        req_id = str(random.randint(0, 1000000))
        # create connection request message in JSON format
        connect_request_msg = {
            'reqId': req_id,
            'command': 'authenticate',
            'payload': {
                'stsToken': self.token_manager.access_token
            }
        }

        return req_id, json.dumps(connect_request_msg)


class WebsocketThread(threading.Thread):

    id_counter = 0

    def __init__(self, url: str, processor: MessageProcessor):
        super(WebsocketThread, self).__init__()
        WebsocketThread.id_counter += 1
        self.Id = WebsocketThread.id_counter
        self.url = url
        self.processor = processor
        self.ws = None
        self.open = False
        self.has_error = False

        self.on_open = EventHandler()
        self.on_open.is_safe = True

        self.on_close = EventHandler()
        self.on_close.is_safe = True

        self.on_error = EventHandler()
        self.on_error.is_safe = True

        self.open_event = threading.Event()
        self.open_event.is_safe = True

        self.name = f'Socket {self.Id}'
        self.executor = ThreadPoolExecutor(max_workers=1)
        self.is_running = False
        LOG.debug(f'Constructing websocket (id: {self.Id})')

    def run(self):
        LOG.debug('run')
        self.is_running = True
        self.ws = websocket.WebSocketApp(
            self.url,
            on_open=lambda ws: self._on_open(),
            on_close=lambda ws: self._on_close(),
            on_error=lambda ws, msg: self._on_error(msg),
            on_message=lambda ws, msg: self._on_message(msg))
        self.ws.run_forever()
        LOG.info('Socket Connection Stopped...')
        self.is_running = False

    def stop(self):
        self.open_event.set()
        self.is_running = False
        if self.ws is not None:
            self.ws.keep_running = False
        self.executor.shutdown()
        del self.on_close
        del self.on_error

    def send(self, msg):
        if self.has_error or not self.is_running:
            raise ConnectionError('Connection failed')

        self.ws.send(msg)
        LOG.debug('Sent: %s' % (json.dumps(msg, sort_keys=True, indent=2, separators=(',', ':'))))

    def _on_open(self):
        if not self.is_running:
            LOG.debug(f'_on_open websocket (id: {self.Id}) closed')
            return

        LOG.debug(f'websocket (id: {self.Id}) connection open')
        self.open = True
        self.has_error = False
        self.open_event.set()
        self.executor.submit(self.on_open)

    def _on_message(self, msg):
        if not self.is_running:
            LOG.debug(f'_on_message websocket (id: {self.Id}) closed')
            return

        LOG.debug(f'websocket (id: {self.Id}) received message: {msg}')
        if self.processor is not None:
            self.executor.submit(self.processor.process_message, msg)

    def _on_error(self, error):
        if not self.is_running:
            LOG.debug(f'_on_error websocket (id: {self.Id}) closed')
            return

        LOG.error(f'websocket (id: {self.Id}) connection err: {error}')
        self.has_error = True
        self.executor.submit(self.on_error, error)

    def _on_close(self):
        if not self.is_running:
            LOG.debug(f'_on_close websocket (id: {self.Id}) closed')
            return

        LOG.debug(f'websocket (id: {self.Id}) connection closed')
        self.open = False
        self.executor.submit(self.on_close)


class MonitoringThread(threading.Thread):

    def __init__(self, url: str, socket_type: Type[WebsocketThread], processor: MessageProcessor,
                 ping_manager: PingManager):
        super(MonitoringThread, self).__init__()
        self.url = url
        self.name = 'Monitoring'
        self.socket_type = socket_type
        self.socket: WebsocketThread = None
        self.stopping = False
        self.processor = processor
        self.ping_manager = ping_manager
        self._sync_event = threading.Event()
        self.on_open = EventHandler()
        self.reconnection_timer = 20

    def stop(self):
        LOG.info(f'Stop Monitoring')
        self.stopping = True
        if self.socket is not None:
            self.socket.stop()
        self._sync_event.set()

    def run(self):
        LOG.info(f'Start Monitoring')

        while not self.stopping:
            try:
                if self.socket is None:
                    self._create_socket()
                self._ping()
            except:
                LOG.exception('Connection error')
                self.ping_manager.token_manager.reset_token()
                self._sync_event.clear()
                if self.socket is not None:
                    self.socket.stop()
                    self.socket = None
                if not self.stopping:
                    LOG.debug(f'Waiting to reconnect - {self.reconnection_timer}s')
                    self._sync_event.wait(self.reconnection_timer)
                    self.reconnection_timer += 5

        LOG.info('Monitoring Stopped...')

    def _create_socket(self):
        LOG.info('Establishing Socket connection...')
        if self.socket is not None:
            LOG.debug('Active socket found - closing...')
            self.socket.stop()

        token = self.ping_manager.token_manager.access_token
        if token is None:
            raise ConnectionError('Authentication failed')

        self.socket = self.socket_type(self.url, self.processor)
        self.socket.on_error.append(lambda error: self._sync_event.set())
        self.socket.on_open.append(self._on_open)
        self.socket.start()

        if self.socket.open_event.wait(10):
            LOG.debug('Open event received')
        else:
            LOG.debug('Monitoring Timeout')

    def _on_open(self):
        LOG.debug('_on_open')
        try:
            req_id, msg = self.ping_manager.generate_handshake()
            self.socket.send(msg)
        except:
            LOG.exception('Handshake failed')

        self._sync_event.wait(5)
        if not self.socket.has_error:
            self.on_open()

    def _ping(self):

        LOG.debug('Monitoring...')
        timeout = 100
        if self.ping_manager.token_manager is not None and self.ping_manager.token_manager.expires_in > 60:
            timeout = self.ping_manager.token_manager.expires_in - 60

        if self._sync_event.wait(timeout):
            LOG.debug('Monitoring Triggered')
        else:
            LOG.debug('Monitoring Timeout')

        self._sync_event.clear()

        if self.stopping:
            LOG.debug('Interrupt monitoring - stopping')
            return

        if self.socket.has_error:
            raise ConnectionError('Socket has error')

        req_id, msg = self.ping_manager.generate_ping()
        self.socket.send(msg)
        LOG.debug('Sent ping!')
