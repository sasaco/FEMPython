import FEMPython as fem


def test_sampleBeamHexa1():
    model = fem.initModel('tests/example_files/beam/sampleBeamHexa1.fem')
    model.calculate()
