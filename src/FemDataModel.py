from Solver import Solver
from Result import Result, EigenValue, NODE_DATA, ELEMENT_DATA, VIBRATION, BUCKLING
from Material import Material
from BoundaryCondition import BoundaryCondition
from ElementBorder import EdgeBorder1, compare
from Element import normalVector
from FENode import FENode

import math
import numpy as np
import time
from typing import List, Union
from functools import cmp_to_key
#--------------------------------------------------------------------#

COEF_F_W = 0.5 / math.pi	# f/ω比 1/2π

# 節点集合の節点ラベルを再設定する
# map - ラベルマップ
# s - 節点集合
def resetNodes(map,s):
    for i in range(len(s.nodes)):
        if s.nodes[i] in map:
            s.nodes[i] = map[s.nodes[i]]
        else:
            raise Exception('節点番号'+s.nodes[i]+'は存在しません')


# 節点ポインタを再設定する
# map - ラベルマップ
# bc - 境界条件
def resetNodePointer(map,bc):
    if(bc.node in map):
        bc.node=map[bc.node]
    else:
        raise Exception('節点番号'+bc.node+'は存在しません')


# 要素ポインタを再設定する
# map - ラベルマップ
# bc - 境界条件
def resetElementPointer(map,bc):
    if(bc.element in map):
        bc.element=map[bc.element]
    else:
        raise Exception('要素番号'+bc.element+'は存在しません')


# 局所座標系を再設定する
# map - ラベルマップ
# bc - 境界条件
def resetCoordinatesPointer(map,bc):
    coords=bc.coords
    if(coords==None):
        pass
    elif(coords in map):
        bc.coords=map[coords]
        bc.globalX=bc.coords.toGlobal(bc.x)
    else:
        raise Exception('局所座標系番号'+coords+'は存在しません')



# 重心位置を返す
# p - 頂点座標
def center(p: List[FENode]) -> np.ndarray:
    x=0
    y=0
    z=0
    cc=1.0/len(p)
    for i in range(len(p)):
        x+=p[i].x
        y+=p[i].y
        z+=p[i].z
    return np.array([cc*x,cc*y,cc*z])


# ラベルを比較する
# o1,o2 - 比較する対象
def compareLabel(o1):
    return o1.label


