from requests import Session
from requests.auth import HTTPBasicAuth
import json
from websocket import create_connection

with Session() as session:

    # session.auth = HTTPBasicAuth("known", "user")
    url = "http://localhost:63804/sentiment"
    ws_url = "ws://localhost:63804/sentiment"

    url = '{url}/negotiate'.format(url=url)

    negotiate = session.post(url)
    negotiate.raise_for_status()
    answer = negotiate.json()
    url = '{url}?id={id}'.format(url=ws_url, id=answer['connectionId'])
    headers = ['%s: %s' % (name, session.headers[name]) for name in session.headers]
    headers.append('X-Requested-With', 'XMLHttpRequest')
    ws = create_connection(url, enable_multithread=True)

    data ={
            'H': 'Sentiment',
            'M': 'Ping',
            'A': 'Test',
            'I': 1
        }
    ws.send(json.dumps(data))
    print('hmm...')