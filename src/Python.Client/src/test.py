import os
import socket
from os import path

from pypsenti import SentimentAnalysis, SentimentConnection, Document


user_name = socket.gethostname()
connection = SentimentConnection(host='localhost', web_port=5000, stream_port=1883, client_id=user_name)

def sentiment_analysis():
    documents = ['I like this bool :)', 'short it baby']
    dictionary = {}
    dictionary['like'] = -1
    dictionary['BOOL'] = 1

    # with custom lexicon and Twitter type cleaning
    analysis = SentimentAnalysis(connection, 'market', dictionary, clean=True, model='Test')
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

    all_documents = read_documents('E:/DataSets/aclImdb/All/Train/neg', False)
    connection.save_documents('Test', all_documents)

    all_documents = read_documents('E:/DataSets/aclImdb/All/Train/pos', True)
    connection.save_documents('Test', all_documents)


def train():
    analysis = SentimentAnalysis(SentimentConnection('TestConnection17'), domain='market', clean=True)
    analysis.train('Test')


if __name__ == "__main__":
    #save_documents()
    #train()
    sentiment_analysis()

