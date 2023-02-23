import numpy as np

#--------------------------------------------------------------------#
# 節点
# label - 節点ラベル
# x,y,z - x,y,z座標
class FENode():
    def __init__(self, label: int, x: float, y: float, z: float):
        self.x: float = x
        self.y: float = y
        self.z: float = z
        self.label: int = label

    # 引き算
    def __sub__(self, other):
        x = self.x - other.x
        y = self.y - other.y
        z = self.z - other.z
        return np.array([x, y, z])

    # 節点のコピーを返す
    def clone(self):
        return FENode(self.label, self.x, self.y, self.z)

    def distanceTo(self, other) -> float:
        xx = self.x - other.x
        yy = self.y - other.y
        zz = self.z - other.z
        return np.sqrt(xx**2 + yy**2 + zz**2)

    # 節点を表す文字列を返す
    def toString(self):
        return 'Node\t'+self.label.toString(10)+'\t'+ \
                    self.x+'\t'+self.y+'\t'+self.z
