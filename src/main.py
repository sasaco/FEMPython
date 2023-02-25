import core

def initModel(s: str):

    model = core.readFemModel(s)
    model.calculate()
    #model.clear()

    return model


