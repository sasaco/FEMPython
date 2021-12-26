import sys
sys.path.append('./src')

from FemMain import FemMain

fem = FemMain()
fem.initModel('./data/beam/sampleBeamHexa1.fem')

fem.calculate()
