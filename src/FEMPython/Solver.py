from FEMPython.BoundaryCondition import BoundaryCondition
from FEMPython.Element import normalVector
from FEMPython.Result import NODE_DATA, ELEMENT_DATA
from FEMPython.Material import Material
from FEMPython.Lanczos import eigByLanczosLUP, eigByLanczosILUCG, eigByArnoldiLUP, eigByArnoldiILUCG
import numpy as np
from typing import List
#--------------------------------------------------------------------#

PRECISION = 1e-10	# マトリックス精度
LU_METHOD = 0	    # LU分解法
ILUCG_METHOD = 1	# 不完全LU分解共役勾配法


# 質量マトリックスを作成する
# dof - モデル自由度
def massMatrix(dof, model):
    mesh = model.mesh
    elements = mesh.elements
    matrix = np.zeros((dof,dof))
    mm,mmax = 0
    # for i in range(dof):
    #     matrix.append([])

    for i in range(len(elements)):
        elem=elements[i]
        material = model.materials[elem.material]
        dens = material.dens
        if elem.isShell:
            sp = model.shellParams[elem.param]
            mm = elem.massMatrix(mesh.getNodes(elem),dens,sp.thickness)
            mmax = setElementMatrix(elem,6,matrix,mm,mmax,model)

        elif elem.isBar:
            sect = model.barParams[elem.param].section
            mm = elem.massMatrix(mesh.getNodes(elem),dens,sect)
            mmax = setElementMatrix(elem,6,matrix,mm,mmax,model)

        else:
            mm = elem.massMatrix(mesh.getNodes(elem),dens)
            mmax = setElementMatrix(elem,3,matrix,mm,mmax,model)

    # 座標変換
    rests = model.bc.restraints
    index = model.bc.nodeIndex
    bcdof = model.bc.dof
    for i in range(len(rests)):
        ri = rests[i]
        if ri.coords:
            ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i])

    # 絶対値が小さい成分を除去する
    eps = PRECISION * mmax
    for i in range(dof):
        mrow=matrix[i]
        for j in mrow:
            if abs(j)<eps:
                j=0

    return matrix


# 剛性マトリックスを作成する
# dof - モデル自由度
def stiffnessMatrix(dof, model):
    mesh = model.mesh
    elements = mesh.elements
    matrix = np.zeros((dof,dof)) #[[] for i in range(dof)]
    kmax = 0

    for i in range(len(elements)):
        elem = elements[i]
        material: Material = model.materials[elem.material]
        mat = material.matrix
        if elem.isShell:
            sp = model.shellParams[elem.param]
            if elem.getName() == 'TriElement1':
                km = elem.stiffnessMatrix(mesh.getNodes(elem), mat['m2d'], sp)
            else:
                km = elem.stiffnessMatrix(mesh.getNodes(elem), mat['msh'], sp)

            kmax = setElementMatrix(elem, 6, matrix, km, kmax, model)

        elif elem.isBar:
            sect = model.barParams[elem.param].section
            km = elem.stiffnessMatrix(mesh.getNodes(elem), material, sect)
            kmax = setElementMatrix(elem, 6, matrix, km, kmax, model)

        else:
            km = elem.stiffnessMatrix(mesh.getNodes(elem), mat['m3d'])
            kmax = setElementMatrix(elem, 3, matrix, km, kmax, model)

    # 座標変換
    rests = model.bc.restraints
    index = model.bc.nodeIndex
    bcdof = model.bc.dof
    for i in range(len(rests)):
        ri = rests[i]
        if ri.coords:
            ri.coords.transMatrix(matrix, dof, index[ri.node], bcdof[i])

    # 絶対値が小さい成分を除去する
    eps = PRECISION * kmax
    for i in range(dof):
        mrow = matrix[i]
        for j in mrow:
            if abs(j) < eps:
                j = 0

    return matrix


