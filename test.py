import sys
sys.path.append('./src')

from FemMain import FemMain

fem = FemMain()
fem.initModel('./data/shell/shellBeamQuad1.fem')

fem.calculate()
