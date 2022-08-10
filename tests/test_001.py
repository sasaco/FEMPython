from src.FemMain import initModel

def test_sampleBeamHexa1():
    model = initModel('tests/example_files/beam/sampleBeamHexa1.fem')
    assert True
    # model.calculate()





if __name__ == "__main__":

    test_sampleBeamHexa1()