#--------------------------------------------------------------------#
# FEM データモデル
class FemDataModel:
    def __init__(self):
        self.materials: List[Material] = []			# 材料
        self.shellParams = []			# シェルパラメータ
        self.barParams = []			# 梁パラメータ
        self.coordinates = []			# 局所座標系
        self.mesh = MeshModel()		# メッシュモデル
        self.bc = BoundaryCondition()	# 境界条件
        self.solver = Solver(self)		# 連立方程式求解オブジェクト
        self.result = Result()		# 計算結果
        self.hasShellBar = False		# シェル要素または梁要素を含まない

    # データを消去する
    def clear(self):
        self.materials.clear()
        self.shellParams.clear()
        self.barParams.clear()
        self.coordinates.clear()
        self.mesh.clear()
        self.bc.clear()
        self.result.clear()
        self.result.type = ELEMENT_DATA # NODE_DATA, VIBRATION, BUCKLING


    # モデルを初期化する
    def init(self):
        mats=self.materials
        sorted(mats, key=compareLabel)
        self.mesh.init()
        self.bc.init()
        self.reNumbering()
        self.resetMaterialLabel()
        self.resetParameterLabel()
        self.resetCoordinates()
        self.mesh.checkChirality()
        self.mesh.getFreeFaces()
        self.mesh.getFaceEdges()
        for i in range(len(mats)):
            m2d = mats[i].matrix2Dstress()
            msh = mats[i].matrixShell()
            m3d = mats[i].matrix3D()
            mats[i].matrix = {
                "m2d": m2d,
                "msh": msh,
                "m3d": m3d
            }


    # 節点・要素ポインタを設定する
    def reNumbering(self):
        nodes = self.mesh.nodes
        elements = self.mesh.elements

        map = dict()
        for i in range(len(nodes)):
            map[nodes[i].label] = i

        for i in range(len(elements)):
            resetNodes(map,elements[i])

        for i in range(len(self.bc.restraints)):
            resetNodePointer(map,self.bc.restraints[i])

        for i in range(len(self.bc.loads)):
            resetNodePointer(map,self.bc.loads[i])

        for i in range(len(self.bc.temperature)):
            resetNodePointer(map,self.bc.temperature[i])

        map = dict()
        for i in range(len(elements)):
            map[elements[i].label]=i

        for i in range(len(self.bc.pressures)):
            resetElementPointer(map,self.bc.pressures[i])

        for i in range(len(self.bc.htcs)):
            resetElementPointer(map,self.bc.htcs[i])


    # 材料ポインタを設定する
    def resetMaterialLabel(self):
        if(len(self.materials)==0):
            self.materials.append(Material(1,1,0.3,1,1,1,1))

        map = dict()
        elements = self.mesh.elements

        for i in range(len(self.materials)):
            map[self.materials[i].label] = i

        for i in range(len(elements)):
            if(elements[i].material in map):
                elements[i].material = map[elements[i].material]

            else:
                raise Exception('材料番号'+elements[i].material+
                                                'は存在しません')


    # シェルパラメータ・梁パラメータのポインタを設定する
    def resetParameterLabel(self):
        if(len(self.shellParams)==0 and len(self.barParams)==0):
            self.hasShellBar=False
            return

        map1 = dict()
        map2 = dict()
        elements=self.mesh.elements
        shellbars=0
        for i in range(len(self.shellParams)):
            map1[self.shellParams[i].label]=i

        for i in range(len(self.barParams)):
            map2[self.barParams[i].label]=i

        for i in range(len(elements)):
            if(elements[i].isShell):
                if(elements[i].param in map1):
                    elements[i].param=map1[elements[i].param]
                    shellbars += 1
                else:
                    raise Exception('パラメータ番号'+elements[i].param+
                                                    'は存在しません')

            elif(elements[i].isBar):
                if(elements[i].param in map2):
                    elements[i].param=map2[elements[i].param]
                    shellbars += 1
                else:
                    raise Exception('パラメータ番号'+elements[i].param+
                                                    'は存在しません')

        self.hasShellBar=(shellbars>0)


    # 局所座標系を設定する
    def resetCoordinates(self):
        if(len(self.coordinates)==0):
            return

        map = dict()
        for i in range(len(self.coordinates)):
            map[self.coordinates[i].label]=self.coordinates[i]

        for i in range(len(self.bc.restraints)):
            resetCoordinatesPointer(map,self.bc.restraints[i])

        for i in range(len(self.bc.loads)):
            resetCoordinatesPointer(map,self.bc.loads[i])


    # 節点の自由度を設定する
    def setNodeDoF(self):
        nodeCount = len(self.mesh.nodes)
        elemCount = len(self.mesh.elements)

        self.bc.dof = [3 for i in range(nodeCount)]
        dof = self.bc.dof
        for i in range(elemCount):
            elem = self.mesh.elements[i]
            if(elem.isShell or elem.isBar):	# シェル要素・梁要素
                count = elem.nodeCount()
                for j in range(count):
                    dof[elem.nodes[j]]=6

        self.solver.dof = self.bc.setPointerStructure(nodeCount)


    # 静解析をする
    def calculate(self):
        t0=time.time()
        calc=False
        if len(self.bc.temperature)>0 or len(self.bc.htcs)>0:
            self.solver.dof=len(self.mesh.nodes)
            self.bc.setPointerHeat(self.solver.dof)
            self.solver.createHeatMatrix()
            tmp=self.solver.solve()
            self.result.setTemperature(self.bc,tmp, len(self.mesh.nodes))
            calc=True

        if len(self.bc.restraints)>0:
            self.setNodeDoF()
            self.solver.createStiffnessMatrix()
            d=self.solver.solve()
            self.result.setDisplacement(self.bc,d, len(self.mesh.nodes))
            if(self.result.type==ELEMENT_DATA):
                self.calculateElementStress()
            else:
                self.calculateNodeStress()
            calc=True

        if calc == False:
            raise Exception('拘束条件不足のため計算できません')

        t1 = time.time()
        print('Calculation time: {0}ms'.format(t1-t0))


    # 固有振動数・固有ベクトルを求める
    # count - 求める固有振動の数
    def charVib(self, count):
        t0=time.time()
        self.result.clear()
        self.setNodeDoF()
        count = min(count, self.solver.dof)
        n = min(3*count, self.solver.dof)
        self.solver.createStiffMassMatrix()
        eig=self.solver.eigenByLanczos(n)
        nodeCount= len(self.mesh.nodes)
        for i in range(count, n):
            del eig.ut[i]

        for i in range(count):
            f = COEF_F_W * math.sqrt(max(eig['lambda'][i], 0))
            uti = eig.ut[i]
            s = 0
            for j in range(len(uti)):
                 s += uti[j] * uti[j]
            u = np.multiply(1/math.sqrt(s), uti)
            ev = EigenValue(f,VIBRATION)
            ev.setDisplacement(self.bc,u,nodeCount)
            self.result.addEigenValue(ev)
            if(self.result.type==ELEMENT_DATA):
                self.calculateEvElementEnergy(ev)
            else:
                self.calculateEvNodeEnergy(ev)
            del eig.ut[i]

        t1=time.time()
        print('Calculation time:'+(t1-t0)+'ms')


    # 線形座屈解析をする
    # count - 求める固有値の数
    def calcBuckling(self, count):
        t0 = time.time()
        if(len(self.bc.restraints)==0):
            raise Exception('拘束条件がありません')

        self.setNodeDoF()
        n = min(3*count,self.solver.dof)
        self.solver.createStiffnessMatrix()
        d=self.solver.solve()
        self.result.setDisplacement(self.bc,d,len(self.mesh.nodes))
        self.solver.createGeomStiffMatrix()
        self.result.clear()
        eig=self.solver.eigenByArnoldi(n,0)
        nodeCount=len(self.mesh.nodes)
        for i in range(count, n):
            del eig.ut[i]
        for i in range(count):
            uti=eig.ut[i]
            s = 0
            for j in range(len(uti)):
                s += uti[j]*uti[j]
            u=np.multiply(1/math.sqrt(s),uti)
            ev=EigenValue(eig['lambda'][i],BUCKLING)
            ev.setDisplacement(self.bc,u,nodeCount)
            self.result.addEigenValue(ev)
            if(self.result.type==ELEMENT_DATA):
                self.calculateEvElementEnergy(ev)
            else:
                self.calculateEvNodeEnergy(ev)
            del eig.ut[i]
        t1=time.time()
        print('Calculation time:'+(t1-t0)+'ms')


    # 節点歪・応力・歪エネルギー密度を計算する
    def calculateNodeStress(self):
        nodes=self.mesh.nodes
        nodeCount=len(nodes)
        elems=self.mesh.elements
        elemCount=len(elems)
        angle= np.zeros(nodeCount)

        self.result.initStrainAndStress(nodeCount)
        for i in range(elemCount):
            elem = elems[i]
            en = elem.nodes
            p = []
            v = []
            for j in range(len(en)):
                p.append(nodes[en[j]])
                v.append(self.result.displacement[en[j]])

            material=self.materials[elem.material]
            mat=material.matrix
            ea=elem.angle(p)
            if elem.isShell:
                sp=self.shellParams[elem.param]
                if(elem.getName()=='TriElement1'):
                    mmat=mat.m2d
                else:
                    mmat=mat.msh
                s=elem.strainStress(p,v,mmat,sp)
                eps1=s[0]
                str1=s[1]
                se1=s[2]
                eps2=s[3]
                str2=s[4]
                se2=s[5]
                for j in range(len(en)):
                    eaj=ea[j]
                    eps1[j].mul(eaj)
                    eps2[j].mul(eaj)
                    str1[j].mul(eaj)
                    str2[j].mul(eaj)
                    se1[j]*=eaj
                    se2[j]*=eaj
                    self.result.addStructureData(en[j],eps1[j],str1[j],se1[j],
                                                                            eps2[j],str2[j],se2[j])
                    angle[en[j]]+=eaj

            elif elem.isBar:
                sect = self.barParams[elem.param].section
                s = elem.strainStress(p,v,material,sect)
                eps1 = s[0]
                str1 = s[1]
                se1 = s[2]
                eps2 = s[3]
                str2 = s[4]
                se2 = s[5]
                for j in range(len(en)):
                    self.result.addStructureData(en[j],eps1[j],str1[j],se1[j],
                                                                            eps2[j],str2[j],se2[j])
                    angle[en[j]] += 1

            else:
                s = elem.strainStress(p, v, mat['m3d'])
                eps1 = s[0]
                str1 = s[1]
                se1 = s[2]
                for j in range(len(en)):
                    eaj = ea[j]
                    eps1[j].mul(eaj)
                    str1[j].mul(eaj)
                    se1[j] *= eaj
                    self.result.addStructureData(
                        en[j], eps1[j], str1[j], se1[j], eps1[j], str1[j], se1[j])
                    angle[en[j]] += eaj

        for i in range(nodeCount):
            if(angle[i]!=0):
                self.result.mulStructureData(i,1/angle[i])


    # 要素歪・応力・歪エネルギー密度を計算する
    def calculateElementStress(self):
        nodes = self.mesh.nodes
        elems = self.mesh.elements
        elemCount = len(elems)
        self.result.initStrainAndStress(elemCount)
        for i in range(elemCount):
            elem = elems[i]
            en = elem.nodes
            p = []
            v = []
            for j in range(len(en)):
                p.append(nodes[en[j]])
                v.append(self.result.displacement[en[j]])

            material = self.materials[elem.material]
            mat = material.matrix
            if elem.isShell:
                sp = self.shellParams[elem.param]
                if elem.getName()=='TriElement1':
                    mmat = mat['m2d']
                else:
                    mmat = mat['msh']
                s=elem.elementStrainStress(p, v, mmat, sp)
                self.result.addStructureData(i,s[0],s[1],s[2],s[3],s[4],s[5])

            elif(elem.isBar):
                sect=self.barParams[elem.param].section
                s=elem.elementStrainStress(p, v, material, sect)
                self.result.addStructureData(i, s[0], s[1], s[2], s[3], s[4], s[5])

            else:
                s=elem.elementStrainStress(p, v, mat['m3d'])
                self.result.addStructureData(i, s[0], s[1], s[2], s[0], s[1], s[2])


    # 固有値データの節点歪エネルギー密度を計算する
    # ev - 固有値データ
    def calculateEvNodeEnergy(self, ev):
        nodes=self.mesh.nodes
        nodeCount=len(nodes)
        elems=self.mesh.elements
        elemCount=len(elems)
        angle=np.zeros(nodeCount)
        ev.initStrainEnergy(nodeCount)
        for i in range(elemCount):
            elem=elems[i]
            en=elem.nodes
            p=[]
            v=[]
            for j in range(len(en)):
                p[j]=nodes[en[j]]
                v[j]=ev.displacement[en[j]]

            material=self.materials[elem.material]
            mat=material.matrix
            ea=elem.angle(p)
            if elem.isShell:
                sp=self.shellParams[elem.param]
                if elem.getName()=='TriElement1':
                    mmat=mat.m2d
                else:
                    mmat=mat.msh

                s=elem.strainStress(p,v,mmat,sp)
                se1=s[2]
                se2=s[5]
                for j in range(len(en)):
                    enj=en[j]
                    eaj=ea[j]
                    se1[j]*=eaj
                    se2[j]*=eaj
                    ev.sEnergy1[enj]+=se1[j]
                    ev.sEnergy2[enj]+=se2[j]
                    angle[enj]+=eaj

            elif elem.isBar:
                sect=self.barParams[elem.param].section
                s=elem.strainStress(p,v,material,sect)
                se1=s[2]
                se2=s[5]
                for j in range(len(en)):
                    enj=en[j]
                    ev.sEnergy1[enj]+=se1[j]
                    ev.sEnergy2[enj]+=se2[j]
                    angle[enj] += 1

            else:
                s=elem.strainStress(p,v,mat.m3d)
                se1=s[2]
                for j in range(len(en)):
                    enj=en[j]
                    eaj=ea[j]
                    se1[j]*=eaj
                    ev.sEnergy1[enj]+=se1[j]
                    ev.sEnergy2[enj]+=se1[j]
                    angle[enj]+=eaj

        for i in range(nodeCount):
            if(angle[i]!=0):
                coef=1/angle[i]
                ev.sEnergy1[i]*=coef
                ev.sEnergy2[i]*=coef


    # 固有値データの要素歪エネルギー密度を計算する
    # ev - 固有値データ
    def calculateEvElementEnergy(self, ev):
        nodes=self.mesh.nodes
        elems=self.mesh.elements
        elemCount=len(elems)
        ev.initStrainEnergy(elemCount)
        for i in range(elemCount):
            elem=elems[i]
            en=elem.nodes
            p=[]
            v=[]
            for j in range(len(en)):
                p[j]=nodes[en[j]]
                v[j]=ev.displacement[en[j]]

            material=self.materials[elem.material]
            mat=material.matrix
            if elem.isShell:
                sp=self.shellParams[elem.param]
                if(elem.getName()=='TriElement1'):
                    mmat=mat.m2d
                else:
                    mmat=mat.msh

                s=elem.elementStrainStress(p,v,mmat,sp)
                ev.sEnergy1[i]=s[2]
                ev.sEnergy2[i]=s[5]

            elif elem.isBar:
                sect=self.barParams[elem.param].section
                s=elem.elementStrainStress(p,v,material,sect)
                ev.sEnergy1[i]=s[2]
                ev.sEnergy2[i]=s[5]

            else:
                s=elem.elementStrainStress(p,v,mat.m3d)
                ev.sEnergy1[i]=s[2]
                ev.sEnergy2[i]=s[2]


    # データ文字列を返す
    def toStrings(self):
        s=[]
        nodes=self.mesh.nodes
        elems=self.mesh.elements
        for i in range(len(self.materials)):
            s.append(self.materials[i].toString())

        for i in range(len(self.shellParams)):
            s.append(self.shellParams[i].toString())

        for i in range(len(self.barParams)):
            s.append(self.barParams[i].toString())

        for i in range(len(self.coordinates)):
            s.append(self.coordinates[i].toString())

        for i in range(len(nodes)):
            s.append(nodes[i].toString())

        for i in range(len(elems)):
            if elems[i].isShell:
                s.append(elems[i].toString(self.materials,self.shellParams,nodes))

            elif elems[i].isBar:
                s.append(elems[i].toString(self.materials,self.barParams,nodes))

            else:
                s.append(elems[i].toString(self.materials,nodes))

        #Array.prototype.push.apply(s,self.bc.toStrings(nodes,elems))
        for e in self.bc.toStrings(nodes,elems):
            s.append(e)
        #Array.prototype.push.apply(s,self.result.toStrings(nodes,elems))
        for e in self.result.toStrings(nodes,elems):
            s.append(e)

        return s


