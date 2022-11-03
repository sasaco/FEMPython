from Nodes import Nodes
from BoundaryCondition import Vector3R
import math
import numpy as np
#--------------------------------------------------------------------#

# データ型
NONE = -1		    # 空データ
DISPLACEMENT = 0    # 変位
STRAIN = 1		    # 歪
STRESS = 2		    # 応力
S_ENERGY = 3		# 歪エネルギー密度
TEMPERATURE = 4	    # 温度
# 成分
X = 0		        # x成分
Y = 1		        # y成分
Z = 2		        # z成分
RX = 3		        # x軸回転成分
RY = 4		        # y軸回転成分
RZ = 5		        # z軸回転成分
XY = 3		        # xyせん断成分
YZ = 4		        # yzせん断成分
ZX = 5		        # zxせん断成分
MAGNITUDE = 6	    # 大きさ
MAX_PRINCIPAL = 7	# 最大主成分
MIN_PRINCIPAL = 8	# 最小主成分
MID_PRINCIPAL = 9	# 中間主成分
MAX_SHARE = 10	    # 最大せん断成分
VON_MISES = 11	    # ミーゼス応力
SHIFT = 12		    # 成分シフト量
# 変位の成分
DISP_COMPONENT = ['Mag.','x','y','z']
DISP2_COMPONENT = ['Mag.','x','y','z','rotx','roty','rotz']
# 歪の成分
STRAIN_COMPONENT = ['Max.prin.','Min.prin.','Mid.prin.',
                    'Max.share',
                    'x','y','z','xy','yz','zx']
# 応力の成分
STRESS_COMPONENT = ['Max.prin.','Min.prin.','Mid.prin.',
                    'Max.share','Von mises',
                    'x','y','z','xy','yz','zx']
# 歪エネルギー密度の成分
ENERGY_COMPONENT = ['Energy']
COMP_MAP = {'Mag.':MAGNITUDE,'x':X,'y':Y,'z':Z,
            'rotx':RX,'roty':RY,'rotz':RZ,'xy':XY,'yz':YZ,'zx':ZX,
            'Max.prin.':MAX_PRINCIPAL,'Min.prin.':MIN_PRINCIPAL,
            'Mid.prin.':MID_PRINCIPAL,'Max.share':MAX_SHARE,
            'Von mises':VON_MISES,'Energy':0,
            'x 1':X,'y 1':Y,'z 1':Z,'xy 1':XY,'yz 1':YZ,'zx 1':ZX,
            'Max.prin. 1':MAX_PRINCIPAL,'Min.prin. 1':MIN_PRINCIPAL,
            'Mid.prin. 1':MID_PRINCIPAL,'Max.share 1':MAX_SHARE,
            'Von mises 1':VON_MISES,'Energy 1':0,
            'x 2':X+SHIFT,'y 2':Y+SHIFT,'z 2':Z+SHIFT,
            'xy 2':XY+SHIFT,'yz 2':YZ+SHIFT,'zx 2':ZX+SHIFT,
            'Max.prin. 2':MAX_PRINCIPAL+SHIFT,
            'Min.prin. 2':MIN_PRINCIPAL+SHIFT,
            'Mid.prin. 2':MID_PRINCIPAL+SHIFT,
            'Max.share 2':MAX_SHARE+SHIFT,
            'Von mises 2':VON_MISES+SHIFT,'Energy 2':1}
EIG_EPS = 1e-10		    # 固有値計算の収束閾値
NODE_DATA = 0		    # 節点データ
ELEMENT_DATA = 1		# 要素データ
VIBRATION = 'Vibration'	# 振動解析
BUCKLING = 'Buckling'	# 座屈解析


# Jacobie法で対称テンソルの固有値を求める
# Numeric.jsでは対角要素が0（例えばせん断のみの条件）だと求められない
# st - 対称テンソル
# iterMax - 反復回数の最大値
def eigenvalue(st,iterMax):
    m=[
        [st.xx,st.xy,st.zx],
        [st.xy,st.yy,st.yz],
        [st.zx,st.yz,st.zz]]
    return eigenByJacob(m,iterMax)


