import FEMPython as fem

model = fem.initModel('tests/example_files/beam/sampleBeamHexa1.fem')

model.calculate()
