import math
from typing import List
import numpy as np
from Result import Strain, Stress
from ElementBorder import TriangleBorder1, TriangleBorder2, QuadangleBorder1, QuadangleBorder2, addMatrix
from Element import FElement, swap, solidAngle, planeAngle, GTETRA2, C1_3, GX2, GTRI2, GX3, C1_6, GW3, C1_24
#--------------------------------------------------------------------#

# 四面体2次要素の節点のξ,η,ζ座標
TETRA2_NODE = [ [0,0,0],[1,0,0],[0,1,0],[0,0,1],[0.5,0,0],[0.5,0.5,0],
                [0,0.5,0],[0,0,0.5],[0.5,0,0.5],[0,0.5,0.5]]

# 四面体2次要素の積分点のξ,η,ζ座標,重み係数
TETRA2_INT=[[GTETRA2[0],GTETRA2[0],GTETRA2[0],C1_24],
            [GTETRA2[1],GTETRA2[0],GTETRA2[0],C1_24],
            [GTETRA2[0],GTETRA2[1],GTETRA2[0],C1_24],
            [GTETRA2[0],GTETRA2[0],GTETRA2[1],C1_24]]

# 楔形1次要素の節点のξ,η,ζ座標
WEDGE1_NODE=[[0,0,-1],[1,0,-1],[0,1,-1],[0,0,1],[1,0,1],[0,1,1]]

# 楔形1次要素の積分点のξ,η,ζ座標,重み係数
WEDGE1_INT=[[C1_3,C1_3,GX2[0],0.5],[C1_3,C1_3,GX2[1],0.5]]

# 楔形2次要素の節点のξ,η,ζ座標
WEDGE2_NODE=[[0,0,-1],[1,0,-1],[0,1,-1],[0,0,1],[1,0,1],[0,1,1],
            [0.5,0,-1],[0.5,0.5,-1],[0,0.5,-1],
            [0.5,0,1],[0.5,0.5,1],[0,0.5,1],
            [0,0,0],[1,0,0],[0,1,0]]

# 楔形2次要素の積分点のξ,η,ζ座標,重み係数
WEDGE2_INT=[[GTRI2[0],GTRI2[0],GX3[0],C1_6*GW3[0]],
            [GTRI2[1],GTRI2[0],GX3[0],C1_6*GW3[0]],
            [GTRI2[0],GTRI2[1],GX3[0],C1_6*GW3[0]],
            [GTRI2[0],GTRI2[0],GX3[1],C1_6*GW3[1]],
            [GTRI2[1],GTRI2[0],GX3[1],C1_6*GW3[1]],
            [GTRI2[0],GTRI2[1],GX3[1],C1_6*GW3[1]],
            [GTRI2[0],GTRI2[0],GX3[2],C1_6*GW3[2]],
            [GTRI2[1],GTRI2[0],GX3[2],C1_6*GW3[2]],
            [GTRI2[0],GTRI2[1],GX3[2],C1_6*GW3[2]]]

# 六面体1次要素の節点のξ,η,ζ座標
HEXA1_NODE=[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],
            [-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]]

# 六面体1次要素の積分点のξ,η,ζ座標,重み係数
HEXA1_INT=[[GX2[0],GX2[0],GX2[0],1],[GX2[1],GX2[0],GX2[0],1],
            [GX2[0],GX2[1],GX2[0],1],[GX2[1],GX2[1],GX2[0],1],
            [GX2[0],GX2[0],GX2[1],1],[GX2[1],GX2[0],GX2[1],1],
            [GX2[0],GX2[1],GX2[1],1],[GX2[1],GX2[1],GX2[1],1]]

# 六面体2次要素の節点のξ,η,ζ座標
HEXA2_NODE=[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],
            [-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1],
            [0,-1,-1],[1,0,-1],[0,1,-1],[-1,0,-1],
            [0,-1,1],[1,0,1],[0,1,1],[-1,0,1],
            [-1,-1,0],[1,-1,0],[1,1,0],[-1,1,0]]

# 六面体2次要素の積分点のξ,η,ζ座標,重み係数
def HEXA2_INT():
    a = []
    for k in range(3):
        for j in range(3):
            for i in range(3):
                a.append([GX3[i], GX3[j], GX3[k], GW3[i] * GW3[j] * GW3[k]] )
    return a

# 六面体1次要素の質量マトリックス係数
def HEXA1_MASS_BASE():
    v = []
    for i in range(8):
        v2 = []
        for j in range(8):
            s = abs( HEXA1_NODE[i][0] - HEXA1_NODE[j][0] ) + \
                abs( HEXA1_NODE[i][1] - HEXA1_NODE[j][1] ) + \
                abs( HEXA1_NODE[i][2] - HEXA1_NODE[j][2] )
            v2.append(math.pow(0.5, 0.5 * s) / 27)
        v.append(v2)
    return v


