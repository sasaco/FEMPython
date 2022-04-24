from FEMPython.FemDataModel import FemDataModel
from FEMPython.FileIO import readServerFemFile
#--------------------------------------------------------------------#
# 3次元有限要素法(FEM)


# データを初期化する
# fileName - データファイル名
def initModel(fileName):
    model = FemDataModel()
    if fileName!=None:
        readServerFemFile(fileName, model)
    return model



