try:
    import core
except:
    import sys, os
    path = os.path.join(os.path.dirname(__file__), '../core/bin')
    sys.path.append(path)
    import core