# Jacobie法で対称テンソルの固有値を求める
# m - 対称行列
# iterMax - 反復回数の最大値
def eigenByJacob(m,iterMax):
    size=len(m)
    dataMax=0
    ev=numeric.identity(size)
    for i in range(size):
        for j in range(size):
            dataMax=max(dataMax,abs(m[i][j]))

    tolerance=EIG_EPS*dataMax
    # 値が0の場合
    if(dataMax==0):
        return {
            "lambda":numeric.getDiag(m),
            "ev":ev
        }

    for iter in range(iterMax):
        im=0
        jm=0
        ndMax=0
        for i in range(2):
            for j in range(i+1,3):
                absm=abs(m[i][j])
                if absm>ndMax:
                    ndMax=absm
                    im=i
                    jm=j

        if ndMax<tolerance:
            break
        mim=m[im]
        mjm=m[jm]
        alpha=0.5*(mim[im]-mjm[jm])
        beta=0.5/math.sqrt(alpha*alpha+ndMax*ndMax)
        cc2=0.5+abs(alpha)*beta
        cs=-beta*mim[jm]
        if alpha<0:
            cs=-cs
        cc=math.sqrt(cc2)
        ss=cs/cc
        aij=2*(alpha*cc2-mim[jm]*cs)
        aii=mjm[jm]+aij
        ajj=mim[im]-aij
        for i in range(3):
            mi=m[i]
            evi=ev[i]
            a1=mi[im]*cc-mi[jm]*ss
            a2=mi[im]*ss+mi[jm]*cc
            mi[im]=a1
            mi[jm]=a2
            mim[i]=a1
            mjm[i]=a2
            a1=evi[im]*cc-evi[jm]*ss
            a2=evi[im]*ss+evi[jm]*cc
            evi[im]=a1
            evi[jm]=a2

        mim[im]=aii
        mim[jm]=0
        mjm[im]=0
        mjm[jm]=ajj

    m=numeric.getDiag(m)

    # 固有値を大きい順に入れ替える
    eig=[]
    ev=numeric.transpose(ev)

    for i in range(size):
        eig.append([m[i],ev[i]])

    sorted(eig, key=lambda v1, v2: v2[0]-v1[0])

    for i in range(size):
        m[i]=eig[i][0]
        ev[i]=eig[i][1]

    return {
        "lambda":m,
        "ev":numeric.transpose(ev)
    }


# 計算結果の成分を表示する
# sel - コンボボックス
# component - 成分
# data - データ番号（1:表面,2:裏面,-1:番号なし）
def setOptions(sel,component,data):
    for i in range(len(component)):
        c=component[i]
        if data>0:
            c += ' '+data
        sel.appendChild(createOption(c,COMP_MAP[c]))


# オプション要素を作成する
# text - オプションのテキスト
# value - オプションの値
def createOption(text,value):
    opt=document.createElement('option')
    opt.value=value
    opt.text=text
    return opt


# コンボボックスのオプションを削除する
# sel - コンボボックス
def removeOptions(sel):
    if sel.hasChildNodes():
        while len(sel.childNodes)>0:
            sel.removeChild(sel.firstChild)


