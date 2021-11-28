from BoundaryCondition import compareNodeLabel, compareElementLabel
import math
#--------------------------------------------------------------------#

# 節点ラベルを比較する
# bc1,bc2 - 比較する境界条件
def compareNodeLabel(bc1,bc2):
    if(bc1.node<bc2.node):
        return -1
    elif(bc1.node>bc2.node):
        return 1
    return 0


# 要素ラベルを比較する
# bc1,bc2 - 比較する境界条件
def compareElementLabel(bc1,bc2):
    if(bc1.element<bc2.element):
        return -1
    elif(bc1.element>bc2.element):
        return 1
    return 0


#--------------------------------------------------------------------#
# 境界条件
class BoundaryCondition():
    def __init__(self):
        self.clear()    
        self.dof=[]			# 節点の自由度
        self.nodeIndex=[]		# 荷重ベクトルの節点ポインタ
        self.bcList=[]		# 境界条件を設定した節点のリスト

    # データを消去する
    def clear(self):
        self.restraints=[]		# 拘束条件
        self.loads=[]		# 荷重条件
        self.pressures=[]		# 面圧条件
        self.temperature=[]		# 節点温度条件
        self.htcs=[]			# 熱伝達境界条件
        self.loadMax=0		# 最大荷重
        self.pressMax=0		# 最大面圧


    # 境界条件を初期化する
    def init(self):
        self.restraints.sort(compareNodeLabel)
        self.loads.sort(compareNodeLabel)
        self.pressures.sort(compareElementLabel)
        self.temperature.sort(compareNodeLabel)
        self.htcs.sort(compareElementLabel)
        self.loadMax=0
        self.pressMax=0

        for i in range(len(self.loads)):
            self.loadMax=max(self.loadMax,self.loads[i].magnitude())

        for i in range(len(self.pressures)):
            self.pressMax=max(self.pressMax,self.pressures[i].press)


    # 構造解析の節点ポインタを設定する
    # count - 節点数
    def setPointerStructure(self, count):
        self.nodeIndex=[]		# 荷重ベクトルの節点ポインタ
        self.bcList=[]		# 境界条件を設定した節点のリスト
        dofAll=0
        for i in range(count):
            self.nodeIndex[i]=dofAll
            dofAll+=self.dof[i]

        for i in range(dofAll):
            self.bcList[i]=-1

        for i in range(len(self.restraints)):
            r=self.restraints[i]
            index0=self.nodeIndex[r.node]
            rdof=self.dof[r.node]
            for j in range(rdof):
                if(r.rest[j]):
                    self.bcList[index0+j]=6*i+j

        return dofAll


    # 熱解析の節点ポインタを設定する
    # count - 節点数
    def setPointerHeat(self, count):
        self.dof=[]			# 節点の自由度
        self.nodeIndex=[]		# 荷重ベクトルの節点ポインタ
        self.bcList=[]		# 境界条件を設定した節点のリスト
        temps=len(self.temperature)
        for i in range(count):
            self.bcList[i]=-1

        for i in range(temps):
            t=self.temperature[i]
            self.bcList[t.node]=i

        return temps


    # 強制変位を返す
    # bc - 変位自由度ポインタ
    def getRestDisp(self, bc):
        return self.restraints[int(bc/6)].x[bc%6]


    # データ文字列を返す
    # nodes - 節点
    # elems - 要素
    def toStrings(self, nodes, elems):
        s=[]
        for i in range(len(self.restraints)):
            s.append(self.restraints[i].toString(nodes))

        for i in range(len(self.loads)):
            s.append(self.loads[i].toString(nodes))

        for i in range(len(self.pressures)):
            s.append(self.pressures[i].toString(elems))

        for i in range(len(self.temperature)):
            s.append(self.temperature[i].toString(nodes))

        for i in range(len(self.htcs)):
            s.append(self.htcs[i].toString(elems))

        return s


#--------------------------------------------------------------------#
# ３次元ベクトル（並進＋回転）
# x,y,z - x,y,z成分
# rx,ry,rz - x,y,z軸周り回転角
class Vector3R:
    def __init__(self, x=0, y=0, z=0, rx=0, ry=0, rz=0):
        self.x=[x,y,z,rx,ry,rz]


    # 並進成分の大きさを返す
    def magnitude(self):
        return math.sqrt(self.magnitudeSq())


    # 並進成分の大きさの2乗を返す
    def magnitudeSq(self):
        return self.x[0]*self.x[0]+self.x[1]*self.x[1]+self.x[2]*self.x[2]


    # 回転成分の大きさを返す
    def magnitudeR(self):
        return math.sqrt(self.magnitudeSqR())


    # 回転成分の大きさの2乗を返す
    def magnitudeSqR(self):
        return self.x[3]*self.x[3]+self.x[4]*self.x[4]+self.x[5]*self.x[5]

    # ベクトルのコピーを返す
    def clone(self):
        return Vector3R(self.x[0],self.x[1],self.x[2],
                                        self.x[3],self.x[4],self.x[5])


