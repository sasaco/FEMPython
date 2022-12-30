import pytest
import sys 
import os

from src.FemMain import initModel


def test_sampleBeamHexa1():
    file_path = sys.path.append(os.path.abspath(os.path.dirname(os.path.abspath(__file__))
                + "/../examples/beam/sampleBeamHexa1.fem"))
    model = initModel(file_path)
    model.calculate()
    assert True

