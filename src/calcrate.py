if 'core' not in sys.modules:
    import sys, os
    sys.path.append(os.path.join(os.path.dirname(__file__), '../core/bin'))
    import core

class FrameCalc:

    def __init__(self):
        self.model = core.Solver()
        self.model.clear()

    def calculate(self):
        self.model.calculate()
        return self.model.result