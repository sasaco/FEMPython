try:
    import core
except:
    import sys, os
    sys.path.append(os.path.join(os.path.dirname(__file__), '../core/bin'))
    import core