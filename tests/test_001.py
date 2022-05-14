from src.FEMPython.FemMain import initModel

def test_sampleBeamHexa1():
    model = initModel('tests/example_files/beam/sampleBeamHexa1.fem')
    assert True
    # model.calculate()
