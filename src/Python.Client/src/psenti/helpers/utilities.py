import logging

LOG = logging.getLogger(__name__)


class EventHandler(list):
    """Event subscription.

    A list of callable objects. Calling an instance of this will cause a
    call to each item in the list in ascending order by index.

    Example Usage:
    >>> def f(x):
    ...     print 'f(%s)' % x
    >>> def g(x):
    ...     print 'g(%s)' % x
    >>> e = EventHandler()
    >>> e()
    >>> e.append(f)
    >>> e(123)
    f(123)
    >>> e.remove(f)
    >>> e()
    >>> e += (f, g)
    >>> e(10)
    f(10)
    g(10)
    >>> del e[0]
    >>> e(2)
    g(2)

    """

    def __init__(self):
        self.is_safe = False

    def __call__(self, *args, **kwargs):
        for f in self:
            try:
                f(*args, **kwargs)
            except:
                LOG.exception('Event handling')
                if not self.is_safe:
                    raise

    def __repr__(self):
        return "Event(%s)" % list.__repr__(self)

    def subscribe(self, method):
        if method not in self:
            self.append(method)


def batch(iterable, n=1):
    l = len(iterable)
    for ndx in range(0, l, n):
        yield iterable[ndx:min(ndx + n, l)]

