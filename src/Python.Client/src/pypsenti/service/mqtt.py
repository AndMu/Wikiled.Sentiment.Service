import time
from ..service import logger


class Callbacks:

    def __init__(self):
        self.messages = []
        self.publisheds = []
        self.subscribeds = []
        self.unsubscribeds = []
        self.disconnecteds = []
        self.connecteds = []

    def __str__(self):
        return str(self.messages) + str(self.messagedicts) + str(self.publisheds) + \
            str(self.subscribeds) + \
            str(self.unsubscribeds) + str(self.disconnects)

    def clear(self):
        logger.debug('clear')
        self.__init__()

    def on_connect(self, client, userdata, flags, rc, properties=None):
        logger.debug('on_connect')
        self.connecteds.append({"userdata": userdata, "flags": flags,
                                "reasonCode": rc, "properties": properties})

    def wait(self, alist, timeout=2):
        interval = 0.2
        total = 0
        while len(alist) == 0 and total < timeout:
            time.sleep(interval)
            total += interval
        if len(alist) == 0:
            raise TimeoutError('Timeout')
        return alist.pop(0)  # if len(alist) > 0 else None

    def wait_connected(self):
        logger.debug('wait_connected')
        return self.wait(self.connecteds)

    def on_disconnect(self, client, userdata, rc=0):
        logger.debug('on_disconnect')
        self.disconnecteds.append(
            {"reasonCode": rc, "properties": userdata})

    def wait_disconnected(self):
        logger.debug('wait_disconnected')
        return self.wait(self.disconnecteds)

    def on_message(self, client, userdata, message):
        logger.debug('on_message')
        self.messages.append({"userdata": userdata, "message": message})

    def wait_messages(self):
        logger.debug('wait_messages')
        return self.wait(self.messages)

    def published(self, client, userdata, msgid):
        logger.debug('published')
        self.publisheds.append(msgid)

    def wait_published(self):
        logger.debug('wait_published')
        return self.wait(self.publisheds)

    def on_subscribe(self, client, userdata, mid, granted_qos, properties=None):
        logger.debug('on_subscribe')
        self.subscribeds.append({"mid": mid, "userdata": userdata,
                                 "properties": properties, "granted_qos": granted_qos})

    def wait_subscribed(self):
        logger.debug('wait_subscribed')
        return self.wait(self.subscribeds)

    def unsubscribed(self, client, userdata, mid, properties, reasonCodes):
        logger.debug('unsubscribed')
        self.unsubscribeds.append({"mid": mid, "userdata": userdata,
                                   "properties": properties, "reasonCodes": reasonCodes})

    def wait_unsubscribed(self):
        logger.debug('wait_unsubscribed')
        return self.wait(self.unsubscribeds)

    def register(self, client):
        client.on_connect = self.on_connect
        client.on_subscribe = self.on_subscribe
        client.on_publish = self.published
        client.on_unsubscribe = self.unsubscribed
        client.on_message = self.on_message
        client.on_disconnect = self.on_disconnect