#--------------------------------------------------------------------#
# ソリッド要素
# label - 要素ラベル
# material - 材料のインデックス
# nodes - 節点番号
# nodeP - 節点のξ,η,ζ座標
# intP - 積分点のξ,η,ζ座標,重み係数
class SolidElement(FElement):
    def __init__(self, label, material, nodes, nodeP, intP, shapeFunction):
        super().__init__(label, material, nodes)
        self.nodeP = nodeP
        self.intP = intP
        self.shapeFunction = shapeFunction

    # ヤコビ行列を返す
    # p - 要素節点
    # sf - 形状関数行列
    def jacobianMatrix(self, p: np.ndarray, sf) -> np.ndarray:
        count = self.nodeCount()
        jac = np.zeros(9)
        for i in range(count):
            sfi = sf[i]
            pix = p[i].x
            piy = p[i].y
            piz = p[i].z
            for j in range(3):
                sfij = sfi[j+1]
                jac[j] += sfij * pix
                jac[j + 3] += sfij * piy
                jac[j + 6] += sfij * piz
        return jac

    # 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
    # p - 要素節点
    # ja - ヤコビ行列
    # sf - 形状関数行列
    def grad(self, p, ja: np.ndarray, sf) -> List[np.ndarray]:
        count = self.nodeCount()
        gr = []
        ji = np.linalg.inv(ja.reshape(3,3)).reshape(9)
        for i in range(count):
            gr.append(np.array([
                ji[0] * sf[i][1] + ji[3] * sf[i][2] + ji[6] * sf[i][3],
                ji[1] * sf[i][1] + ji[4] * sf[i][2] + ji[7] * sf[i][3],
                ji[2] * sf[i][1] + ji[5] * sf[i][2] + ji[8] * sf[i][3]
                ]))
        return gr

    # 歪 - 変位マトリックスの転置行列を返す
    # grad - 形状関数の勾配
    def strainMatrix(self, grad) -> np.ndarray:
        count = self.nodeCount()
        m = np.zeros((3*count, 6))
        for i in range(count):
            i3 = 3 * i
            gr = grad[i]
            m[i3][0] = gr[0]
            m[i3+1][1] = gr[1]
            m[i3+2][2] = gr[2]
            m[i3][3] = gr[1]
            m[i3+1][3] = gr[0]
            m[i3+1][4] = gr[2]
            m[i3+2][4] = gr[1]
            m[i3][5] = gr[2]
            m[i3+2][5] = gr[0]

        return m

    # 積分点の形状関数マトリックス [ NiNj ] を返す
    # p - 要素節点
    # x - ξ,η,ζ座標
    # w - 重み係数
    def shapePart(self, p,x,w):
        sf = self.shapeFunction(x[0], x[1], x[2])
        ja = self.jacobianMatrix(p,sf)
        count = self.nodeCount()
        matrix = []
        coef = w * abs(np.linalg.det(ja))
        for i in range(count):
            matr = []
            cf2 = coef * sf[i][0]
            for j in range(count):
                matr[j] = cf2 * sf[j][0]

            matrix[i] = matr

        return matrix

    # 積分点の拡散マトリックス [ ∇Ni・∇Nj ] を返す
    # p - 要素節点
    # x - ξ,η,ζ座標
    # w - 重み係数
    def gradPart(self, p, x, w):
        sf = self.shapeFunction(x[0], x[1], x[2])
        ja = self.jacobianMatrix(p, sf)
        gr = self.grad(p,ja,sf)
        count = self.nodeCount()
        matrix = []
        coef = w * abs(np.linalg.det(ja))
        for i in range(count):
            matr = []
            gri = gr[i]
            c1 = coef * gri[0]
            c2 = coef * gri[1]
            c3 = coef * gri[2]
            for j in range(count):
                grj = gr[j]
                matr[j] = c1 * grj[0] + c2 * grj[1] + c3 * grj[2]

            matrix[i]=matr

        return matrix


    # 質量マトリックスを返す
    # p - 要素節点
    # dens - 材料の密度
    def massMatrix(self, p, dens):
        count = self.nodeCount()
        m = np.zeros((3*count,3*count))
        for i in range(len(self.intP)):
            sf = self.shapeFunction( self.intP[i][0], self.intP[i][1], self.intP[i][2] )
            ja = self.jacobianMatrix(p,sf)
            coef = self.intP[i][3] * dens*abs(np.linalg.det(ja))
            for i1 in range(count):
                for j1 in range(count):
                    value = coef * sf[i1][0] * sf[j1][0]
                    i3 = 3 * i1
                    j3 = 3 * j1
                    m[i3][j3] += value
                    m[i3+1][j3+1] += value
                    m[i3+2][j3+2] += value

        return m


    # 剛性マトリックスを返す
    # p - 要素節点
    # d1 - 応力 - 歪マトリックス
    def stiffnessMatrix(self, p, d1):
        count = 3 * self.nodeCount()
        kk = np.zeros((count,count))
        for i in range(len(self.intP)):
            ip = self.intP[i]
            sf = self.shapeFunction(ip[0], ip[1], ip[2])
            ja = self.jacobianMatrix(p, sf)
            jd = np.linalg.det(ja.reshape(3,3))
            jp = ip[3] * abs(jd)
            ks = self.stiffPart(d1, self.strainMatrix(self.grad(p, ja, sf)), jp)
            addMatrix(kk, ks)

        return kk


    # 形状関数マトリックス [ ∫NiNjdV ] を返す
    # p - 要素節点
    # coef - 係数
    def shapeFunctionMatrix(self, p, coef):
        count = self.nodeCount()
        s = np.zeros((count,count))
        for i in range(len(self.intP)):
            addMatrix(s, self.shapePart(p, self.intP[i], coef * self.intP[i][3]))

        return s


    # 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
    # p - 要素節点
    # coef - 係数
    def gradMatrix(self, p, coef):
        count = self.nodeCount()
        g = np.zeros((count,count))
        for i in range(len(self.intP)):
            addMatrix(g, self.gradPart(p, self.intP[i], coef * self.intP[i][3]))

        return g


    # 幾何剛性マトリックスを返す
    # p - 要素節点
    # u - 節点変位
    # d1 - 応力 - 歪マトリックス
    def geomStiffnessMatrix(self, p, u, d1):
        count = self.nodeCount()
        kk = np.zeros((3*count,3*count))
        v = self.toArray(u, 3)
        for i in range(len(self.intP)):
            ip = self.intP[i]
            sf = self.shapeFunction(ip[0], ip[1], ip[2])
            ja = self.jacobianMatrix(p, sf)
            gr = self.grad(p, ja, sf)
            sm = self.strainMatrix(gr)
            str = np.dot(d1, np.dot(v, sm))
            w = ip[3] * abs(np.linalg.det(ja))
            for i1 in range(count):
                i3 = 3 * i1
                gri = gr[i1]
                for j1 in range(count):
                    j3 = 3 * j1
                    grj = gr[j1]
                    s = w ( gri[0] * ( str[0] * grj[0] + str[3] * grj[1] + str[5] * grj[2] ) +
                            gri[1] * ( str[3] * grj[0] + str[1] * grj[1] + str[4] * grj[2] ) +
                            gri[2] * ( str[5] * grj[0] + str[4] * grj[1] + str[2] * grj[2] ))
                    kk[i3][j3] += s
                    kk[i3+1][j3+1] += s
                    kk[i3+2][j3+2] += s

        return kk


    # 節点歪・応力を返す
    # p - 要素節点
    # u - 節点変位
    # d1 - 応力 - 歪マトリックス
    def strainStress(self, p, u, d1):
        count = self.nodeCount()
        v = self.toArray(u, 3)
        strain = []
        stress = []
        energy = []
        for i in range(count):
            eps = self.strainPart(p, v, self.nodeP[i])
            strain.append(Strain(eps))
            str = np.dot(d1, eps)
            stress.append(Stress(str))
            energy.append(0.5 * np.inner(eps, str))

        return [strain, stress, energy]


    # 要素内の歪ベクトルを返す
    # p - 要素節点
    # v - 節点変位ベクトル
    # x - ξ,η,ζ座標
    def strainPart(self, p, v, x):
        sf = self.shapeFunction(x[0], x[1], x[2])
        ja = self.jacobianMatrix(p, sf)
        sm = self.strainMatrix(self.grad(p, ja, sf))
        return np.dot(v, sm)


    # 要素歪・応力を返す
    # p - 要素節点
    # u - 節点変位
    # d1 - 応力 - 歪マトリックス
    def elementStrainStress(self, p, u, d1):
        v = self.toArray(u,3)
        cf = 1 / len(self.intP)
        strain = [0, 0, 0, 0, 0, 0]
        stress = [0, 0, 0, 0, 0, 0]
        energy = 0
        for i in range(len(self.intP)):
            eps = self.strainPart(p, v, self.intP[i])
            strain = strain + eps
            str1 = np.dot(d1, eps)
            stress = stress + str1
            energy += np.dot(eps, str1)

        strain = np.multiply(strain, cf)
        stress = np.multiply(stress, cf)
        energy *= 0.5 * cf
        return [Strain(strain), Stress(stress), energy]


    # 要素を表す文字列を返す
    # materials - 材料
    # p - 節点
    def toString(self, materials, p):
        s = self.getName() + '\t' + self.label.toString(10) + '\t' + \
                    materials[self.material].label.toString(10)
        for i in range(len(self.nodes)):
            s += '\t' + p[self.nodes[i]].label.toString(10)

        return s


