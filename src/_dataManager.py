import _config, core

class _dataManager:

    def __init__(self):

        # core の 節点番号は int なので 節点Id と 節点番号 を変換する dict を持つ
        self._node_index = {}

        # core の 部材番号は int なので 部材Id と 部材番号 を変換する dict を持つ
        self._element_index = {}

        ## core 
        self._materials = []
        self._nodes = [] 
        self._elements = []
        self._shellParams = []
        self._barParams = []
        self._coordinates = []
        self._pressures = []
        self._temperature = []
        self._htcs = []
        self._restraints = []
        self._loads = []


    # 節点を追加する
    def addNode(self, ID: str, x: float, y: float, z: float):
        self._nodes.append(core.FENode(str(ID), float(x), float(y), float(z)))


    # 要素を追加する
    def addElement(self, ID: str, ni: str, nj: str, e: str, cg: float, joint_i: int, joint_j: int):
        columns = []
        columns.append(ID)
        columns.append(str(ni))
        columns.append(str(nj))
        columns.append(str(e))
        columns.append(str(cg))
        columns.append(str(joint_i))
        columns.append(str(joint_j))
        element = core.ElementManager("barelement", columns)

        self._elements.append(element)






