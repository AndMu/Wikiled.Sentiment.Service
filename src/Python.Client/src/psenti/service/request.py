import socket
import uuid
from abc import ABC

import jsonpickle
# from .. import version


class Message(ABC):

    def __init__(self):
        self.MessageType = self.__class__.__name__

    def get_json(self):
        return jsonpickle.encode(self, unpicklable=False, make_refs=False)


class ConnectMessage(Message):

    def __init__(self, user):
        self.Hostname = socket.gethostname()
        self.UserAgent = f'Python pSenti'
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


class Document(object):
    def __init__(self, document_id: str, text: str):
        if document_id is None:
            document_id = uuid.uuid4()
        self.Id = str(document_id)
        self.Date = None
        self.Author = None
        self.Text = text
        self.IsPositive = None


class TrainMessage(Message):
    def __init__(self, name: str):
        self.Name = name
        self.CleanText = True
        self.Domain = None
        super().__init__()