#--------------------------------------------------------------------#
# 要素境界条件
# element - 要素ラベル
# face - 要素境界面
class ElementBorderBound:
    def __init__(self, element, face):
        self.element=element
        self.face=face


    # 要素境界を返す
    # elem - 要素
    def getBorder(self, elem):
        if len(self.face.length)==2:

            if self.face.charAt(0)=='F':
                j=int(self.face.charAt(1))-1
                return elem.border(self.element,j)

            elif self.face.charAt(0)=='E':
                j=int(self.face.charAt(1))-1
                return elem.borderEdge(self.element,j)

        return None


#--------------------------------------------------------------------#
# 拘束条件
# node - 節点ラベル
# coords - 局所座標系
# restx,resty,restz - x,y,z方向の拘束の有無
# x,y,z - 強制変位のx,y,z成分
# restrx,restry,restrz - x,y,z方向の回転拘束の有無
# rx,ry,rz - 強制変位のx,y,z軸周り回転角
class Restraint(Vector3R):
    def __init__(self, node,coords, restx, resty, restz, x, y, z,
                                                                    restrx,restry,restrz,rx,ry,rz):
        super().__init__(x,y,z,rx,ry,rz)
        self.node=node
        self.coords=coords
        self.rest=[restx,resty,restz,restrx,restry,restrz]
        self.globalX=self.x


    # 拘束条件を表す文字列を返す
    # nodes - 節点
    def toString(self, nodes):
        s='Restraint\t'+nodes[self.node].label.toString(10)
        for i in range(6):
            if(self.rest[i]):
                s+='\t1\t'+self.x[i]

            else:
                s+='\t0\t'+self.x[i]

        if(self.coords):
            s+='\t'+self.coords.label.toString(10)

        return s


#--------------------------------------------------------------------#
# 荷重条件
# node - 節点ラベル
# coords - 局所座標系
# x,y,z - x,y,z成分
# rx,ry,rz - x,y,z軸周り回転成分
class Load(Vector3R):
    def __init__(self, node,coords,x,y,z,rx,ry,rz):
        super().__init__(x,y,z,rx,ry,rz)
        self.node=node
        self.coords=coords
        self.globalX=self.x

    # 荷重条件を表す文字列を返す
    # nodes - 節点
    def toString(self, nodes):
        s='Load\t'+nodes[self.node].label.toString(10)+'\t'+ \
                    self.x.join('\t')
        if self.coords:
            s+='\t'+self.coords.label.toString(10)

        return s


#--------------------------------------------------------------------#
# 面圧条件
# element - 要素ラベル
# face - 要素境界面
# press - 面圧
class Pressure(ElementBorderBound):
    def __init__(self, element,face,press):
        super().__init__(element,face)
        self.press=press

    # 面圧条件を表す文字列を返す
    # elems - 要素
    def toString(self, elems):
        return 'Pressure\t'+elems[self.element].label.toString(10)+'\t'+ \
                        self.face+'\t'+self.press


#--------------------------------------------------------------------#
# 温度固定条件
# node - 節点ラベル
# t - 温度
class Temperature:
    def __init__(self, node, t):
        self.node=node
        self.t=t

    # 温度固定条件を表す文字列を返す
    # nodes - 節点
    def toString(self, nodes):
        return 'Temperature\t'+nodes[self.node].label.toString(10)+'\t'+ \
                    self.t


#--------------------------------------------------------------------#
# 熱伝達境界条件
# element - 要素ラベル
# face - 要素境界面
# htc - 熱伝達率
# outTemp - 外部温度
class HeatTransferBound(ElementBorderBound):
    def __init__(self, element,face,htc,outTemp):
        super().__init__(element,face)
        self.htc=htc
        self.outTemp=outTemp

    # 熱伝達境界条件を表す文字列を返す
    # elems - 要素
    def toString(self, elems):
        return 'HTC\t'+elems[self.element].label.toString(10)+'\t'+ \
                        self.face+'\t'+self.htc+'\t'+self.outTemp
