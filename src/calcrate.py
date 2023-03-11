import confcore, core

class FrameCalc:

    def __init__(self):
        self.model = core.Solver()
        self.model.clear()

    def calculate(self):
        self.model.calculate()
        return self.model.result