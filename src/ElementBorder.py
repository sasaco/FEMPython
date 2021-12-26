import math
import numpy as np

from Element import GTRI2, GX2, C1_6, GX3, GW3, C1_6, C1_3, normalize, normalVector
from Nodes import Nodes
#--------------------------------------------------------------------#
# 三角形2次要素の積分点のξ,η座標,重み係数
TRI2_INT=[[GTRI2[0],GTRI2[0],C1_6],[GTRI2[1],GTRI2[0],C1_6],[GTRI2[0],GTRI2[1],C1_6]]

# 四角形1次要素の積分点のξ,η座標,重み係数
QUAD1_INT=[[GX2[0],GX2[0],1],[GX2[1],GX2[0],1],[GX2[0],GX2[1],1],[GX2[1],GX2[1],1]]

# 四角形2次要素の積分点のξ,η座標,重み係数
QUAD2_INT=[
    [GX3[0],GX3[0],GW3[0]*GW3[0]],
    [GX3[1],GX3[0],GW3[1]*GW3[0]],
    [GX3[2],GX3[0],GW3[2]*GW3[0]],
    [GX3[0],GX3[1],GW3[0]*GW3[1]],
    [GX3[1],GX3[1],GW3[1]*GW3[1]],
    [GX3[2],GX3[1],GW3[2]*GW3[1]],
    [GX3[0],GX3[2],GW3[0]*GW3[2]],
    [GX3[1],GX3[2],GW3[1]*GW3[2]],
    [GX3[2],GX3[2],GW3[2]*GW3[2]]
]


# 行列の和を計算する
# a - 基準行列
# da - 加える行列
def addMatrix(a, da):
    for i in range(len(a)):
        for j in range(len(a[i])):
            a[i][j] += da[i][j]


# ベクトルの和を計算する
# v - 基準ベクトル
# dv - 加えるベクトル
def addVector(v,dv):
    for i in range(len(v)):
        v[i]+=dv[i]

#--------------------------------------------------------------------#
# 要素境界
# element - 要素ラベル
# nodes - 節点番号
# intP - 積分点のξ,η座標,重み係数
class ElementBorder(Nodes):
    def __init__(self, element, nodes, intP):
        super().__init__(nodes)
        self.element = element
        self.intP = intP
        self.isEdge = False		# 辺ではない


    # 周回順に並んだ節点ラベルを返す
    def cycleNodes(self):
        return self.nodes

    # 要素境界のコピーを返す
    def clone(self):
        return self.constructor(self.element,self.nodes.concat())

    # 要素境界を比較する
    # b - 比較対象の要素境界
    def compare(self, b):
        p1 = self.nodes.concat()
        p2 = b.nodes.concat()
        sorted(p1, key=lambda a, b: a-b)
        sorted(p2, key=lambda a, b: a-b)
        count = min(len(p1), len(p2))
        for i in range(count):
            d = p1[i] - p2[i]
            if ( d != 0 ):
                return d

        return len(p1) - len(p2)


    # 擬似ヤコビアンを返す
    # p - 節点
    # sf - 形状関数行列
    def jacobian(self, p, sf):
        count = self.nodeCount()
        jac = [0,0,0,0,0,0]
        for i in range(count):
            for j in range(2):
                jac[j] += sf[i][j+1] * p[i].x
                jac[j+2] += sf[i][j+1] * p[i].y
                jac[j+4] += sf[i][j+1] * p[i].z

        j1 = jac[2] * jac[5] - jac[4] * jac[3]
        j2 = jac[4] * jac[1] - jac[0] * jac[5]
        j3 = jac[0] * jac[3] - jac[2] * jac[1]

        return math.sqrt(j1*j1+j2*j2+j3*j3)


    # 積分点の形状関数マトリックス [ NiNj ] を返す
    # p - 節点
    # x - ξ,η座標
    # w - 重み係数
    def shapeMatrixPart(self, p, x, w):
        sf = self.shapeFunction(x[0], x[1])
        coef = w * self.jacobian(p,sf)
        count = self.nodeCount()
        matrix = []
        for i in range(count):
            matrix[i] = []
            cf2 = coef * sf[i][0]
            for j in range(count):
                matrix[i][j] = cf2 * sf[j][0]

        return matrix


    # 積分点の形状関数ベクトル [ Ni ] を返す
    # p - 節点
    # x - ξ,η座標
    # w - 重み係数
    def shapeVectorPart(self, p, x, w):
        sf = self.shapeFunction(x[0], x[1])
        coef = w * self.jacobian(p, sf)
        count = self.nodeCount()
        vector = []
        for i in range(count):
            vector[i] = coef * sf[i][0]

        return vector


    # 形状関数マトリックス [ ∫NiNjdS ] を返す
    # p - 節点
    # coef - 係数
    def shapeFunctionMatrix(self, p, coef):
        count = self.nodeCount()
        s = np.zeros((count,count))
        for i in range(len(self.intP)):
            addMatrix(s, self.shapeMatrixPart(p,self.intP[i], coef*self.intP[i][2]))

        return s


    # 形状関数ベクトル [ ∫NidS ] を返す
    # p - 節点
    # coef - 係数
    def shapeFunctionVector(self, p, coef):
        count = self.nodeCount()
        s = np.zeros((count))
        for i in range(len(self.intP)):
            addVector(s, self.shapeVectorPart(p,self.intP[i], coef*self.intP[i][2]))

        return s