# 幾何剛性マトリックスを作成する
# dof - モデル自由度
def geomStiffnessMatrix(dof, model):
    mesh = model.mesh
    elements = mesh.elements
    nodes = mesh.nodes
    disp = model.result.displacement
    matrix = np.zeros((dof, dof))
    kmax = 0
    # for i in range(dof):
    #     matrix.append([])
    for i in range(len(elements)):
        elem = elements[i]
        en = elem.nodes
        p = []
        v = []
        for j in range(len(en)):
            p.append(nodes[en[j]])
            v.append(disp[en[j]])

        material = model.materials[elem.material]
        mat = material.matrix

        if elem.isShell:
            sp = model.shellParams[elem.param]
            if elem.getName() == 'TriElement1':
                km = elem.geomStiffnessMatrix(p, v, mat.m2d, sp)
            else:
                km = elem.geomStiffnessMatrix(p, v, mat.msh, sp)
            kmax = setElementMatrix(elem, 6, matrix, km, kmax,model)

        elif(elem.isBar):
            sect = model.barParams[elem.param].section
            km = elem.geomStiffnessMatrix(p, v, material, sect)
            kmax = setElementMatrix(elem, 6, matrix, km, kmax,model)

        else:
            km = elem.geomStiffnessMatrix(p, v, mat.m3d)
            kmax = setElementMatrix(elem, 3, matrix, km, kmax,model)

    # 座標変換
    rests = model.bc.restraints
    index = model.bc.nodeIndex
    bcdof = model.bc.dof
    for i in range(len(rests)):
        ri = rests[i]
        if ri.coords:
            ri.coords.transMatrix(matrix, dof, index[ri.node], bcdof[i])

    # 絶対値が小さい成分を除去・符号反転
    eps = PRECISION * kmax
    for i in range(dof):
        mrow = matrix[i]
        for j in mrow:
            if abs(j) < eps:
                j =0
            else:
                j = -j
    return matrix

# 要素のマトリックスを設定する
# element - 要素
# dof - 自由度
# matrix - 全体剛性マトリックス
# km - 要素の剛性マトリックス
# kmax - 成分の絶対値の最大値
def setElementMatrix(element, dof, matrix, km, kmax, model):
    nodeCount = element.nodeCount()
    index = model.bc.nodeIndex
    nodes = element.nodes
    for i in range(nodeCount):
        row0 = index[nodes[i]]
        i0 = dof * i
        for j in range(nodeCount):
            column0 = index[nodes[j]]
            j0 = dof * j
            for i1 in range(dof):
                mrow = matrix[row0+i1]
                krow = km[i0+i1]
                for j1 in range(dof):
                    cj1 = column0+j1
                    # if cj1 in mrow:
                    mrow[cj1] += krow[j0+j1]
                    kmax=max(kmax, abs(mrow[cj1]))
                    # else:
                    #     mrow[cj1] = krow[j0+j1]
                    #     kmax = max(kmax, abs(mrow[cj1]))

    return kmax

# 荷重ベクトルを作成する
# dof - モデル自由度
def loadVector(dof, model):
    loads = model.bc.loads
    press = model.bc.pressures
    vector = np.zeros(dof)
    index: List[int] = model.bc.nodeIndex
    bcdof = model.bc.dof
    for i in range(len(loads)):
        ld = loads[i]
        nd = ld.node
        ldx = ld.globalX
        ldof = bcdof[nd]
        index0 = index[nd]
        for j in range(ldof):
            vector[index0+j] = ldx[j]

    for i in range(len(press)):
        border = press[i].getBorder(model.mesh.elements[press[i].element])
        p = model.mesh.getNodes(border)
        ps = border.shapeFunctionVector(p,press[i].press)
        norm = normalVector(p)
        count = border.nodeCount()
        for j in range(count):
            index0 = index[border.nodes[j]]
            vector[index0] -= ps[j] * norm.x
            vector[index0+1] -= ps[j] * norm.y
            vector[index0+2] -= ps[j] * norm.z

    rests = model.bc.restraints

    for i in range(len(rests)):
        ri = rests[i]
        if ri.coords:
            ri.coords.transVector(vector,dof,index[ri.node],bcdof[i])

    return vector

