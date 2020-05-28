import json
import unittest
from queue import Empty
from time import sleep
from unittest import mock
from unittest.mock import Mock, MagicMock

from bpsenti.refinitiv.rooms import RefiRooms
from bpsenti.refinitiv.security import TokenManagement
from bpsenti.refinitiv.sockets import RefiMessageProcessor, PingManager, MonitoringThread, WebsocketThread


class RefiMessageProcessorTest(unittest.TestCase):

    def setUp(self):
        self.room = MagicMock()
        self.pipeline = MagicMock()
        self.processor = RefiMessageProcessor(self.room, self.pipeline)
        self.msg = json.dumps({
            'event': "chatroomPost",
            'post':
                {
                    'chatroomId': "1",
                    'message': "Msg",
                    'sender':
                        {
                            'email': "mail"
                        }
                }
        })

        self.room.get_room_name.return_value = 'test'
        self.pipeline.process = self.processing
        self.total = 0

    def test_timeout(self):
        with self.assertRaises(Empty):
            list(self.processor.get_items(timeout=1))

    def test_stop(self):
        self.processor.stop()
        result = list(self.processor.get_items(timeout=1))
        self.assertEqual(0, len(result))

    def test_process_message(self):
        self.processor.process_message(self.msg)
        result = next(self.processor.get_items(1))
        self.assertEqual("test", result[0])
        self.assertEqual("test", result[1])

    def test_on_message(self):
        self.processor.on_message.subscribe(self.on_message)
        self.processor.process_message(self.msg)
        self.assertEqual(2, self.total)

    def test_on_message(self):
        self.processor.queue_changed.subscribe(self.queue_changed)
        self.processor.process_message(self.msg)
        self.assertEqual(1, self.total)

    def queue_changed(self):
        self.total = self.total + 1

    def on_message(self, message):
        self.total = self.total + 1

    def processing(self, argument):
        yield argument[0]
        yield argument[0]


class PingManagerTest(unittest.TestCase):

    def setUp(self):
        self.token_manager = MagicMock()
        type(self.token_manager).access_token = mock.PropertyMock(return_value='token')
        self.manager = PingManager(self.token_manager)

    def test_generate_ping(self):
        id, ping = self.manager.generate_ping()
        self.assertGreater(len(ping), 10)


class MonitoringThreadTest(unittest.TestCase):

    def setUp(self):
        self.processor = MagicMock()
        self.ping = MagicMock()
        self.ping.token_manager.expires_in = 60
        self.ping.generate_ping.return_value = (1, 'test')
        self.socket = MagicMock()
        self.socket.has_error = False
        self.thread = MonitoringThread('test', self.create, self.processor, self.ping)
        self.total = 0
        self.thread.start()

    def tearDown(self):
        self.thread.stop()

    def test_stop(self):
        self.thread.stop()
        self.thread.join(10)
        self.assertFalse(self.thread.is_alive())

    def test_ping(self):
        self.thread._sync_event.set()
        sleep(1)
        self.ping.token_manager.reset_token.assert_not_called()
        self.socket.send.assert_called_once()
        self.assertFalse(self.thread._sync_event.isSet())

    def test_ping_on_error(self):
        self.socket.has_error = True
        self.thread._sync_event.set()
        sleep(1)
        self.ping.token_manager.reset_token.assert_called_once()
        self.socket.send.assert_not_called()
        self.socket.stop.assert_called_once()
        self.assertFalse(self.thread._sync_event.isSet())

    def test_create(self):
        self.assertEqual(1, self.total)

    def test_send_error(self):
        self.socket.send.side_effect = Exception()
        self.thread._sync_event.set()
        self.thread.reconnection_timer = 1
        sleep(2)
        self.assertEqual(2, self.total)

    def create(self, str, proc):
        self.total += 1
        return self.socket


class WebsocketThreadTest(unittest.TestCase):

    def setUp(self):
        self.room = MagicMock()
        self.pipeline = MagicMock()
        self.manager = WebsocketThread('not_available', RefiMessageProcessor(self.room, self.pipeline))

    def test_error(self):

        total = {'value': 0}

        def test():
            total['value'] += 1

        self.manager.on_error.append(test())
        self.manager.start()
        sleep(1)
        self.assertEqual(1, total['value'])
        self.manager.stop()


