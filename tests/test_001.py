import FEMPython as fem

model = fem.initModel('./data/beam/sampleBeamHexa1.fem')

model.calculate()