#--------------------------------------------------------------------#
# 四面体1次要素
# label - 要素ラベル
# material - 材料のインデックス
# nodes - 節点番号
class TetraElement1(SolidElement):
    def __init__(self, label, material, nodes):
        super().__init__(label, material, nodes, None, None, self.shapeFunction)


    # 要素名称を返す
    def getName(self):
        return 'TetraElement1'

    # 節点数を返す
    def nodeCount(self):
        return 4

    # 要素境界数を返す
    def borderCount(self):
        return 4

    # 要素境界を返す
    # element - 要素ラベル
    # index - 要素境界のインデックス
    def border(self, element, index):
        p = self.nodes
        if(index == 0):
            return TriangleBorder1(element, [p[0], p[2], p[1]] )
        elif(index == 1):
            return TriangleBorder1(element, [p[0], p[1], p[3]] )
        elif(index == 2):
            return TriangleBorder1(element, [p[1], p[2], p[3]] )
        elif(index == 3):
            return TriangleBorder1(element, [p[2], p[0], p[3]] )

        return None


    # 要素を鏡像反転する
    def mirror(self):
        swap(self.nodes, 1, 2)


    # 要素節点の角度を返す
    # p - 要素節点
    def angle(self, p) -> np.ndarray:
        th = np.zeros(4)
        for i in range(4):
            th[i] = solidAngle( p[i], p[(i+1)%4], p[(i+2)%4], p[(i+3)%4] )

        return th


    # 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
    # xsi,eta,zeta - 要素内部ξ,η,ζ座標
    def shapeFunction(self, xsi, eta, zeta):
        return [
            [ 1 - xsi - eta - zeta, -1, -1, -1 ],
            [ xsi, 1, 0, 0 ],
            [ eta, 0, 1, 0 ],
            [ zeta, 0, 0, 1 ]]

    # ヤコビアンを返す
    # p - 要素節点
    def jacobian(self, p):
        p0x = p[0].x
        p0y = p[0].y
        p0z = p[0].z
        j11 = ( p[2].y - p0y ) * ( p[3].z - p0z ) - ( p[3].y -p0y ) * ( p[2].z - p0z )
        j21 = ( p[3].y - p0y ) * ( p[1].z - p0z ) - ( p[1].y -p0y ) * ( p[3].z - p0z )
        j31 = ( p[1].y - p0y ) * ( p[2].z - p0z ) - ( p[2].y -p0y ) * ( p[1].z - p0z )
        return ( p[1].x - p0x ) * j11 + ( p[2].x - p0x ) * j21 + ( p[3].x - p0x ) * j31


    # 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
    # p - 要素節点
    # ja - ヤコビアン
    def grad(self, p, ja):
        count = self.nodeCount()
        gr = []
        ji = 1 / ja
        for i in range(count):
            ji =- ji
            i2 = (i+1)%4
            i3 = (i+2)%4
            i4 = (i+3)%4
            p2x = p[i2].x
            p2y = p[i2].y
            p2z = p[i2].z
            gr[i]=[
                ji * ( ( p[i3].y - p2y ) * ( p[i4].z - p2z )-
                             ( p[i4].y - p2y ) * ( p[i3].z - p2z )),
                ji * ( ( p[i3].z - p2z ) * ( p[i4].x - p2x )-
                             ( p[i4].z - p2z ) * ( p[i3].x - p2x )),
                ji * ( ( p[i3].x - p2x ) * ( p[i4].y - p2y )-
                             ( p[i4].x - p2x ) * ( p[i3].y - p2y ))
            ]

        return gr


    # 質量マトリックスを返す
    # p - 要素節点
    # dens - 材料の密度
    def massMatrix(self, p, dens):
        m = np.zeros((12,12))
        value = dens * self.jacobian(p)/60
        vh = 0.5 * value
        for i in range(12, 3):
            m[i][i] = value
            m[i+1][i+1] = value
            m[i+2][i+2] = value
            for j in range(i + 3, 12, 3):
                m[i][j] = vh
                m[i+1][j+1] = vh
                m[i+2][j+2] = vh
                m[j][i] = vh
                m[j+1][i+1] = vh
                m[j+2][i+2] = vh

        return m


    # 剛性マトリックスを返す
    # p - 要素節点
    # d1 - 応力 - 歪マトリックス
    def stiffnessMatrix(self, p, d1):
        ja = self.jacobian(p)
        return self.stiffPart( d1, self.strainMatrix(self.grad(p,ja)),
                                                    C1_6 * abs(ja))


    # 形状関数マトリックス [ ∫NiNjdV ] を返す
    # p - 要素節点
    # coef - 係数
    def shapeFunctionMatrix(self, p, coef):
        value = 0.1 * C1_6 * coef * abs( self.jacobian(p) )
        vh = 0.5 * value
        return [
            [ value, vh, vh, vh ],
            [ vh, value, vh, vh ],
            [ vh, vh, value, vh ],
            [ vh, vh, vh, value ]
        ]


    # 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
    # p - 要素節点
    # coef - 係数
    def gradMatrix(self, p, coef):
        g = []
        ja = self.jacobian(p)
        gr =self.grad(p, ja)
        coef2 = C1_6 * coef * abs(ja)
        for i in range(4):
            g[i] = []
            for j in range(4):
                g[i][j] = coef2 * ( 
                    gr[i][0] * gr[j][0] + gr[i][1] * gr[j][1] + gr[i][2] * gr[j][2]
                )

        return g


    # 幾何剛性マトリックスを返す
    # p - 要素節点
    # u - 節点変位
    # d1 - 応力 - 歪マトリックス
    def geomStiffnessMatrix(self, p, u, d1):
        count = self.nodeCount()
        kk = np.zeros((3*count,3*count))
        ja = self.jacobianMatrix(p)
        gr = self.grad(p, ja)
        sm = self.strainMatrix(gr)
        str = np.dot(d1, np.dot( self.toArray(u,3), sm ))
        w = C1_6 * abs(ja)
        for i1 in range(count):
            i3 = 3 * i1
            gri = gr[i1]
            for j1 in range(count):
                j3 = 3 * j1
                grj = gr[j1]
                s = w * (
                    gri[0] * ( str[0] * grj[0] + str[3] * grj[1] + str[5] * grj[2] ) +
                    gri[1] * ( str[3] * grj[0] + str[1] * grj[1] + str[4] * grj[2] ) +
                    gri[2] * ( str[5] * grj[0] + str[4] * grj[1] + str[2] * grj[2] )
                )
                kk[i3][j3] = s
                kk[i3+1][j3+1] = s
                kk[i3+2][j3+2] = s

        return kk


    # 節点歪・応力を返す
    # p - 要素節点
    # u - 節点変位
    # d1 - 応力 - 歪マトリックス
    def strainStress(self, p, u, d1):
        sm = self.strainMatrix( self.grad( p, self.jacobian(p)))
        eps = np.dot(self.toArray(u,3), sm)
        strain = Strain(eps)
        str = np.dot(d1, eps)
        stress = Stress(str)
        energy = 0.5 * strain.innerProduct(stress)
        return [
            [ strain, strain, strain, strain ],
            [ stress, stress, stress, stress ],
            [ energy, energy, energy, energy ]]


    # 要素歪・応力を返す
    # p - 要素節点
    # u - 節点変位
    # d1 - 応力 - 歪マトリックス
    def elementStrainStress(self, p, u, d1):
        sm = self.strainMatrix(self.grad(p,self.jacobian(p)))
        eps = np.dot(self.toArray(u,3),sm)
        str = np.dot(d1,eps)
        energy = 0.5 * np.dot(eps,str)
        return [ Strain(eps), Stress(str), energy ]


