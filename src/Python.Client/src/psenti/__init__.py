from .service.sentiment import SentimentConnection, SentimentAnalysis
from .service.request import Document
from .helpers.logging_helpers import LoggingFileHandler

__name__ = "psenti"

__version__ = '1.0.0'
version = __version__

__all__ = ['SentimentConnection', 'SentimentAnalysis', 'LoggingFileHandler', 'Document', 'SentimentAnalysis']