#--------------------------------------------------------------------#
# 計算結果
class Result():
    def __init__(self):
        self.type=NODE_DATA		# データ保持形態：節点データ
        self.clear()

    # 計算結果を消去する
    def clear(self):
        self.displacement=[]		# 変位
        self.strain1=[]		# 節点歪
        self.strain2=[]
        self.stress1=[]		# 節点応力
        self.stress2=[]
        self.sEnergy1=[]		# 節点歪エネルギー密度
        self.sEnergy2=[]
        self.temperature=[]		# 節点温度
        self.dispMax=0		# 変位の大きさの最大値
        self.angleMax=0		# 回転角の大きさの最大値
        self.tempMax=0		# 温度の最大値
        self.eigenValue=[]		# 固有値データ
        self.calculated=False	# 計算前＝計算結果無し
        self.value=[]		# コンター図データ
        self.minValue=0		# コンター図データ最小値
        self.maxValue=0		# コンター図データ最大値


    # 節点変位を設定する
    # bc - 境界条件
    # disp - 節点変位を表すベクトル
    # nodeCount - 節点数
    def setDisplacement(self, bc, disp, nodeCount):
        self.displacement=[]		# 変位
        self.dispMax=0
        self.angleMax=0
        rests=bc.restraints
        ii=0
        for i in range(nodeCount):
            v=Vector3R()
            i0=bc.nodeIndex[i]
            bcDof=bc.dof[i]
            r=-1
            x=v.x
            for j in range(bcDof):
                bcl=bc.bcList[i0+j]
                if(bcl<0):
                    x[j]=disp[ii]
                    ii+=1
                else:
                    r=int(bcl/6)
                    x[j]=rests[r].x[j]

            if r>=0 and rests[r].coords:
                v.x=rests[r].coords.toGlobal(x)

            self.dispMax=max(self.dispMax,v.magnitude())
            self.angleMax=max(self.angleMax,v.magnitudeR())
            self.displacement.append(v)

        self.calculated=True


    # 節点温度を設定する
    # bc - 境界条件
    # t - 節点温度を表すベクトル
    # nodeCount - 節点数
    def setTemperature(self, bc, t, nodeCount):
        self.temperature=[]		# 節点温度
        temp=bc.temperature
        ii=0
        for i in range(nodeCount):
            tt = 0
            if(bc.bcList[i]<0):
                tt=t[ii]
                ii+=1

            else:
                tt=temp[bc.bcList[i]].t

            self.tempMax=max(self.tempMax,tt)
            self.temperature.append(tt)

        self.calculated=True


    # 節点の構造解析結果に値を加える
    # i - 節点のインデックス
    # eps1,str1,se1,eps2,str2,se2 - 表面・裏面の歪，応力，歪エネルギー密度
    def addStructureData(self, i,eps1,str1,se1,eps2,str2,se2):
        self.strain1[i].add(eps1)
        self.stress1[i].add(str1)
        self.sEnergy1[i]+=se1
        self.strain2[i].add(eps2)
        self.stress2[i].add(str2)
        self.sEnergy2[i]+=se2


    # 節点の構造解析結果に値を掛ける
    # i - 節点のインデックス
    # coef - 計算結果に掛ける係数
    def mulStructureData(self, i,coef):
        self.strain1[i].mul(coef)
        self.stress1[i].mul(coef)
        self.sEnergy1[i]*=coef
        self.strain2[i].mul(coef)
        self.stress2[i].mul(coef)
        self.sEnergy2[i]*=coef


    # 固有値データを追加する
    # ev - 固有値
    def addEigenValue(self, ev):
        self.eigenValue.append(ev)
        self.calculated=True


    # コンター図データを設定する
    # param - データの種類
    # component - データの成分
    # data - コンター図参照元
    def setContour(self, param, component, data):
        if(param<0):
            return
        data=data or self
        dpara=[
            data.displacement, 
            data.strain1, 
            data.stress1, 
            data.sEnergy1,
            data.temperature
        ]
        count=len(dpara[param])
        if(count==0):
            return
        self.value=[]
        self.value[0]=data.getData(param,component,0)
        self.minValue=self.value[0]
        self.maxValue=self.value[0]
        for i in range(count):
            self.value[i]=data.getData(param,component,i)
            self.minValue=min(self.minValue,self.value[i])
            self.maxValue=max(self.maxValue,self.value[i])


    # データを取り出す
    # param - データの種類
    # component - データの成分
    # index - 節点のインデックス
    def getData(self, param, component, index):
        if param==DISPLACEMENT:
            if component==X:
                pass
            elif component==Y:
                pass
            elif component==Z:
                pass
            elif component==RX:
                pass
            elif component==RY:
                pass
            elif component==RZ:
                    return self.displacement[index].x[component]
            elif component==MAGNITUDE:
                    return self.displacement[index].magnitude()
        elif param==STRAIN:
            if component<SHIFT:
                return self.getTensorComp(self.strain1[index],component)
            else:
                return self.getTensorComp(self.strain2[index],component-SHIFT)

        elif param==STRESS:
                if component<SHIFT:
                    return self.getTensorComp(self.stress1[index],component)
                else:
                    return self.getTensorComp(self.stress2[index],component-SHIFT)
        elif param==S_ENERGY:
                if component==0:
                    return self.sEnergy1[index]
                else:
                    return self.sEnergy2[index]
        elif param==TEMPERATURE:
                return self.temperature[index]

        return 0


    # 歪・応力を取り出す
    # s - 歪 or 応力
    # component - データの成分
    def getTensorComp(self, s, component):
        if component<6:
            return s.vector()[component]

        elif component<=10 :
            pri=s.principal()
            if(component==MAX_PRINCIPAL):
                return pri[0]
            elif(component==MIN_PRINCIPAL):
                return pri[2]
            elif(component==MID_PRINCIPAL):
                return pri[1]
            elif(component==MAX_SHARE):
                return 0.5*(pri[0]-pri[2])

        elif component==VON_MISES:
            return s.mises()

        return 0


    # 節点歪・応力を初期化する
    # count - 節点数
    def initStrainAndStress(self, count):
        zeros = [0,0,0,0,0,0]
        # self.strain1 = [Strain(zeros) for i in range(count)]		# 節点歪
        # self.strain2 = [Strain(zeros) for i in range(count)]
        # self.stress1 = [Stress(zeros) for i in range(count)]		# 節点応力
        # self.stress2 = [Stress(zeros) for i in range(count)]
        # self.sEnergy1 = [0 for i in range(count)]		# 節点歪エネルギー密度
        # self.sEnergy2 = [0 for i in range(count)]
        for i in range(count):
            self.strain1.append(Strain(zeros))
            self.strain2.append(Strain(zeros))
            self.stress1.append(Stress(zeros))
            self.stress2.append(Stress(zeros))
            self.sEnergy1.append(0)
            self.sEnergy2.append(0)


    # データ文字列を返す
    # nodes - 節点
    # elems - 要素
    def toStrings(self, nodes, elems):
        s=[]
        tuple = None
        if self.type==ELEMENT_DATA:
            s.append('ResultType\tElement')
            tuple=elems
        else:
            s.append('ResultType\tNode')
            tuple=nodes

        for i in range(len(self.displacement)):
            s.append('Displacement\t'+nodes[i].label.toString(10)+'\t'+
                        self.displacement[i].x.join('\t'))

        for i in range(len(self.strain1)):
            s.append('Strain1\t'+tuple[i].label.toString(10)+'\t'+
                        self.strain1[i].vector().join('\t'))

        for i in range(len(self.stress1)):
            s.append('Stress1\t'+tuple[i].label.toString(10)+'\t'+
                        self.stress1[i].vector().join('\t'))

        for i in range(len(self.sEnergy1)):
            s.append('StrEnergy1\t'+tuple[i].label.toString(10)+'\t'+
                        self.sEnergy1[i])

        for i in range(len(self.strain2)):
            s.append('Strain2\t'+tuple[i].label.toString(10)+'\t'+
                        self.strain2[i].vector().join('\t'))

        for i in range(len(self.stress2)):
            s.append('Stress2\t'+tuple[i].label.toString(10)+'\t'+
                        self.stress2[i].vector().join('\t'))

        for i in range(len(self.sEnergy2)):
            s.append('StrEnergy2\t'+tuple[i].label.toString(10)+'\t'+
                        self.sEnergy2[i])

        for i in range(len(self.temperature)):
            s.append('Temp\t'+nodes[i].label.toString(10)+'\t'+
                        self.temperature[i])

        for i in range(len(self.eigenValue)):
            #Array.prototype.push.apply(s,self.eigenValue[i].toStrings(nodes,tuple))
            for e in self.eigenValue[i].toStrings(nodes,tuple):
                s.append(e)

        return s


