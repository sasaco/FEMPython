
#--------------------------------------------------------------------#
# 節点集合
# nodes - 節点番号
class Nodes:
    def __init__(self, nodes):
        self.nodes=nodes

    # 節点数を返す
    def nodeCount(self):
        return len(self.nodes)