#--------------------------------------------------------------------#
# 四面体2次要素
# label - 要素ラベル
# material - 材料のインデックス
# nodes - 節点番号
class TetraElement2(SolidElement):
    def __init__(self, label, material, nodes):
        super().__init__(label, material, nodes, TETRA2_NODE, TETRA2_INT, self.shapeFunction)


    # 要素名称を返す
    def getName(self):
        return 'TetraElement2'

    # 節点数を返す
    def nodeCount(self):
        return 10

    # 要素境界数を返す
    def borderCount(self):
        return 4

    # 要素境界を返す
    # element - 要素ラベル
    # index - 要素境界のインデックス
    def border(self, element, index):
        p = self.nodes
        if (index == 0):
            return TriangleBorder2(element,[p[0],p[2],p[1],p[6],p[5],p[4]])
        elif ( index == 1 ):
            return TriangleBorder2(element,[p[0],p[1],p[3],p[4],p[8],p[7]])
        elif ( index == 2):
            return TriangleBorder2(element,[p[1],p[2],p[3],p[5],p[9],p[8]])
        elif ( index == 3):
            return TriangleBorder2(element,[p[2],p[0],p[3],p[6],p[7],p[9]])

        return None


    # 要素を鏡像反転する
    def mirror(self):
        swap(self.nodes, 1, 2)
        swap(self.nodes, 4, 6)
        swap(self.nodes, 8, 9)


    # 要素節点の角度を返す
    # p - 要素節点
    def angle(self, p) -> np.ndarray:
        return np.array([
            solidAngle(p[0], p[4], p[6], p[7]),
            solidAngle(p[1], p[5], p[4], p[8]),
            solidAngle(p[2], p[6], p[5], p[9]),
            solidAngle(p[3], p[7], p[8], p[9]),
            planeAngle(p[4], p[2], p[3]), planeAngle(p[5],p[0],p[3]),
            planeAngle(p[6], p[1], p[3]), planeAngle(p[7],p[1],p[2]),
            planeAngle(p[8], p[2], p[0]), planeAngle(p[9],p[0],p[1])])


    # 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
    # xsi,eta,zeta - 要素内部ξ,η,ζ座標
    def shapeFunction(self, xsi, eta, zeta):
        xez = 1 - xsi - eta - zeta
        return [[xez * (2 * xez - 1), 1 - 4 * xez, 1 - 4 * xez, 1 - 4 * xez],
                [xsi * (2 * xsi - 1), 4 * xsi - 1, 0, 0],
                [eta * (2 * eta - 1), 0, 4 * eta - 1, 0],
                [zeta * (2 * zeta - 1), 0, 0, 4 * zeta - 1],
                [4 * xez * xsi, 4 * (xez - xsi), -4 * xsi, -4 * xsi],
                [4 * xsi * eta, 4 * eta, 4 * xsi, 0],
                [4 * xez * eta, -4 * eta, 4 * (xez - eta), -4 * eta],
                [4 * xez * zeta, -4 * zeta, -4 * zeta, 4 * (xez - zeta)],
                [4 * xsi * zeta, 4 * zeta, 0, 4 * xsi],
                [4 * eta * zeta, 0, 4 * zeta, 4 * eta]]


