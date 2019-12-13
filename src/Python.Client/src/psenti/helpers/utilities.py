import asyncio, queue
import logging
logger_on = False


def batch(iterable, n=1):
    l = len(iterable)
    for ndx in range(0, l, n):
        yield iterable[ndx:min(ndx + n, l)]


def wrap_async_iter(ait, loop):
    # create an asyncio loop that runs in the background to
    # serve our asyncio needs

    """Wrap an asynchronous iterator into a synchronous one"""
    q = queue.Queue()
    _END = object()

    def yield_queue_items():
        while True:
            next_item = q.get()
            if next_item is _END:
                break
            yield next_item
        # After observing _END we know the aiter_to_queue coroutine has
        # completed.  Invoke result() for side effect - if an exception
        # was raised by the async iterator, it will be propagated here.
        async_result.result()

    async def aiter_to_queue():
        try:
            async for item in ait:
                q.put(item)
        finally:
            q.put(_END)

    async_result = asyncio.run_coroutine_threadsafe(aiter_to_queue(), loop)
    return yield_queue_items()


def add_logger(logger, level=logging.INFO):
    global logger_on
    if logger_on:
        return

    logger_on = True
    # create logger
    logger.setLevel(level)

    # create console handler and set level to debug
    ch = logging.StreamHandler()
    ch.setLevel(level)

    # create formatter
    formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')

    # add formatter to ch
    ch.setFormatter(formatter)
    logger.addHandler(ch)