# 伝熱マトリックスを作成する
def heatMatrix(model):
    elements = model.mesh.elements
    mesh = model.mesh
    dof = len(model.mesh.nodes)
    matrix = []
    for i in range(dof):
        matrix.append([])

    for i in range(len(elements)):
        elem = elements[i]
        count = elem.nodeCount()
        h = model.materials[elem.material].hCon
        if elem.isShell:
            sp = model.shellParams[elem.param]
            ls = elem.gradMatrix(mesh.getNodes(elem), h, sp)

        elif elem.isBar:
            sect = model.barParams[elem.param].section
            ls = elem.gradMatrix(mesh.getNodes(elem), h, sect)

        else:
            ls = elem.gradMatrix(mesh.getNodes(elem), h)

        for i1 in range(count):
            mrow = matrix[elem.nodes[i1]]
            lrow = ls[i1]
            for j1 in range(count):
                if elem.nodes[j1] in mrow:
                    mrow[elem.nodes[j1]] += lrow[j1]

                else:
                    mrow[elem.nodes[j1]] = lrow[j1]

    return matrix


# 熱境界条件ベクトルを作成する
# matrix - 伝熱マトリックス
def tempVector(matrix, model):
    htcs = model.bc.htcs
    vector = np.zeros(len(model.mesh.nodes))

    for i in range(len(htcs)):
        elem = model.mesh.elements[htcs[i].element]
        border = htcs[i].getBorder(elem)
        p = model.mesh.getNodes(border)
        h = htcs[i].htc
        if border.isEdge:
            sp = model.shellParams[elem.param]
            h *= sp.thickness

        hm = border.shapeFunctionMatrix(p,h)
        hv = border.shapeFunctionVector(p,h*htcs[i].outTemp)
        count = border.nodeCount()
        for i1 in range(count):
            mrow = matrix[border.nodes[i1]]
            hrow = hm[i1]
            for j1 in range(count):
                if border.nodes[j1] in mrow:
                    mrow[border.nodes[j1]] += hrow[j1]

                else:
                    mrow[border.nodes[j1]] = hrow[j1]

            vector[border.nodes[i1]] += hv[i1]

    return vector


# 行列の行から一部を抽出する
# mrow - 元のマトリックスの行データ
# list - 抽出部分のリスト
def extructRow(mrow, list):
    for j in list:
        self.matrix[i].append(matrix1[list[i]][j])

    exrow = [0 for i in range(len(mrow))]
    col = [i for i in range(len(mrow))]
    i1 = 0
    j1 = 0
    # for j in mrow:
    #     if mrow.hasOwnProperty(j):
    #         col.append(int(j))
    # col = sorted(col, key=lambda j1, j2: j1-j2)

    while i1 < len(col) and j1 < len(list):
        if col[i1] == list[j1]:
            exrow[j1] = mrow[col[i1]]
            i1 += 1
            j1 += 1

        elif col[i1] < list[j1]:
            i1 += 1

        else:
            j1 += 1

    return exrow