#--------------------------------------------------------------------#
# 固有値
# value - 固有値・固有振動数
# type - 解析種類
class EigenValue:
    def __init__(self, value, type):
        self.value=value
        self.type=type
        self.displacement=[]		# 変位
        self.sEnergy1=[]		# 節点歪エネルギー密度
        self.sEnergy2=[]
        self.dispMax=0
        self.angleMax=0

    # 変位を設定する
    # bc - 境界条件
    # disp - 変位を表す固有ベクトル
    # nodeCount - 節点数
    def setDisplacement(self, bc, disp, nodeCount):
        self.displacement=[]
        self.dispMax=0
        self.angleMax=0
        rests=bc.restraints
        ii=0
        for i in range(nodeCount):
            v = Vector3R()
            i0=bc.nodeIndex[i]
            bcDof=bc.dof[i]
            r=-1
            x=v.x
            for j in range(bcDof):
                bcl=bc.bcList[i0+j]
                if(bcl<0):
                    x[j]=disp[ii]
                    ii+=1
                else:
                    r=int(bcl/6)

            if r>=0 and rests[r].coords:
                v.x=rests[r].coords.toGlobal(x)

            self.dispMax=max(self.dispMax,v.magnitude())
            self.angleMax=max(self.angleMax,v.magnitudeR())
            self.displacement.append(v)


    # データを取り出す
    # param - データの種類
    # component - データの成分
    # index - 節点のインデックス
    def getData(self, param, component, index):
        if param==DISPLACEMENT:
                if component== X:
                    pass
                elif component== Y:
                    pass
                elif component== Z:
                    pass
                elif component== RX:
                    pass
                elif component== RY:
                    pass
                elif component== RZ:
                        return self.displacement[index].x[component]
                elif component== MAGNITUDE:
                        return self.displacement[index].magnitude()

        elif param==S_ENERGY:
            if component==0:
                return self.sEnergy1[index]
            else:
                return self.sEnergy2[index]

        return 0

    # 節点歪・応力を初期化する
    # count - 節点数
    def initStrainEnergy(self, count):
        self.sEnergy1=np.zeros(count)	# 節点歪エネルギー密度
        self.sEnergy2=np.zeros(count)


    # データ文字列を返す
    # nodes - 節点
    # tuple - 節点or要素
    def toStrings(self, nodes, tuple):
        s=[]
        s.append('EigenValue\t'+self.type+'\t'+self.value)
        for i in range(len(self.displacement)):
            s.append('Displacement\t'+nodes[i].label.toString(10)+'\t'+
                        self.displacement[i].x.join('\t'))

        for i in range(len(self.sEnergy1)):
            s.append('StrEnergy1\t'+tuple[i].label.toString(10)+'\t'+
                        self.sEnergy1[i])

        for i in range(len(self.sEnergy2)):
            s.append('StrEnergy2\t'+tuple[i].label.toString(10)+'\t'+
                        self.sEnergy2[i])

        return s


