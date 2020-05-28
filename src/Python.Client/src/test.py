import os
import socket
import logging.config
from datetime import datetime
from os import path

import asyncio

from src.psenti.service.sentiment import SentimentAnalysis, SentimentConnection, Document

# create logger
logging.config.fileConfig('logging.conf', disable_existing_loggers=False)
logger = logging.getLogger('psenti')

user_name = socket.gethostname()
host = '127.0.0.1'
port = 5000


def sentiment_analysis():
    documents = ['I like this bool :)', 'short it baby']
    dictionary = {}
    dictionary['like'] = -1
    dictionary['BOOL'] = 1

    connection = SentimentConnection(host=host, port=port, client_id=user_name)
    analysis = SentimentAnalysis(connection, 'market', dictionary, clean=True)
    analysis.on_message.subscribe(lambda message: print(message))
    analysis.detect_sentiment_text(documents)


def sentiment_analysis_market():
    documents = ['Huge loss reported.',
                 'Huge profit reported.']

    connection = SentimentConnection(host=host, port=port, client_id=user_name)
    analysis = SentimentAnalysis(connection, 'market', clean=True)
    analysis.on_message.subscribe(lambda message: print(message))
    analysis.detect_sentiment_text(documents)


def sentiment_analysis_docs():
    documents = [
        Document('1',
                 'I love this hello kitty decal! I like that the bow is pink instead of red. Only bad thing is that after putting it on the window theres a few air bubbles, but that most likely my fault. Shipped fast too.',
                 'Ben'),
        Document('2',
                 'I bought this for my 3 yr old daughter when I took it out the pack it had a bad oder, cute but very cheap material easy to ripe.  When I tried it on her it was to big, but of course she liked it so I kept it. I dressed her up in it and she looked cute.',
                 'Ben',
                 datetime(1995, 5, 2))
        ]

    # with custom lexicon and Twitter type cleaning
    # analysis = SentimentAnalysis(connection, 'market', dictionary, clean=True, model='Test')
    connection = SentimentConnection(host=host, port=port, client_id=user_name)
    analysis = SentimentAnalysis(connection)
    analysis.on_message.subscribe(lambda message: print(message))
    analysis.detect_sentiment(documents)


def read_documents(path_folder: str, class_type: bool):
    directory = os.fsencode(path_folder)
    all_documents = []
    for file in os.listdir(directory):
        filename = os.fsdecode(file)
        id = os.path.splitext(filename)[0]
        full_name = path.join(path_folder, filename)
        with open(full_name, "r", encoding='utf8') as reader:
            text = reader.read()
            doc = Document(id, text)
            doc.isPositive = class_type
            all_documents.append(doc)
    return all_documents


def save_documents():
    with SentimentConnection(host=host, port=port, client_id=user_name) as connection:
        connection.delete_documents('Test')
        print("Loading Negative files")
        all_documents = read_documents('D:/DataSets/aclImdb/All/Train/neg', False)
        print("Sending...")
        connection.save_documents('Test', all_documents)

        print("Loading Positive files")
        all_documents = read_documents('D:/DataSets/aclImdb/All/Train/pos', True)
        print("Sending...")
        connection.save_documents('Test', all_documents)


def train():
    with SentimentConnection(host=host, port=port, client_id=user_name) as connection:
        analysis = SentimentAnalysis(connection, domain='market', clean=True)
        analysis.train('Test')


if __name__ == "__main__":
    print('Test')
    sentiment_analysis_market()


