import pytest


def test_sampleBeamHexa1():
    from src.FemMain import initModel
    model = initModel('tests/example_files/beam/sampleBeamHexa1.fem')
    model.calculate()
    assert True

if __name__ == "__main__": 

    print('Running benchmarks with COUNT')
    #####
    from FEMCore import *
    #a = stiffnessMatrix('test')
    #####
    

    test_sampleBeamHexa1() 