
from calcrate import FrameCalc
import core

class dataInput:
    def __init__(self):
        pass

    # テストデータを読み込む
    def readTestData(self, s: str):
        calc = FrameCalc()

        mesh = calc.model.mesh
        bc = calc.model.bc