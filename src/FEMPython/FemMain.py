from FemDataModel import FemDataModel
from FileIO import readServerFemFile
#--------------------------------------------------------------------#
# 3次元有限要素法(FEM)

class FemMain:

    def __init__(self):
        self.model: FemDataModel = None		# FEclass Mデータモデル

    # データを初期化する
    # fileName - データファイル名
    def initModel(self, fileName):
        self.model = FemDataModel()
        if fileName!=None:
            readServerFemFile(fileName, self.model)
        return


    def calculate(self):
        self.model.calculate()


