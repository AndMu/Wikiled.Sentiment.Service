import json

from requests import Session


class SentimentAnalysis(object):

    def __init__(self, documents, lexicon=None, domain=None, clean=False):
        self.lexicon = lexicon
        self.documents = documents
        self.clean = clean
        self.domain = domain
        self.__load__()
        
        if domain is not None and domain.lower() not in [x.lower() for x in self.supported_domains]:
            raise ValueError("Not supported domain:" + domain)

    def __load__(self):
        with Session() as session:
            url = "http://sentiment.wikiled.com/api/sentiment/version"
            self.version = session.get(url).content
            url = "http://sentiment.wikiled.com/api/sentiment/domains"
            self.supported_domains = json.loads(session.get(url).content)

    def __iter__(self):
        with Session() as session:
            url = "http://sentiment.wikiled.com/api/sentiment/parsestream"
            data = {}
            data['CleanText'] = self.clean
            if self.lexicon is not None:
                data['dictionary'] = self.lexicon
            if self.domain is not None:
                data['domain'] = self.domain
            request_documents = []
            for document in self.documents:
                request_documents.append({'text': document})
            data['documents'] = request_documents
            json_object = json.dumps(data, indent=2)
            headers = {'Content-type': 'application/json', 'Accept': 'text/plain'}
            with session.post(url, json_object, headers=headers, stream=True) as r:
                for line in r.iter_lines():
                    # filter out keep-alive new lines
                    if line:
                        decoded_line = line.decode('utf-8')
                        yield json.loads(decoded_line)


if __name__ == "__main__":
    documents = ['I like this bool :)', 'short it baby']
    # with standard lexicon
    sentiment = SentimentAnalysis(documents)
    for result in sentiment:
        print(result)

    dictionary = {}
    dictionary['like'] = -1
    dictionary['BOOL'] = 1

    # with custom lexicon and Twitter type cleaning
    sentiment = SentimentAnalysis(documents, dictionary, clean=True)
    for result in sentiment:
        print(result)

    sentiment = SentimentAnalysis(documents, domain='TwitterMarket')
    for result in sentiment:
        print(result)
        