#--------------------------------------------------------------------#
# 楔形1次要素
# label - 要素ラベル
# material - 材料のインデックス
# nodes - 節点番号
class WedgeElement1(SolidElement):
    def __init__(self, label, material, nodes):
        super().__init__(label, material, nodes, WEDGE1_NODE, WEDGE1_INT, self.shapeFunction)

    # 要素名称を返す
    def getName(self):
        return 'WedgeElement1'

    # 節点数を返す
    def nodeCount(self):
        return 6

    # 要素境界数を返す
    def borderCount(self):
        return 5

    # 要素境界を返す
    # element - 要素ラベル
    # index - 要素境界のインデックス
    def border(self, element, index):
        p = self.nodes
        if(index == 0):
            return TriangleBorder1(element,[p[0],p[2],p[1]])
        elif(index == 1):
            return TriangleBorder1(element,[p[3],p[4],p[5]])
        elif(index == 2):
            return QuadangleBorder1(element,[p[0],p[1],p[4],p[3]])
        elif(index == 3):
            return QuadangleBorder1(element,[p[1],p[2],p[5],p[4]])
        elif(index == 4):
            return QuadangleBorder1(element,[p[2],p[0],p[3],p[5]])

        return None


    # 要素を鏡像反転する
    def mirror(self):
        swap(self.nodes,1,2)
        swap(self.nodes,4,5)

    # 要素節点の角度を返す
    # p - 要素節点
    def angle(self, p) -> np.ndarray:
        th = np.zeros(6)
        for i in range(3):
            th[i] = solidAngle( p[i], p[(i+1)%3], p[(i+2)%3], p[i+3])
            th[i+3] = solidAngle( p[i+3], p[(i+1)%3+3], p[(i+2)%3+3], p[i])

        return th


    # 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
    # xsi,eta,zeta - 要素内部ξ,η,ζ座標
    def shapeFunction(self, xsi, eta, zeta):
        return [[0.5*(1-xsi-eta)*(1-zeta),-0.5*(1-zeta),-0.5*(1-zeta),
                        -0.5*(1-xsi-eta)],
                        [0.5*xsi*(1-zeta),0.5*(1-zeta),0,-0.5*xsi],
                        [0.5*eta*(1-zeta),0,0.5*(1-zeta),-0.5*eta],
                        [0.5*(1-xsi-eta)*(1+zeta),-0.5*(1+zeta),-0.5*(1+zeta),
                        0.5*(1-xsi-eta)],
                        [0.5*xsi*(1+zeta),0.5*(1+zeta),0,0.5*xsi],
                        [0.5*eta*(1+zeta),0,0.5*(1+zeta),0.5*eta]]


    # 質量マトリックスを返す
    # p - 要素節点
    # dens - 材料の密度
    def massMatrix(self, p, dens):
        ja = 0
        for i in range(2):
            sf=self.shapeFunction(
                WEDGE1_INT[i][0], WEDGE1_INT[i][1], WEDGE1_INT[i][2])
            ja += np.linalg.det(self.jacobianMatrix(p, sf))

        v0 = dens * ja / 36
        m = np.zeros((18,18))
        for i in range(6):
            for j in range(6):
                value = v0
                i3 = 3 * i
                j3 = 3 * j
                if( i%3 != j%3 ):
                    value *= 0.5
                if( math.floor(i/3) != math.floor(j/3) ):
                    value *= 0.5
                m[i3][j3] += value
                m[i3+1][j3+1] += value
                m[i3+2][j3+2] += value

        return m


