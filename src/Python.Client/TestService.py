from requests import Session
from requests.auth import HTTPBasicAuth
import json
from websocket import create_connection

with Session() as session:

    # session.auth = HTTPBasicAuth("known", "user")
    url = "http://localhost:63804/sentiment"
    ws_url = "ws://localhost:63804/sentiment"

    url = '{url}/negotiate'.format(url=url)
    data = {
        "connectionId": "9gQg9iim9RMRZ7IVIJtDRg",
        "availableTransports":
            [
                {
                    "transport": "WebSockets",
                    "transferFormats": ["Text", "Binary"]
                }
            ]
    }

    negotiate = session.post(url, data)
    negotiate.raise_for_status()
    answer = negotiate.json()
    url = '{url}?id={id}'.format(url=ws_url, id=answer['connectionId'])

    ws = create_connection(url, enable_multithread=True)

    data ={
            'H': 'Sentiment',
            'M': 'Ping',
            'A': 'Test',
            'I': 1
        }
    ws.send(json.dumps(data))
    print('hmm...')