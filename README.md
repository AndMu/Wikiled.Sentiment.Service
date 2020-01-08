# .Net Web API Sentiment service

* Sentiment service, with [Docker support](https://hub.docker.com/r/wikiled/sentiment/)
* Based on [pSenti GitHub project](https://github.com/AndMu/Wikiled.Sentiment)

## Concept-Level Domain Sentiment Analysis System

The core of **pSenti** is its lexicon-based system, so it shares many common NLP processing techniques with other similar approaches.

## Python samples

Library with samples can be found [here](src/Python.Client)

```
reviews = ['I love this hello kitty decal! I like that the bow is pink instead of red. Only bad thing is that after putting it on the window there a few air bubbles, but that most likely my fault. Shipped fast too.',
                  'I bought this for my 3 yr old daughter when I took it out the pack it had a bad odour, cute but very cheap material easy to ripe.  When I tried it on her it was too big, but of course she liked it so I kept it. I dressed her up in it and she looked cute.']

user_name = socket.gethostname()
host = 'sentiment2.wikiled.com'
port=80
with SentimentConnection(host=host, port=port, client_id=user_name) as connection:
    analysis = SentimentAnalysis(connection, domain='market')
    for result in analysis.detect_sentiment_text(amazon_reviews):
        if result['Stars'] is None:
            print('No Sentinent')
        else:
            print(f'Sentinment Stars: {result["Stars"]:1.2f}')
```