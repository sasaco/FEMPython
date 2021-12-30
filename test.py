import sys
sys.path.append('./src')

from FemMain import FemMain

fem = FemMain()
fem.initModel('./data/bend/sampleBendHexa1WT.fem')

fem.calculate()
