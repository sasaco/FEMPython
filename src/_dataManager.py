import config, core

class _dataManager:

    def __init__(self):

        # core �� �ߓ_�ԍ��� int �Ȃ̂� �ߓ_Id �� �ߓ_�ԍ� ��ϊ����� dict ������
        self._node_index = {}
        self._current_node_index = 0

        # core �� ���ޔԍ��� int �Ȃ̂� ����Id �� ���ޔԍ� ��ϊ����� dict ������
        self._element_index = {}
        self._current_element_index = 0

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

        self.node_index[ID] = self._current_node_index
        node = core.FENode(self._current_node_index, x, y, z)

        self._nodes.append(node)

        self._current_node_index += 1


    # �v�f��ǉ�����
    def addBarElement(self, label, node_i, node_j, element, palam):

        if node_i in self.node_index:
            i_node_index = self.node_index[node_i]
        if node_j in self.node_index:
            j_node_index = self.node_index[node_j]


        self._element_index[label] = self._current_element_index

        columns = []
        columns.append(str(self._current_element_index))
        columns.append(str(i_node_index))
        columns.append(str(j_node_index))
        columns.append(str())
        element = core.ElementManager("barelement", columns)

        self._elements.append(element)

        self._current_element_index += 1