#--------------------------------------------------------------------#
# メッシュモデル
class MeshModel():
    def __init__(self):
        self.clear()

    # データを消去する
    def clear(self):
        self.nodes=[]		    # 節点
        self.elements=[]		# 要素
        self.freeFaces=[]		# 表面
        self.faceEdges=[]		# 表面の要素辺


    # 節点を返す
    # s - 節点集合
    def getNodes(self, s):
        p=[]
        for i in range(len(s.nodes)):
            p.append(self.nodes[s.nodes[i]])
        return p

    # モデルを初期化する
    def init(self):
        sorted(self.nodes, key=compareLabel)
        # bounds.set() // THREE.js に関する初期化処理`


    # 要素の鏡像向きを揃える
    def checkChirality(self):
        for i in range(len(self.elements)):
            elem = self.elements[i]
            if elem.isShell == False and elem.isBar == False:
                pe = self.getNodes(elem)
                pf = self.getNodes(elem.border(i, 0))
                n1 = normalVector(pf)
                n2 = center(pe) - center(pf)
                if n1.dot(n2) > 0:
                    elem.mirror()


    # 表面を取り出す
    def getFreeFaces(self):
        elems = self.elements
        if len(elems) == 0:
            return
        self.freeFaces = []
        border = []
        for i in range(len(elems)):
            if elems[i].isShell:
                self.freeFaces.append(elems[i].border(i,0))

            elif elems[i].isBar == False:
                count = elems[i].borderCount()
                for j in range(count):
                    border.append(elems[i].border(i,j))

        if len(border)>0:
            border = sorted(border, key=cmp_to_key(compare))
            addsw = True
            beforeEb = border[0]
            for i in range(len(border)):
                eb=border[i]
                if compare(beforeEb, eb) == 0:
                    addsw = False
                else:
                    if(addsw):
                        self.freeFaces.append(beforeEb)
                    beforeEb = eb
                    addsw = True

            if(addsw):
                self.freeFaces.append(beforeEb)


    # 表面の要素辺を取り出す
    def getFaceEdges(self):
        if len(self.freeFaces)==0:
            return
        self.faceEdges=[]
        edges=[]
        for i in range(len(self.freeFaces)):
            nds = self.freeFaces[i].cycleNodes()
            for j in range(len(nds)):
                edges.append(EdgeBorder1(i, [nds[j], nds[(j+1)%len(nds)]]))

        if len(edges)>0:
            edges = sorted(edges, key=cmp_to_key(compare))
            beforeEdge = edges[0]
            self.faceEdges.append(beforeEdge)
            for i in range(1, len(edges)):
                edge = edges[i]
                if compare(beforeEdge, edge) != 0:
                    self.faceEdges.append(edge)
                    beforeEdge = edge


    # # 形状データを取り出す
    # def getGeometry(self):
    #     sb=[]
    #     for i in range(len(self.freeFaces)):
    #         # Array.prototype.push.apply(sb,self.freeFaces[i].splitBorder())
    #         for e in self.freeFaces[i].splitBorder():
    #             sb.append(e)

    #     pos=new Float32Array(9*sb.length)
    #     norm=new Float32Array(9*sb.length)
    #     colors=new Float32Array(9*sb.length)
    #     geometry=THREE.BufferGeometry()
    #     geometry.elements=new Int32Array(3*sb.length)
    #     geometry.nodes=new Int32Array(3*sb.length)
    #     geometry.angle=new Float32Array(9*sb.length)
    #     for i in range(len(sb)):
    #         i9 = 9*i
    #         v=sb[i].nodes
    #         elem=sb[i].element
    #         p=[self.nodes[v[0]],self.nodes[v[1]],self.nodes[v[2]]]
    #         n=normalVector(p)
    #         for j in range(3):
    #             j3=i9+3*j
    #             geometry.elements[3*i+j]=elem
    #             geometry.nodes[3*i+j]=v[j]
    #             pos[j3]=p[j].x
    #             pos[j3+1]=p[j].y
    #             pos[j3+2]=p[j].z
    #             norm[j3]=n.x
    #             norm[j3+1]=n.y
    #             norm[j3+2]=n.z
    #             colors[j3]=meshColors[0]
    #             colors[j3+1]=meshColors[1]
    #             colors[j3+2]=meshColors[2]
    #             geometry.angle[j3]=0
    #             geometry.angle[j3+1]=0
    #             geometry.angle[j3+2]=0

    #     geometry.addAttribute('position',THREE.BufferAttribute(pos,3))
    #     geometry.addAttribute('normal',THREE.BufferAttribute(norm,3))
    #     geometry.addAttribute('color',THREE.BufferAttribute(colors,3))
    #     return geometry


    # # 要素辺の形状データを取り出す
    # def getEdgeGeometry(self):
    #     edges=self.faceEdges
    #     pos=new Float32Array(6*edges.length)
    #     geometry=THREE.BufferGeometry()
    #     geometry.nodes=new Int32Array(2*edges.length)
    #     geometry.angle=new Float32Array(6*edges.length)
    #     for i in range(len(edges)):
    #         i2=2*i
    #         i6=6*i
    #         v=edges[i].nodes
    #         p1=self.nodes[v[0]]
    #         p2=self.nodes[v[1]]
    #         geometry.nodes[i2]=v[0]
    #         geometry.nodes[i2+1]=v[1]
    #         pos[i6]=p1.x
    #         pos[i6+1]=p1.y
    #         pos[i6+2]=p1.z
    #         pos[i6+3]=p2.x
    #         pos[i6+4]=p2.y
    #         pos[i6+5]=p2.z
    #         for j in range(6):
    #             geometry.angle[i6+j]=0

    #     geometry.addAttribute('position',THREE.BufferAttribute(pos,3))
    #     return geometry


    # # 梁要素の形状データを取り出す
    # def getBarGeometry(self):
    #     geometry=THREE.BufferGeometry()
    #     geometry.param=[]
    #     geometry.dir=[]
    #     elems=self.elements
    #     bars=[]
    #     axis=[]
    #     for i in range(len(elems)):
    #         if(elems[i].isBar):
    #             bars.append(elems[i].border(i,0))
    #             geometry.param.append(self.barParams[elems[i].param].section)
    #             axis.append(elems[i].axis)

    #     pos=new Float32Array(6*bars.length)
    #     colors=new Float32Array(6*bars.length)
    #     geometry.elements=new Int32Array(2*bars.length)
    #     geometry.nodes=new Int32Array(2*bars.length)
    #     geometry.angle=new Float32Array(6*bars.length)
    #     for i in range(len(bars)):
    #         i2=2*i
    #         i6=6*i
    #         v=bars[i].nodes
    #         elem=bars[i].element
    #         p1=self.nodes[v[0]]
    #         p2=self.nodes[v[1]]
    #         geometry.dir.append(dirVectors([p1,p2],axis[i]))
    #         geometry.elements[i2]=elem
    #         geometry.elements[i2+1]=elem
    #         geometry.nodes[i2]=v[0]
    #         geometry.nodes[i2+1]=v[1]
    #         pos[i6]=p1.x
    #         pos[i6+1]=p1.y
    #         pos[i6+2]=p1.z
    #         pos[i6+3]=p2.x
    #         pos[i6+4]=p2.y
    #         pos[i6+5]=p2.z
    #         for j in range(3):
    #             colors[i6+j]=meshColors[j]
    #             colors[i6+j+3]=meshColors[j]
    #             geometry.angle[i6+j]=0
    #             geometry.angle[i6+j+3]=0

    #     geometry.addAttribute('position',THREE.BufferAttribute(pos,3))
    #     geometry.addAttribute('color',THREE.BufferAttribute(colors,3))
    #     return geometry



