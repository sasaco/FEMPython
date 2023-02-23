import pytest
import conftest
import sys
import os

from FemMain import initModel

def test_sampleBeamHexa1():

    fileName = os.path.abspath(os.path.dirname(os.path.abspath(__file__)) 
                + '/examples/beam/sampleBeamHexa1.fem')

    f = open(fileName, encoding="utf-8")
    fstr = f.read()  
    f.close()

    model = initModel(fstr)
    model.calculate()

    assert True


if __name__ == "__main__":
    test_sampleBeamHexa1()
