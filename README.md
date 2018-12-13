# .Net Web API Sentiment service

* Sentiment service, with [Docker support](https://hub.docker.com/r/wikiled/sentiment/)
* Based on [pSenti GitHub project](https://github.com/AndMu/Wikiled.Sentiment)

## Concept-Level Domain Sentiment Analysis System

The core of **pSenti** is its lexicon-based system, so it shares many common NLP processing techniques with other similar approaches.

## Python samples

Working sample can be found [here](src/Python.Client/TestService.py)

```
url = 'http://{}/api/sentiment/parsestream'.format(self.host)
data['documents'] = batch_request_documents
json_object = json.dumps(data, indent=2)
headers = {'Content-type': 'application/json', 'Accept': 'text/plain'}
with session.post(url, json_object, headers=headers, stream=True) as r:
	for line in r.iter_lines():                    
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
```