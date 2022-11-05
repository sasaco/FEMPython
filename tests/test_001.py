from asyncio.windows_events import NULL
import pytest

from src.FemMain import initModel


def test_sampleBeamHexa1():
    model = initModel('tests/example_files/beam/sampleBeamHexa1.fem')
    model.calculate()
    assert True

if __name__ == "__main__": 

    #####
    from Core import fast_stiffnessMatrix
    fast_stiffnessMatrix('test')
    #####


    test_sampleBeamHexa1() 