#--------------------------------------------------------------------#
# 楔形2次要素
# label - 要素ラベル
# material - 材料のインデックス
# nodes - 節点番号
class WedgeElement2(SolidElement):
    def __init__(self, label, material, nodes):
        super().__init__(label, material, nodes, WEDGE2_NODE, WEDGE2_INT, self.shapeFunction)

    # 要素名称を返す
    def getName(swlf):
        return 'WedgeElement2'

    # 節点数を返す
    def nodeCount(self):
        return 15

    # 要素境界数を返す
    def borderCount(self):
        return 5

    # 要素境界を返す
    # element - 要素ラベル
    # index - 要素境界のインデックス
    def border(self, element, index):
        p = self.nodes
        if(index == 0):
            return TriangleBorder2(element,[p[0],p[2],p[1],p[8],p[7],p[6]])
        elif(index == 1):
            return TriangleBorder2(element,[p[3],p[4],p[5],p[9],p[10],p[11]])
        elif(index == 2):
            return QuadangleBorder2(element,[p[0],p[1],p[4],p[3],p[6],p[13],p[9],p[12]])
        elif(index == 3):
            return QuadangleBorder2(element,[p[1],p[2],p[5],p[4],p[7],p[14],p[10],p[13]])
        elif(index == 4):
            return QuadangleBorder2(element,[p[2],p[0],p[3],p[5],p[8],p[12],p[11],p[14]])

        return None


    # 要素を鏡像反転する
    def mirror(self):
        swap(self.nodes,1,2)
        swap(self.nodes,4,5)
        swap(self.nodes,6,8)
        swap(self.nodes,9,11)
        swap(self.nodes,13,14)


    # 要素節点の角度を返す
    # p - 要素節点
    def angle(self, p) -> np.ndarray:
        return np.array([
            solidAngle(p[0],  p[6],  p[8],   p[12]),
            solidAngle(p[1],  p[6],  p[7],   p[14]),
            solidAngle(p[2],  p[7],  p[8],   p[14]),
            solidAngle(p[3],  p[9],  p[11],  p[12]),
            solidAngle(p[4],  p[10], p[9],   p[13]),
            solidAngle(p[5],  p[11], p[10],  p[14]),
            planeAngle(p[6],  p[2],  p[9]),  planeAngle(p[7],  p[0],  p[10]),
            planeAngle(p[8],  p[1],  p[11]), planeAngle(p[9],  p[6],  p[5]),
            planeAngle(p[10], p[7],  p[3]),  planeAngle(p[11], p[8],  p[4]),
            planeAngle(p[12], p[13], p[14]), planeAngle(p[13], p[14], p[12]),
            planeAngle(p[14], p[12], p[13])])


    # 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
    # xsi,eta,zeta - 要素内部ξ,η,ζ座標
    def shapeFunction(self, xsi, eta, zeta):
        xe = 1 - xsi - eta
        return [[xe*(xe-0.5*zeta-1)*(1-zeta),-(2*xe-0.5*zeta-1)*(1-zeta),
                        -(2*xe-0.5*zeta-1)*(1-zeta),xe*(zeta-xe+0.5)],
                        [xsi*(xsi-0.5*zeta-1)*(1-zeta),(2*xsi-0.5*zeta-1)*(1-zeta),
                        0,xsi*(zeta-xsi+0.5)],
                        [eta*(eta-0.5*zeta-1)*(1-zeta),0,
                        (2*eta-0.5*zeta-1)*(1-zeta),eta*(zeta-eta+0.5)],
                        [xe*(xe+0.5*zeta-1)*(1+zeta),-(2*xe+0.5*zeta-1)*(1+zeta),
                        -(2*xe+0.5*zeta-1)*(1+zeta),xe*(zeta+xe-0.5)],
                        [xsi*(xsi+0.5*zeta-1)*(1+zeta),(2*xsi+0.5*zeta-1)*(1+zeta),
                        0,xsi*(zeta+xsi-0.5)],
                        [eta*(eta+0.5*zeta-1)*(1+zeta),0,
                        (2*eta+0.5*zeta-1)*(1+zeta),eta*(zeta+eta-0.5)],
                        [2*xe*xsi*(1-zeta),2*(xe-xsi)*(1-zeta),-2*xsi*(1-zeta),
                        -2*xe*xsi],
                        [2*xsi*eta*(1-zeta),2*eta*(1-zeta),2*xsi*(1-zeta),
                        -2*xsi*eta],
                        [2*eta*xe*(1-zeta),-2*eta*(1-zeta),2*(xe-eta)*(1-zeta),
                        -2*eta*xe],
                        [2*xe*xsi*(1+zeta),2*(xe-xsi)*(1+zeta),-2*xsi*(1+zeta),
                        2*xe*xsi],
                        [2*xsi*eta*(1+zeta),2*eta*(1+zeta),2*xsi*(1+zeta),
                        2*xsi*eta],
                        [2*eta*xe*(1+zeta),-2*eta*(1+zeta),2*(xe-eta)*(1+zeta),
                        2*eta*xe],
                        [xe*(1+zeta)*(1-zeta),-(1+zeta)*(1-zeta),-(1+zeta)*(1-zeta),
                        -2*xe*zeta],
                        [xsi*(1+zeta)*(1-zeta),(1+zeta)*(1-zeta),0,-2*xsi*zeta],
                        [eta*(1+zeta)*(1-zeta),0,(1+zeta)*(1-zeta),-2*eta*zeta]]


#--------------------------------------------------------------------#
# 六面体1次要素
# label - 要素ラベル
# material - 材料のインデックス
# nodes - 節点番号
class HexaElement1(SolidElement):
    def __init__(self, label, material, nodes):
        super().__init__(label, material, nodes, HEXA1_NODE, HEXA1_INT, self.shapeFunction)

    # 要素名称を返す
    def getName(self):
        return 'HexaElement1'

    # 節点数を返す
    def nodeCount(self):
        return 8

    # 要素境界数を返す
    def borderCount(self):
        return 6

    # 要素境界を返す
    # element - 要素ラベル
    # index - 要素境界のインデックス
    def border(self, element,index):
        p = self.nodes
        if(index == 0):
            return QuadangleBorder1(element,[p[0],p[3],p[2],p[1]])
        elif(index == 1):
            return QuadangleBorder1(element,[p[4],p[5],p[6],p[7]])
        elif(index == 2):
            return QuadangleBorder1(element,[p[0],p[1],p[5],p[4]])
        elif(index == 3):
            return QuadangleBorder1(element,[p[1],p[2],p[6],p[5]])
        elif(index == 4):
            return QuadangleBorder1(element,[p[2],p[3],p[7],p[6]])
        elif(index == 5):
            return QuadangleBorder1(element,[p[3],p[0],p[4],p[7]])

        return None


    # 要素を鏡像反転する
    def mirror(self):
        swap(self.nodes, 1, 3)
        swap(self.nodes, 5, 7)


    # 要素節点の角度を返す
    # p - 要素節点
    def angle(self, p) -> np.ndarray:
        th = np.zeros(8)
        for i in range(4):
            th[i] = solidAngle(p[i],p[(i+1)%4],p[(i+3)%4],p[i+4])
            th[i+4] = solidAngle(p[i+4],p[(i+1)%4+4],p[(i+3)%4+4],p[i])

        return th


    # 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
    # xsi,eta,zeta - 要素内部ξ,η,ζ座標
    def shapeFunction(self, xsi, eta, zeta):
        return [[0.125*(1-xsi)*(1-eta)*(1-zeta),-0.125*(1-eta)*(1-zeta),
                            -0.125*(1-xsi)*(1-zeta),-0.125*(1-xsi)*(1-eta)],
                        [0.125*(1+xsi)*(1-eta)*(1-zeta),0.125*(1-eta)*(1-zeta),
                            -0.125*(1+xsi)*(1-zeta),-0.125*(1+xsi)*(1-eta)],
                        [0.125*(1+xsi)*(1+eta)*(1-zeta),0.125*(1+eta)*(1-zeta),
                            0.125*(1+xsi)*(1-zeta),-0.125*(1+xsi)*(1+eta)],
                        [0.125*(1-xsi)*(1+eta)*(1-zeta),-0.125*(1+eta)*(1-zeta),
                            0.125*(1-xsi)*(1-zeta),-0.125*(1-xsi)*(1+eta)],
                        [0.125*(1-xsi)*(1-eta)*(1+zeta),-0.125*(1-eta)*(1+zeta),
                            -0.125*(1-xsi)*(1+zeta),0.125*(1-xsi)*(1-eta)],
                        [0.125*(1+xsi)*(1-eta)*(1+zeta),0.125*(1-eta)*(1+zeta),
                            -0.125*(1+xsi)*(1+zeta),0.125*(1+xsi)*(1-eta)],
                        [0.125*(1+xsi)*(1+eta)*(1+zeta),0.125*(1+eta)*(1+zeta),
                            0.125*(1+xsi)*(1+zeta),0.125*(1+xsi)*(1+eta)],
                        [0.125*(1-xsi)*(1+eta)*(1+zeta),-0.125*(1+eta)*(1+zeta),
                            0.125*(1-xsi)*(1+zeta),0.125*(1-xsi)*(1+eta)]]

    # 質量マトリックスを返す
    # p - 要素節点
    # dens - 材料の密度
    def massMatrix(self, p, dens):
        ja = 0
        for i in range(8):
            sf = self.shapeFunction(HEXA1_INT[i][0],HEXA1_INT[i][1],
                                                                HEXA1_INT[i][2])
            ja += abs(np.linalg.det(self.jacobianMatrix(p,sf)))

        coef = dens * ja
        m = np.zeros((24,24))
        for i in range(8):
            i3 = 3 * i
            for j in range(8):
                value = coef * HEXA1_MASS_BASE[i][j]
                j3 = 3 * j
                m[i3][j3] += value
                m[i3+1][j3+1] += value
                m[i3+2][j3+2] += value

        return m


