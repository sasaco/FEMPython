import numpy as np
from typing import List
from SolidElement import HexaElement1
from ElementBorder import addMatrix
#--------------------------------------------------------------------#

class HexaElement1WT(HexaElement1):

    #--------------------------------------------------------------------#
    # Wilson-Taylor非適合六面体1次要素
    # label - 要素ラベル
    # material - 材料のインデックス
    # nodes - 節点番号
    def __init__(self, label,material,nodes):
        super().__init__(label, material, nodes)
        self.te=None		# 非適合モードの変換マトリックス

    # 要素境界名称を返す
    def getName(self):
        return 'HexaElement1WT'


    # 剛性マトリックスを返す
    # p - 要素節点
    # d1 - 応力 - 歪マトリックス
    def stiffnessMatrix(self, p,d1) -> np.ndarray:
        size = 3 * self.nodeCount()
        kk = np.zeros((size, size))
        k2 = np.zeros((size, 9))
        k3 = np.zeros((9, 9))
        sf0 = self.shapeFunction(0, 0, 0)
        ja0 = self.jacobianMatrix(p, sf0).reshape(3,3)
        ji0 = np.linalg.inv(ja0).reshape(9)
        jj0 = abs(np.linalg.det(ja0))
        for i in range(len(self.intP)):
            sf = self.shapeFunction(self.intP[i][0], self.intP[i][1], self.intP[i][2])
            ja = self.jacobianMatrix(p, sf)
            b = self.strainMatrix(self.grad(p, ja, sf))
            b0 = self.strainMatrix2(ji0, self.intP[i])
            ks = self.stiffPart(d1, b, self.intP[i][3] * abs(np.linalg.det(ja.reshape(3,3))))
            cf0 = self.intP[i][3] * jj0
            addMatrix(kk, ks)
            addMatrix(k2, self.stiffPart2(d1,b,b0,cf0))
            addMatrix(k3, self.stiffPart(d1,b0,cf0))
        invK3 = np.linalg.inv(k3)
        traK2 = np.transpose(k2)
        k4 = np.dot(invK3, -1 * traK2)
        addMatrix(kk, np.dot(k2, k4))
        self.te = np.transpose(k4)
        return kk

    # 要素内の歪ベクトルを返す
    # p - 要素節点
    # v - 節点変位ベクトル
    # x - ξ,η,ζ座標
    def strainPart(self, p, v, x):
        sf = self.shapeFunction(x[0],x[1],x[2])
        ja = self.jacobianMatrix(p,sf)
        ji = np.linalg.inv(ja.reshape(3,3)).reshape(9)
        sm = self.strainMatrix(self.grad(p,ja,sf))
        sm0 = np.dot(self.te, self.strainMatrix2(ji, x))
        count = 3 * self.nodeCount()
        eps = []
        for j in range(6):
            s = 0
            for k in range(count):
                s += v[k] * (sm[k][j] + sm0[k][j])
            eps.append(s)
        return eps

    # 非適合モードの歪 - 変位マトリックスの転置行列を返す
    # ji - ヤコビ逆行列の要素配列
    # x - ξ,η,ζ座標
    def strainMatrix2(self, ji, x) -> np.ndarray:
        m = np.zeros((9,6))
        d1 =- 2*x[0]
        d2 =- 2*x[1]
        d3 =- 2*x[2]
        gr = [[ ji[0] * d1, ji[1] * d1, ji[2] * d1 ],
              [ ji[3] * d2, ji[4] * d2, ji[5] * d2 ],
              [ ji[6] * d3, ji[7] * d3, ji[8] * d3 ]]
        for i in range(3):
            i3 = 3 * i
            m1 = m[i3]
            m2 = m[i3 + 1]
            m3 = m[i3 + 2]
            gx = gr[i][0]
            gy = gr[i][1]
            gz = gr[i][2]
            m1[0] = gx
            m2[1] = gy
            m3[2] = gz
            m1[3] = gy
            m2[3] = gx
            m2[4] = gz
            m3[4] = gy
            m1[5] = gz
            m3[5] = gx

        return m

    # 積分点の剛性マトリックス [B1t][D][B2]を返す
    # d - 応力-歪マトリックス
    # b1 - 歪-変位マトリックスの転置行列 B1t
    # b2 - 歪-変位マトリックスの転置行列 B2t
    # coef - 係数
    def stiffPart2(self, d, b1, b2, coef):
        size1 = len(b1)
        size2 = len(d)
        size3 = len(b2)

        k = []
        for i in range(size1):
            a=[]
            bi = b1[i]
            for j in range(size2):
                a.append(coef * np.dot(bi, d[j]))

            ki=[]
            for j in range(size3):
                ki.append(np.dot(a, b2[j]))

            k.append(ki)

        return k
