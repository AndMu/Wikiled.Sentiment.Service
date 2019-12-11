import asyncio
import os
import socket
from os import path

from pypsenti import SentimentAnalysis, SentimentConnection, Document

import logging
# create logger
logger = logging.getLogger('pypsenti')
logger.setLevel(logging.DEBUG)

# create console handler and set level to debug
ch = logging.StreamHandler()
ch.setLevel(logging.DEBUG)

# create formatter
formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')

# add formatter to ch
ch.setFormatter(formatter)
logger.addHandler(ch)

user_name = socket.gethostname()
connection = SentimentConnection(host='localhost', port=5000, client_id=user_name)


def sentiment_analysis():
    documents = ['I like this bool :)', 'short it baby']
    dictionary = {}
    dictionary['like'] = -1
    dictionary['BOOL'] = 1

    # with custom lexicon and Twitter type cleaning
    # analysis = SentimentAnalysis(connection, 'market', dictionary, clean=True, model='Test')
    analysis = SentimentAnalysis(connection, 'market', dictionary, clean=True)
    for result in analysis.detect_sentiment_text(documents):
        print(result)


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

    print("Loading Negative files")
    all_documents = read_documents('D:/DataSets/aclImdb/All/Train/neg', False)
    print("Sending...")
    connection.save_documents('Test', all_documents)

    print("Loading Positive files")
    all_documents = read_documents('D:/DataSets/aclImdb/All/Train/pos', True)
    print("Sending...")
    connection.save_documents('Test', all_documents)


def train():
    analysis = SentimentAnalysis(connection, domain='market', clean=True)
    analysis.train('Test')


if __name__ == "__main__":
    save_documents()
    train()
    print('Test')
    sentiment_analysis()

