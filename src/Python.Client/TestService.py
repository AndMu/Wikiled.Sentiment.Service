import json

from requests import Session


class SentimentAnalysis(object):

    def __init__(self, documents, lexicon=None):
        self.lexicon = lexicon
        self.documents = documents

    def __iter__(self):
        with Session() as session:
            # session.auth = HTTPBasicAuth("known", "user")
            url = "http://localhost:63804/api/sentiment/parsestream"
            data = {}
            if self.lexicon is not None:
                data["dictionary"] = self.lexicon
            request_documents = []
            for document in self.documents:
                request_documents.append({'text': document})
            data["documents"] = request_documents
            json_object = json.dumps(data, indent=2)
            headers = {'Content-type': 'application/json', 'Accept': 'text/plain'}
            with session.post(url, json_object, headers=headers, stream=True) as r:
                for line in r.iter_lines():
                    # filter out keep-alive new lines
                    if line:
                        decoded_line = line.decode('utf-8')
                        yield json.loads(decoded_line)


if __name__ == "__main__":
    documents = ['I like this bool']
    sentiment = SentimentAnalysis(documents)
    for result in sentiment:
        print(result)