#--------------------------------------------------------------------#
# 連立方程式求解オブジェクト
class Solver():
    def __init__(self, model):
        self.clear()
        self.method = LU_METHOD	# 方程式解法
        self.model = model
        self.bc: BoundaryCondition = model.bc

    # データを消去する
    def clear(self):
        self.matrix = []		# 行列
        self.matrix2 = []		# 第２行列
        self.vector = []		# ベクトル
        self.dof = 0			# モデル自由度

    # 剛性マトリックス・荷重ベクトルを作成する
    def createStiffnessMatrix(self):
        bc = self.bc
        bcList: List[int] = bc.bcList
        reducedList = []
        for i in range(len(bcList)):
            if bcList[i] < 0:
                reducedList.append(i)

        # 剛性マトリックス・荷重ベクトルの作成
        matrix1 = stiffnessMatrix(self.dof, self.model)
        vector1 = loadVector(self.dof, self.model)

        # 拘束自由度を除去する
        for i in range(len(bcList)):
            if bcList[i] >= 0:
                rx = bc.getRestDisp(bcList[i])
                for j in range(len(vector1)):
                    if i in matrix1[j]:
                        vector1[j] -= rx*matrix1[j][i]

        self.extruct(matrix1,vector1,reducedList)


    # 剛性マトリックス・質量マトリックスを作成する
    def createStiffMassMatrix(self):
        bc = self.bc
        bcList = bc.bcList
        reducedList = []
        for i in range(len(bcList)):
            if bcList[i] < 0:
                reducedList.append(i)

        matrix1 = stiffnessMatrix(self.dof)
        matrix2 = massMatrix(self.dof)

        self.matrix = []	# 行列
        self.matrix2 = []   # 第２行列

        for i in range(len(reducedList)):
            self.matrix[i] = extructRow(matrix1[reducedList[i]], reducedList)
            self.matrix2[i] = extructRow(matrix2[reducedList[i]], reducedList)


    # 幾何剛性マトリックスを作成する
    def createGeomStiffMatrix(self):
        bc = self.bc
        bcList: List[int] = bc.bcList
        reducedList = []
        for i in range(len(bcList)):
            if bcList[i] < 0:
                reducedList.append(i)

        matrix2 = geomStiffnessMatrix(self.dof)

        self.matrix2 = []		# 第２行列
        for i in range(len(reducedList)):
            self.matrix2[i] = extructRow(matrix2[reducedList[i]], reducedList)


    # 熱計算のマトリックス・ベクトルを計算する
    def createHeatMatrix(self):
        bcList: List[int] = self.bc.bcList
        reducedList = []
        for i in range(len(bcList)):
            if bcList[i] < 0:
                reducedList.append(i)

        # 伝熱マトリックス・熱境界条件ベクトルの作成
        matrix1 = heatMatrix()
        vector1 = tempVector(matrix1)

        # 拘束自由度を除去する
        for i in range(len(bcList)):
            if bcList[i] >= 0:
                t = self.bc.temperature[bcList[i]]
                for j in range(len(vector1)):
                    if i in matrix1[j]:
                        vector1[j] -= t.t * matrix1[j][i]

        self.extruct(matrix1, vector1, reducedList)

    # 行列の一部を抽出する
    # matrix1,vector1 - 元のマトリックス,ベクトル
    # list - 抽出部分のリスト
    def extruct(self, matrix1: np.ndarray, vector1: np.ndarray, list: List[int]):
        # self.matrix = matrix1    # 行列
        # self.vector = vector1	# ベクトル
        # return
        self.matrix = []    # 行列
        self.vector = []	# ベクトル
        for i in list:
            self.vector.append(vector1[i])
            matrix2 = []
            for j in list:
                matrix2.append(matrix1[i][j])
                #self.matrix.append(extructRow(matrix1[list[i]], list))
            self.matrix.append(matrix2)


    # 連立方程式を解く
    def solve(self):
        return np.linalg.solve(self.matrix, self.vector)


    # ランチョス法で固有値・固有ベクトルを求める
    # n - ３重対角化行列の大きさ
    # delta - シフト量δ
    def eigenByLanczos(self, n,delta):
        return eigByLanczosLUP(self.matrix,self.matrix2,n,delta)
        return eigByLanczosILUCG(self.matrix,self.matrix2,n,delta)

    # アーノルディ法で固有値・固有ベクトルを求める
    # n - ３重対角化行列の大きさ
    # delta - シフト量δ
    def eigenByArnoldi(self,n,delta):
        return eigByArnoldiLUP(self.matrix,self.matrix2,n,delta)
        return eigByArnoldiILUCG(self.matrix,self.matrix2,n,delta)
