import os
import socket
from os import path

from psenti import SentimentAnalysis, SentimentConnection, Document, add_logger

import logging
# create logger
logger = logging.getLogger('psenti')

add_logger(logger)

user_name = socket.gethostname()
# host = '192.168.0.70'
# port = 7044
host = 'sentiment2.wikiled.com'
port = 80

def sentiment_analysis():
    documents = ['I like this bool :)', 'short it baby']
    dictionary = {}
    dictionary['like'] = -1
    dictionary['BOOL'] = 1

    # with custom lexicon and Twitter type cleaning
    # analysis = SentimentAnalysis(connection, 'market', dictionary, clean=True, model='Test')
    with SentimentConnection(host=host, port=port, client_id=user_name) as connection:
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
    #save_documents()
    #train()
    print('Test')
    sentiment_analysis()
    sentiment_analysis()