#--------------------------------------------------------------------#
# 六面体セレンディピティ族2次要素
# label - 要素ラベル
# material - 材料のインデックス
# nodes - 節点番号
class HexaElement2(SolidElement):
    def __init__(self, label, material, nodes):
        super().__init__(label, material, nodes, HEXA2_NODE, HEXA2_INT(), self.shapeFunction)

    # 要素名称を返す
    def getName(self):
        return 'HexaElement2'

    # 節点数を返す
    def nodeCount(self):
        return 20

    # 要素境界数を返す
    def borderCount(self):
        return 6

    # 要素境界を返す
    # element - 要素ラベル
    # index - 要素境界のインデックス
    def border(self, element, index):
        p = self.nodes
        if(index== 0):
                return QuadangleBorder2(element,[p[0],p[3],p[2],p[1],p[11],p[10],p[9],p[8]])
        elif(index == 1):
                return QuadangleBorder2(element,[p[4],p[5],p[6],p[7],p[12],p[13],p[14],p[15]])
        elif(index == 2):
                return QuadangleBorder2(element,[p[0],p[1],p[5],p[4],p[8],p[17],p[12],p[16]])
        elif(index == 3):
                return QuadangleBorder2(element,[p[1],p[2],p[6],p[5],p[9],p[18],p[13],p[17]])
        elif(index == 4):
                return QuadangleBorder2(element,[p[2],p[3],p[7],p[6],p[10],p[19],p[14],p[18]])
        elif(index == 5):
                return QuadangleBorder2(element,[p[3],p[0],p[4],p[7],p[11],p[16],p[15],p[19]])

        return None


    # 要素を鏡像反転する
    def mirror(self):
        swap(self.nodes,1,3)
        swap(self.nodes,5,7)
        swap(self.nodes,8,11)
        swap(self.nodes,9,10)
        swap(self.nodes,12,15)
        swap(self.nodes,13,14)
        swap(self.nodes,17,19)

    # 要素節点の角度を返す
    # p - 要素節点
    def angle(self, p) -> np.ndarray:
        return np.array([
            solidAngle(p[0],  p[8],  p[11],  p[16]),
            solidAngle(p[1],  p[9],  p[8],   p[17]),
            solidAngle(p[2],  p[10], p[9],   p[18]),
            solidAngle(p[3],  p[11], p[10],  p[19]),
            solidAngle(p[4],  p[12], p[15],  p[16]),
            solidAngle(p[5],  p[13], p[12],  p[17]),
            solidAngle(p[6],  p[14], p[13],  p[18]),
            solidAngle(p[7],  p[15], p[14],  p[19]),
            planeAngle(p[8],  p[12], p[10]), planeAngle(p[9],  p[13], p[11]),
            planeAngle(p[10], p[14], p[8]),  planeAngle(p[11], p[15], p[9]),
            planeAngle(p[12], p[8],  p[14]), planeAngle(p[13], p[9],  p[15]),
            planeAngle(p[14], p[10], p[12]), planeAngle(p[15], p[11], p[13]),
            planeAngle(p[16], p[17], p[19]), planeAngle(p[17], p[18], p[16]),
            planeAngle(p[18], p[19], p[17]), planeAngle(p[19], p[16], p[18])])


    # 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
    # xsi,eta,zeta - 要素内部ξ,η,ζ座標
    def shapeFunction(self, xsi, eta, zeta):
        return [[0.125*(1-xsi)*(1-eta)*(1-zeta)*(-xsi-eta-zeta-2),
                        0.125*(1-eta)*(1-zeta)*(2*xsi+eta+zeta+1),
                        0.125*(1-xsi)*(1-zeta)*(xsi+2*eta+zeta+1),
                        0.125*(1-xsi)*(1-eta)*(xsi+eta+2*zeta+1)],
                        [0.125*(1+xsi)*(1-eta)*(1-zeta)*(xsi-eta-zeta-2),
                        0.125*(1-eta)*(1-zeta)*(2*xsi-eta-zeta-1),
                        0.125*(1+xsi)*(1-zeta)*(-xsi+2*eta+zeta+1),
                        0.125*(1+xsi)*(1-eta)*(-xsi+eta+2*zeta+1)],
                        [0.125*(1+xsi)*(1+eta)*(1-zeta)*(xsi+eta-zeta-2),
                        0.125*(1+eta)*(1-zeta)*(2*xsi+eta-zeta-1),
                        0.125*(1+xsi)*(1-zeta)*(xsi+2*eta-zeta-1),
                        0.125*(1+xsi)*(1+eta)*(-xsi-eta+2*zeta+1)],
                        [0.125*(1-xsi)*(1+eta)*(1-zeta)*(-xsi+eta-zeta-2),
                        0.125*(1+eta)*(1-zeta)*(2*xsi-eta+zeta+1),
                        0.125*(1-xsi)*(1-zeta)*(-xsi+2*eta-zeta-1),
                        0.125*(1-xsi)*(1+eta)*(xsi-eta+2*zeta+1)],
                        [0.125*(1-xsi)*(1-eta)*(1+zeta)*(-xsi-eta+zeta-2),
                        0.125*(1-eta)*(1+zeta)*(2*xsi+eta-zeta+1),
                        0.125*(1-xsi)*(1+zeta)*(xsi+2*eta-zeta+1),
                        0.125*(1-xsi)*(1-eta)*(-xsi-eta+2*zeta-1)],
                        [0.125*(1+xsi)*(1-eta)*(1+zeta)*(xsi-eta+zeta-2),
                        0.125*(1-eta)*(1+zeta)*(2*xsi-eta+zeta-1),
                        0.125*(1+xsi)*(1+zeta)*(-xsi+2*eta-zeta+1),
                        0.125*(1+xsi)*(1-eta)*(xsi-eta+2*zeta-1)],
                        [0.125*(1+xsi)*(1+eta)*(1+zeta)*(xsi+eta+zeta-2),
                        0.125*(1+eta)*(1+zeta)*(2*xsi+eta+zeta-1),
                        0.125*(1+xsi)*(1+zeta)*(xsi+2*eta+zeta-1),
                        0.125*(1+xsi)*(1+eta)*(xsi+eta+2*zeta-1)],
                        [0.125*(1-xsi)*(1+eta)*(1+zeta)*(-xsi+eta+zeta-2),
                        0.125*(1+eta)*(1+zeta)*(2*xsi-eta-zeta+1),
                        0.125*(1-xsi)*(1+zeta)*(-xsi+2*eta+zeta-1),
                        0.125*(1-xsi)*(1+eta)*(-xsi+eta+2*zeta-1)],
                        [0.25*(1+xsi)*(1-xsi)*(1-eta)*(1-zeta),
                        -0.5*xsi*(1-eta)*(1-zeta),
                        -0.25*(1+xsi)*(1-xsi)*(1-zeta),
                        -0.25*(1+xsi)*(1-xsi)*(1-eta)],
                        [0.25*(1+xsi)*(1+eta)*(1-eta)*(1-zeta),
                        0.25*(1+eta)*(1-eta)*(1-zeta),
                        -0.5*(1+xsi)*eta*(1-zeta),
                        -0.25*(1+xsi)*(1+eta)*(1-eta)],
                        [0.25*(1+xsi)*(1-xsi)*(1+eta)*(1-zeta),
                        -0.5*xsi*(1+eta)*(1-zeta),
                        0.25*(1+xsi)*(1-xsi)*(1-zeta),
                        -0.25*(1+xsi)*(1-xsi)*(1+eta)],
                        [0.25*(1-xsi)*(1+eta)*(1-eta)*(1-zeta),
                        -0.25*(1+eta)*(1-eta)*(1-zeta),
                        -0.5*(1-xsi)*eta*(1-zeta),
                        -0.25*(1-xsi)*(1+eta)*(1-eta)],
                        [0.25*(1+xsi)*(1-xsi)*(1-eta)*(1+zeta),
                        -0.5*xsi*(1-eta)*(1+zeta),
                        -0.25*(1+xsi)*(1-xsi)*(1+zeta),
                        0.25*(1+xsi)*(1-xsi)*(1-eta)],
                        [0.25*(1+xsi)*(1+eta)*(1-eta)*(1+zeta),
                        0.25*(1+eta)*(1-eta)*(1+zeta),
                        -0.5*(1+xsi)*eta*(1+zeta),
                        0.25*(1+xsi)*(1+eta)*(1-eta)],
                        [0.25*(1+xsi)*(1-xsi)*(1+eta)*(1+zeta),
                        -0.5*xsi*(1+eta)*(1+zeta),
                        0.25*(1+xsi)*(1-xsi)*(1+zeta),
                        0.25*(1+xsi)*(1-xsi)*(1+eta)],
                        [0.25*(1-xsi)*(1+eta)*(1-eta)*(1+zeta),
                        -0.25*(1+eta)*(1-eta)*(1+zeta),
                        -0.5*(1-xsi)*eta*(1+zeta),
                        0.25*(1-xsi)*(1+eta)*(1-eta)],
                        [0.25*(1-xsi)*(1-eta)*(1+zeta)*(1-zeta),
                        -0.25*(1-eta)*(1+zeta)*(1-zeta),
                        -0.25*(1-xsi)*(1+zeta)*(1-zeta),
                        -0.5*(1-xsi)*(1-eta)*zeta],
                        [0.25*(1+xsi)*(1-eta)*(1+zeta)*(1-zeta),
                        0.25*(1-eta)*(1+zeta)*(1-zeta),
                        -0.25*(1+xsi)*(1+zeta)*(1-zeta),
                        -0.5*(1+xsi)*(1-eta)*zeta],
                        [0.25*(1+xsi)*(1+eta)*(1+zeta)*(1-zeta),
                        0.25*(1+eta)*(1+zeta)*(1-zeta),
                        0.25*(1+xsi)*(1+zeta)*(1-zeta),
                        -0.5*(1+xsi)*(1+eta)*zeta],
                        [0.25*(1-xsi)*(1+eta)*(1+zeta)*(1-zeta),
                        -0.25*(1+eta)*(1+zeta)*(1-zeta),
                        0.25*(1-xsi)*(1+zeta)*(1-zeta),
                        -0.5*(1-xsi)*(1+eta)*zeta]]
