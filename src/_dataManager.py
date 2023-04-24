import _config, core

class _dataManager:

    def __init__(self):

        # core �� �ߓ_�ԍ��� int �Ȃ̂� �ߓ_Id �� �ߓ_�ԍ� ��ϊ����� dict ������
        self._node_index = {}

        # core �� ���ޔԍ��� int �Ȃ̂� ����Id �� ���ޔԍ� ��ϊ����� dict ������
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


    # �ߓ_��ǉ�����
    def addNode(self, ID: str, x: float, y: float, z: float):
        self._nodes.append(core.FENode(str(ID), float(x), float(y), float(z)))


    # �v�f��ǉ�����
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






