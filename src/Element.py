from Nodes import Nodes
import math
import numpy as np
from typing import List

from FENode import FENode
#--------------------------------------------------------------------#

# 三角形2次要素のガウス積分の積分点座標
GTRI2 = [1/6, 2/3]
# 四面体2次要素のガウス積分の積分点座標
GTETRA2 = [0.25 - 0.05 * math.sqrt(5), 0.25 + 0.15 * math.sqrt(5)]
# 四角形1次要素のガウス積分の積分点座標
GX2 = [ -1 / math.sqrt(3), 1 / math.sqrt(3)]
# 四角形2次要素のガウス積分の積分点座標
GX3 = [ -math.sqrt(0.6), 0, math.sqrt(0.6)]
# ガウス積分の重み係数
GW3 = [ 5/9, 8/9, 5/9 ]
C1_3 = 1/3
C1_6 = 1/6
C1_12 = 1/12
C1_24 = 1/24	# 1/3,1/6,1/12,1/24


# 節点を入れ替える
# i1,i2 - 節点インデックス
def swap(nodes,i1,i2):
    t = nodes[i1]
    nodes[i1] = nodes[i2]
    nodes[i2] = t


def normalize(vector: np.ndarray) -> np.ndarray:
    return vector / np.linalg.norm(vector)

# 法線ベクトルを返す
# p - 頂点座標
def normalVector(p: List[FENode]) -> np.ndarray:
    if len(p) < 3:
        return None

    elif len(p) == 3 or len(p) == 6:
        return normalize(np.cross(p[1].sub(p[0]), p[2].sub(p[0])))

    elif len(p)==4 or len(p)==8:
        return normalize(np.cross(p[2].sub(p[0]), p[3].sub(p[1])))

    else:
        vx = 0
        vy = 0
        vz = 0
        for i in range(len(p)):
            p1 = p[(i+1)%len(p)]
            p2 = p[(i+2)%len(p)]
            norm = np.cross(p1.sub(p[i]), p2.sub(p[i]))
            vx += norm[0]
            vy += norm[1]
            vz += norm[2]

        return normalize(np.array([vx,vy,vz]))

# 平面上の角度を求める
# p0 - 基点
# p1,p2 - 頂点
def planeAngle(p0: FENode, p1: FENode, p2: FENode):
    v1 = normalize(p1.sub(p0))
    v2 = normalize(p2.sub(p0))
    return math.acos(min(max(np.dot(v1, v2),0),1))

# 三角形の立体角を球面過剰から求める
# p0 - 基点
# p1,p2,p3 - 頂点
def solidAngle(p0: FENode, p1:FENode, p2:FENode, p3:FENode):
    v1 = p1.sub(p0)
    v2 = p2.sub(p0)
    v3 = p3.sub(p0)
    v12 = normalize(np.cross(v1, v2))
    v23 = normalize(np.cross(v2, v3))
    v31 = normalize(np.cross(v3, v1))
    a1 = max(min(np.dot(-v12, v31),1),-1)
    a2 = max(min(np.dot(-v23, v12),1),-1)
    a3 = max(min(np.dot(-v31, v23),1),-1)
    return math.acos(a1) + math.acos(a2) + math.acos(a3) - math.pi


# 方向余弦マトリックスを返す
# p - 頂点座標
# axis - 断面基準方向ベクトル
def dirMatrix(p: np.ndarray, axis = None):
    v = dirVectors(p, axis)
    return v
    # [
    #     [v[0].x, v[1].x, v[2].x],
    #     [v[0].y, v[1].y, v[2].y],
    #     [v[0].z, v[1].z, v[2].z]
    # ]