#--------------------------------------------------------------------#
# 辺1次要素境界
# element - 要素ラベル
# nodes - 節点番号
class EdgeBorder1(ElementBorder):
    def __init__(self, element, nodes):
        super().__init__(element, nodes, None)
        self.isEdge = True

    # 要素境界名称を返す
    def getName(self):
        return 'EdgeBorder1'

    # 節点数を返す
    def nodeCount(self):
        return 2

    # 要素境界を分割する
    def splitBorder(self):
        return [self.clone()]


    # 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
    # xsi,eta - 要素内部ξ,η座標
    def shapeFunction(self, xsi, eta):
        if(eta==None):
            return [[0.5*(1-xsi),-0.5],[0.5*(1+xsi),0.5,1]]
        else:
            return [[0.5*(1-xsi),-0.5,0],[0.5*(1+xsi),0.5,0],[0,0,0]]


    # 形状関数マトリックス [ ∫NiNjdS ] を返す
    # p - 節点
    # coef - 係数
    def shapeFunctionMatrix(self, p, coef):
        value = C1_3 * coef*p[0].distanceTo(p[1])
        vh = 0.5 * value
        return [[value,vh],[vh,value]]


    # 形状関数ベクトル [ ∫NidS ] を返す
    # p - 節点
    # coef - 係数
    def shapeFunctionVector(self, p, coef):
        value = 0.5 * coef * p[0].distanceTo(p[1])
        return [value,value]


    # 辺の法線ベクトルを返す
    # p - 節点
    # ep - 要素の節点
    def normalVector(self, p, ep):
        ne = normalVector(ep)
        dir = p[1].clone().sub(p[0])
        return normalize(dir.cross(ne))


#--------------------------------------------------------------------#
# 三角形1次要素境界
# element - 要素ラベル
# nodes - 節点番号
class TriangleBorder1(ElementBorder):
    def __init__(self, element, nodes):
        super().__init__(element, nodes, None)

    # 要素境界名称を返す
    def getName(self):
        return 'TriangleBorder1'

    # 節点数を返す
    def nodeCount(self):
        return 3

    # 要素境界を分割する
    def splitBorder(self):
        return [self.clone()]

    # 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
    # xsi,eta - 要素内部ξ,η座標
    def shapeFunction(self, xsi, eta):
        return [[1-xsi-eta,-1,-1],[xsi,1,0],[eta,0,1]]

    # 擬似ヤコビアンを返す
    # p - 節点
    def jacobian(self, p):
        p0x=p[0].x,p0y=p[0].y,p0z=p[0].z
        j1=(p[1].y-p0y)*(p[2].z-p0z)-(p[1].z-p0z)*(p[2].y-p0y)
        j2=(p[1].z-p0z)*(p[2].x-p0x)-(p[1].x-p0x)*(p[2].z-p0z)
        j3=(p[1].x-p0x)*(p[2].y-p0y)-(p[1].y-p0y)*(p[2].x-p0x)
        return math.sqrt(j1*j1+j2*j2+j3*j3)

    # 形状関数マトリックス [ ∫NiNjdS ] を返す
    # p - 節点
    # coef - 係数
    def shapeFunctionMatrix(self, p, coef):
        value = coef * self.jacobian(p) / 12
        vh = 0.5 * value
        count = self.nodeCount()
        s = np.repeat(vh, count * count).reshape(count, count)
        for i in range(count):
            s[i][i] = value

        return s


    # 形状関数ベクトル [ ∫NidS ] を返す
    # p - 節点
    # coef - 係数
    def shapeFunctionVector(self, p, coef):
        return np.repeat(C1_6 * coef * self.jacobian(p), self.nodeCount())


