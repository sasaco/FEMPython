import pytest


def test_sampleBeamHexa1():
    from src.FemMain import initModel

    fileName = os.path.abspath(os.path.dirname(os.path.abspath(__file__)) 
                               + '/examples/beam/sampleBeamHexa1.fem')
    # model = initModel('tests/examples/others/sample3.fem')
    model = initModel(fileName)
    model.calculate()
    assert True

if __name__ == "__main__":
    import sys
    import os
    sys.path.append(os.path.abspath(os.path.dirname(os.path.abspath(__file__)) + "/../"))
    import conftest

    test_sampleBeamHexa1()