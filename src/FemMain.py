from .FemDataModel import FemDataModel
from .FileIO import readFemModel
#--------------------------------------------------------------------#
# 3次元有限要素法(FEM)


# データを初期化する
# fileName - データファイル名
def initModel(fstr: str) -> FemDataModel:
    model = FemDataModel()
    readFemModel(fstr.strip().split('\n'), model)
    return model



