import json
import uuid
import csv
from requests import Session
from sklearn import metrics


class SentimentAnalysis(object):

    def __init__(self, documents, lexicon=None, domain=None, clean=False):
        self.lexicon = lexicon
        self.documents = documents
        self.clean = clean
        self.domain = domain
        self.host = 'sentiment.wikiled.com'
        # self.host = 'localhost:63804'
        self.__load__()
        
        if domain is not None and domain.lower() not in [x.lower() for x in self.supported_domains]:
            raise ValueError("Not supported domain:" + domain)

    def __load__(self):
        with Session() as session:
            url = 'http://{}/api/sentiment/version'.format(self.host)
            self.version = session.get(url).content
            url = 'http://{}/api/sentiment/domains'.format(self.host)
            self.supported_domains = json.loads(session.get(url).content)

    def __iter__(self):
        index = 0
        processed_ids = {}
        batch_request_documents = []
        for document in self.documents:
            id = str(uuid.uuid4())
            batch_request_documents.append({'text': document, 'id': id})
            processed_ids[id] = index
            index += 1
            if len(batch_request_documents) >= 200:
                yield from self.process_on_server(batch_request_documents, processed_ids)
                batch_request_documents = []
                print('Processed {}'.format(index))

        # Process outstanding documents
        if len(batch_request_documents) > 0:
            yield from self.process_on_server(batch_request_documents, processed_ids)

    def process_on_server(self, batch_request_documents, processed_ids):
        data = {}
        data['CleanText'] = self.clean
        if self.lexicon is not None:
            data['dictionary'] = self.lexicon
        if self.domain is not None:
            data['domain'] = self.domain

        with Session() as session:
            url = 'http://{}/api/sentiment/parsestream'.format(self.host)
            data['documents'] = batch_request_documents
            json_object = json.dumps(data, indent=2)
            headers = {'Content-type': 'application/json', 'Accept': 'text/plain'}
            with session.post(url, json_object, headers=headers, stream=True) as r:
                for line in r.iter_lines():
                    # filter out keep-alive new lines
                    if line:
                        decoded_line = line.decode('utf-8')
                        sentimen_result = json.loads(decoded_line)
                        sentiment_class = 0
                        if sentimen_result['Stars'] is not None:
                            if sentimen_result['Stars'] > 3:
                                sentiment_class = 1
                            else:
                                sentiment_class = -1
                        id = processed_ids[sentimen_result['Id']]
                        yield (id, sentiment_class, sentimen_result)


if __name__ == "__main__":
    # documents = ['I like this bool :)', 'short it baby']
    # # with standard lexicon
    # sentiment = SentimentAnalysis(documents)
    # for result in sentiment:
    #     print(result)
    #
    # dictionary = {}
    # dictionary['like'] = -1
    # dictionary['BOOL'] = 1
    #
    # # with custom lexicon and Twitter type cleaning
    # sentiment = SentimentAnalysis(documents, dictionary, clean=True)
    # for result in sentiment:
    #     print(result)

    row_id = 0
    documents = []
    sentiments = []
    with open('h:/DataSets/Dataset.Market/laballed_twt_sample.csv', encoding='utf8') as file:
        reader = csv.reader(file)
        for row in reader:
            row_id += 1
            if row_id == 1:
                continue
            #if row_id > 2:
            #    break

            text = row[1]
            if row[2] == 'positive':
                sentiments.append(1)
            else:
                sentiments.append(-1)
            documents.append(text)

    result_class = [0] * len(documents)
    sentiment_analysis = SentimentAnalysis(documents, domain='TwitterMarket')
    y_actual = []
    y_result = []
    for result in sentiment_analysis:
        result_class[result[0]] = result[1]
        if result[1] != 0:
            y_actual.append(sentiments[result[0]])
            y_result.append(result[1])

    report = metrics.classification_report(y_actual, y_result, digits=3)
    print('\n{}'.format(report))