#--------------------------------------------------------------------#
# 三角形2次要素境界
# element - 要素ラベル
# nodes - 節点番号
class TriangleBorder2(ElementBorder):
    def __init__(self, element, nodes):
        super().__init__(element, nodes, TRI2_INT)

    # 要素境界名称を返す
    def getName(self):
        return 'TriangleBorder2'

    # 節点数を返す
    def nodeCount(self):
        return 6

    # 周回順に並んだ節点ラベルを返す
    def cycleNodes(self):
        return [self.nodes[0],self.nodes[3],self.nodes[1],self.nodes[4],
                        self.nodes[2],self.nodes[5]]

    # 要素境界を分割する
    def splitBorder(self):
        p = self.nodes
        return [TriangleBorder1(self.element,[p[0],p[3],p[5]]),
                        TriangleBorder1(self.element,[p[1],p[4],p[3]]),
                        TriangleBorder1(self.element,[p[2],p[5],p[4]]),
                        TriangleBorder1(self.element,[p[3],p[4],p[5]])]

    # 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
    # xsi,eta - 要素内部ξ,η座標
    def shapeFunction(self, xsi, eta):
        xe = 1 - xsi - eta
        return [[xe*(2*xe-1),1-4*xe,1-4*xe],[xsi*(2*xsi-1),4*xsi-1,0],
                        [eta*(2*eta-1),0,4*eta-1],[4*xe*xsi,4*(xe-xsi),-4*xsi],
                        [4*xsi*eta,4*eta,4*xsi],[4*xe*eta,-4*eta,4*(xe-eta)]]


#--------------------------------------------------------------------#
# 四角形1次要素境界
# element - 要素ラベル
# nodes - 節点番号
class QuadangleBorder1(ElementBorder):
    def __init__(self, element, nodes):
        super().__init__(element,nodes, QUAD1_INT)

    # 要素境界名称を返す
    def getName(self):
        return 'QuadangleBorder1'

    # 節点数を返す
    def nodeCount(self):
        return 4

    # 要素境界を分割する
    def splitBorder(self):
        p = self.nodes
        return [TriangleBorder1(self.element,[p[0],p[1],p[2]]),
                        TriangleBorder1(self.element,[p[2],p[3],p[0]])]


    # 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
    # xsi,eta - 要素内部ξ,η座標
    def shapeFunction(self, xsi, eta):
        return [[0.25*(1-xsi)*(1-eta),-0.25*(1-eta),-0.25*(1-xsi)],
                        [0.25*(1+xsi)*(1-eta),0.25*(1-eta),-0.25*(1+xsi)],
                        [0.25*(1+xsi)*(1+eta),0.25*(1+eta),0.25*(1+xsi)],
                        [0.25*(1-xsi)*(1+eta),-0.25*(1+eta),0.25*(1-xsi)]]

#--------------------------------------------------------------------#
# 四角形2次要素境界
# element - 要素ラベル
# nodes - 節点番号
class QuadangleBorder2(ElementBorder):
    def __init__(self, element, nodes):
        super().__init__(element, nodes, QUAD2_INT)

    # 要素境界名称を返す
    def getName(self):
        return 'QuadangleBorder2'

    # 節点数を返す
    def nodeCount(self):
        return 8

    # 周回順に並んだ節点ラベルを返す
    def cycleNodes(self):
        return [self.nodes[0],self.nodes[4],self.nodes[1],self.nodes[5],
                        self.nodes[2],self.nodes[6],self.nodes[3],self.nodes[7]]


    # 要素境界を分割する
    def splitBorder(self):
        p=self.nodes
        return [TriangleBorder1(self.element,[p[0],p[4],p[7]]),
                        TriangleBorder1(self.element,[p[1],p[5],p[4]]),
                        TriangleBorder1(self.element,[p[2],p[6],p[5]]),
                        TriangleBorder1(self.element,[p[3],p[7],p[6]]),
                        TriangleBorder1(self.element,[p[4],p[5],p[6]]),
                        TriangleBorder1(self.element,[p[6],p[7],p[4]])]


    # 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
    # xsi,eta - 要素内部ξ,η座標
    def shapeFunction(self, xsi, eta):
        return [[0.25*(1-xsi)*(1-eta)*(-xsi-eta-1),0.25*(1-eta)*(2*xsi+eta),
                        0.25*(1-xsi)*(xsi+2*eta)],
                        [0.25*(1+xsi)*(1-eta)*(xsi-eta-1),0.25*(1-eta)*(2*xsi-eta),
                        0.25*(1+xsi)*(-xsi+2*eta)],
                        [0.25*(1+xsi)*(1+eta)*(xsi+eta-1),0.25*(1+eta)*(2*xsi+eta),
                        0.25*(1+xsi)*(xsi+2*eta)],
                        [0.25*(1-xsi)*(1+eta)*(-xsi+eta-1),0.25*(1+eta)*(2*xsi-eta),
                        0.25*(1-xsi)*(-xsi+2*eta)],
                        [0.5*(1+xsi)*(1-xsi)*(1-eta),-xsi*(1-eta),
                        -0.5*(1+xsi)*(1-xsi)],
                        [0.5*(1+xsi)*(1+eta)*(1-eta),0.5*(1+eta)*(1-eta),
                        -(1+xsi)*eta],
                        [0.5*(1+xsi)*(1-xsi)*(1+eta),-xsi*(1+eta),
                        0.5*(1+xsi)*(1-xsi)],
                        [0.5*(1-xsi)*(1+eta)*(1-eta),-0.5*(1+eta)*(1-eta),
                        -(1-xsi)*eta]]