# 方向余弦マトリックスを返す
# p - 頂点座標
# axis - 断面基準方向ベクトル
def dirVectors(p: np.ndarray, axis):
    if len(p)==2:		# 梁要素
        v1 = normalize(p[1].sub(p[0]))
        if not axis is None:
            dt = np.dot(v1, axis)
            v2 = np.array( (axis[0] - dt * v1[0],
                            axis[1] - dt * v1[1],
                            axis[2] - dt * v1[2]))
            if np.sqrt(np.sum(np.square(v2))) > 0:
                v2 = normalize(v2)

        if np.sqrt(np.sum(np.square(v2)))==0:
            if(abs(v1[0]) < abs(v1[2])):
                v2 = normalize(np.array([
                    1 - v1[0] * v1[0], -v1[0] * v1[1], -v1[0] * v1[2]
                ]))
            else:
                v2 = normalize(np.array([
                    -v1[1] * v1[0], 1 - v1[1] * v1[1], -v1[1] * v1[2]
                ]))

        v3 = np.cross(v1, v2)
        return [v1, v2, v3]

    elif len(p)>2:		# シェル要素
        v3 = normalVector(p)
        v2 = p[1].sub(p[0])
        v2 = normalize(np.cross(v3, v2))
        v1 = np.cross(v2, v3)
        return [v1, v2, v3]

    return None


# 剛性マトリックスの方向を修正する
# d - 方向余弦マトリックス
# k - 剛性マトリックス
def toDir(d,k):
    a = np.dot(d,k)
    for i in range(len(k)):
        ki=k[i]
        ai=a[i]
        for j in range(len(ki)):
            ki[j] = np.dot(ai, d[j])


# 剛性マトリックスの方向を修正する
# d - 方向余弦マトリックス
# k - 剛性マトリックス
def toDir3(d, k):
    a = np.zeros((3,3))
    for i in range(0, len(k), 3):
        for j in range(0, len(k[i]), 3):
            for i1 in range(3):
                ai=a[i1]
                di=d[i1]
                for j1 in range(3):
                    s=0
                    for ii in range(3):
                        s += di[ii] * k[i+ii][j+j1]
                    ai[j1] = s

            for i1 in range(3):
                ai = a[i1]
                ki = k[i+i1]
                for j1 in range(3):
                    ki[j+j1] = np.dot(ai, d[j1])


#--------------------------------------------------------------------#
# 要素
# label - 要素ラベル
# material - 材料のインデックス
# nodes - 節点番号
class FElement(Nodes):
    def __init__(self, label, material, nodes):
        super().__init__(nodes)
        self.label=label
        self.material=material
        self.isShell = False		# シェル要素ではない
        self.isBar = False		# 梁要素ではない


    # 要素境界を返す
    # element - 要素ラベル
    def borders(self, element):
        count = self.borderCount()
        borders = []
        for i in range(count):
            borders[i] = self.border(element,i)
        return borders


    # 積分点の剛性マトリックスを返す
    # d - 応力-歪マトリックス
    # b - 歪-変位マトリックスの転置行列
    # coef - 係数
    def stiffPart(self, d, b, coef) -> List[np.ndarray]:
        size1 = len(b)
        size2 = len(d)
        k=[]
        for i in range(size1):
            a=[]
            bi=b[i]
            for j in range(size2):
                a.append(coef * np.dot(bi,d[j]))

            ki=[]
            for j in range(size1):
                ki.append(np.dot(a,b[j]))

            k.append(ki)

        return k


    # 節点変位を1次元配列に変換する
    # u - 節点変位
    # dof - 節点自由度
    def toArray(self, u, dof):
        count = self.nodeCount()
        v=[]
        for i in range(count):
            ux = u[i].x
            for j in range(dof):
                v.append(ux[j])

        return v


    # 節点変位を局所座標系・1次元配列に変換する
    # u - 節点変位
    # d - 方向余弦マトリックス
    def toLocalArray(self, u,d):
        v = []
        for i in range(2):
            ux = u[i].x
            for j in range(3):
                v.append(d[0][j] * ux[0] + d[1][j] * ux[1] + d[2][j] * ux[2])

            for j in range(3):
                v.append(d[0][j] * ux[3] + d[1][j] * ux[4] + d[2][j] * ux[5])

        return v

