import json
import socket
from abc import ABC

import jsonpickle


class Message(ABC):

    def __init__(self):
        self.MessageType = self.__class__.__name__

    def get_json(self):
        return jsonpickle.encode(self)


class ConnectMessage(Message):

    def __init__(self, user):
        self.Hostname = socket.gethostname()
        self.UserAgent = 'pywiSenti'
        self.UserName = user
        super().__init__()


class SentimentMessage(Message):

    def __init__(self):
        self.Request = WorkRequest()
        super().__init__()


class WorkRequest(object):
    def __init__(self):
        self.Dictionary = {}
        self.Documents = []
        self.CleanText = True
        self.Domain = None
        self.Model = None


class SingleDocument(object):
    def __init__(self, text):
        self.Date = None
        self.Author = None
        self.Id = None
        self.Text = text
        self.IsPostivie = None