#--------------------------------------------------------------------#
# ３次元対称テンソル
# s - 成分
class SymmetricTensor3:
    def __init__(self, s):
        self.xx=s[0]
        self.yy=s[1]
        self.zz=s[2]
        self.xy=s[3]
        self.yz=s[4]
        self.zx=s[5]


    # テンソルをベクトルとして返す
    def vector(self):
        return [self.xx,self.yy,self.zz,self.xy,self.yz,self.zx]


    # テンソルを加える
    # t - 加えるテンソル
    def add(self, t):
        self.xx+=t.xx
        self.yy+=t.yy
        self.zz+=t.zz
        self.xy+=t.xy
        self.yz+=t.yz
        self.zx+=t.zx


    # 成分にスカラーを掛ける
    # a - 掛けるスカラー
    def mul(self, a):
        self.xx*=a
        self.yy*=a
        self.zz*=a
        self.xy*=a
        self.yz*=a
        self.zx*=a


    # 固有値を返す
    def principal(self):
        return eigenvalue(self, 100)['lambda']


    # テンソルを回転させる
    # d - 方向余弦マトリックス
    def rotate(self, d):
        mat=[
            [self.xx,self.xy,self.zx],
            [self.xy,self.yy,self.yz],
            [self.zx,self.yz,self.zz]
        ]
        s=[0,0,0,0,0,0]

        for i in range(3):
            for j in range(3):
                mij=mat[i][j]
                for k in range(3):
                    s[k]+=d[k][i]*d[k][j]*mij
                    s[k+3]+=d[k][i]*d[(k+1)%3][j]*mij

        self.xx=s[0]
        self.yy=s[1]
        self.zz=s[2]
        self.xy=s[3]
        self.yz=s[4]
        self.zx=s[5]


    # テンソルの内積を計算する
    # t - 相手のテンソル
    def innerProduct(self, t):
        return self.xx * t.xx + self.yy * t.yy + self.zz * t.zz +\
            2 * ( self.xy * t.xy + self.yz * t.yz + self.zx * t.zx )


#--------------------------------------------------------------------#
# 歪
# s - 成分
class Strain(SymmetricTensor3):
    def __init__(self, s):
        super().__init__(s)
        self.xy=0.5*s[3]
        self.yz=0.5*s[4]
        self.zx=0.5*s[5]


    # テンソルをベクトルとして返す
    def vector(self):
        return [self.xx,self.yy,self.zz,2*self.xy,2*self.yz,2*self.zx]


#--------------------------------------------------------------------#
# 応力
# s - 成分
class Stress(SymmetricTensor3):
    def __init__(self, s):
        super().__init__(s)


    # ミーゼス応力を返す
    def mises(self):
        dxy = self.xx - self.yy
        dyz = self.yy - self.zz
        dzx = self.zz - self.xx
        ss = dxy * dxy + dyz * dyz + dzx * dzx
        tt = self.xy * self.xy + self.yz * self.yz + self.zx * self.zx
        return math.sqrt(0.5 * ss + 3 * tt)

