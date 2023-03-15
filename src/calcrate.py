import config, core

class FrameCalc:

    def __init__(self):
        self.core = core
        self.model = self.core.Solver()
        self.model.clear()


    def calculate(self):
        self.model.calculate()
        return self.model.result