import sys 
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '../core/bin'))
import core

def initModel(s: str):

    model = core.readFemModel(s)
    model.calculate()
    #model.clear()

    return model


