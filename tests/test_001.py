import pytest
import sys
import os

from src.FemMain import initModel

def test_sampleBeamHexa1():

    fileName = os.path.abspath(os.path.dirname(os.path.abspath(__file__)) 
                + '/examples/beam/sampleBeamHexa1.fem')
    f = open(fileName, encoding="utf-8")
    fstr = f.read()  # ファイル終端まで全て読んだデータを返す
    f.close()

    model = initModel(fileName)
    model.calculate()
    assert True




