import math
import numpy as np
# from Vector3 import Vector3
#--------------------------------------------------------------------#

CIRCLE_DIV = 16			                # 円形断面表示オブジェクトの分割数
CIRCLE_DTH = 2 * math.pi / CIRCLE_DIV	# 円形断面の１分割の角度
COEF_K1 = 64 / math.pow(math.pi, 5)	    # 矩形断面の捩り係数
COEF_K = 8 / (math.pi * math.pi)
#--------------------------------------------------------------------#
KS_RECT = 5/6			# 矩形断面のせん断補正係数
KS_CIRCLE = 6/7			# 円形断面のせん断補正係数

# 矩形断面の捩り係数を求める
# ba - 辺の長さ比b/a
def rectCoef(ba):
    dk1s = 0
    dks = 0
    dbs = 0
    pba = 0.5 * math.pi * ba
    i = 1
    sg = 1
    while dk1 / dk1s > 1e-10:
        ex = math.exp(-2 * pba * i)
        dk1 = (1 - ex) / ((i+ex)*math.pow(i, 5))	# IEは双曲線関数未実装
        dk1s += dk1
        i += 2

    i = 1
    while dk / dks > 1e-10:
        dk = 2 / ((math.exp(pba*i) + math.exp(-pba*i)) * i * i)
        dks += dk
        i += 2

    i = 1
    while abs(db/dbs) > 1e-12:
        ex = math.exp(-2 * pba * i)
        db = sg * (1-ex) / ((i+ex) * i * i)
        dbs += db
        i += 2
        sg =- sg

    k1=1/3-COEF_K1*dk1s/ba
    k=1-COEF_K*dks
    b=COEF_K*dbs
    return [k1,k,k1/k,b/k]


#--------------------------------------------------------------------#
# 円形断面
# ss - データ文字列
class CircleSection:
    def __init__(self,ss):
        self.d1=float(ss[0])	# 外径
        self.d2=float(ss[1])	# 内径
        # 断面積
        self.area=0.25*math.pi*(self.d1*self.d1-self.d2*self.d2)
        # 断面２次モーメント
        self.iy=0.0625*self.area*(self.d1*self.d1+self.d2*self.d2)
        self.iz=self.iy
        self.ip=self.iy+self.iz	# 断面２次極モーメント

    # 断面の頂点数を返す
    def vertexCount(self):
        return CIRCLE_DIV

    # せん断補正係数を返す
    def shearCoef(self):
        return KS_CIRCLE

    # 歪・応力ベクトルを返す
    # material - 材料
    # ex - 引張圧縮歪
    # thd - 比捩り角
    # kpy,kpz - 曲げによる曲率
    # sy,sz - 断面せん断歪
    def strainStress(self,material,ex,thd,kpy,kpz,sy,sz):
        kp=math.sqrt(kpy*kpy+kpz*kpz)
        kpr=0.5*kp*self.d1
        gx=0.5*self.d1*thd
        gy=0
        gz=gx
        if kp>0:
            gy=-gx*kpz/kp
            gz=gx*kpy/kp

        ee=material.ee
        gg=material.gg
        return [[ex+kpr,0,0,gy+sy,0,gz+sz],
                        [ee*(ex+kpr),0,0,gg*(gy+sy),0,gg*(gz+sz)],
                        [ex-kpr,0,0,-gy+sy,0,-gz+sz],
                        [ee*(ex-kpr),0,0,gg*(-gy+sy),0,gg*(-gz+sz)]]


    # 断面表示モデルの座標系を設定する
    # pos1,pos2 - 外径，内径の座標
    # cx,cy,cz - 中心点座標
    # v1,v2 - 軸方向，断面基準方向ベクトル
    def setCoords(self,pos1,pos2,cx,cy,cz,v1,v2):
        r1=0.5*self.d1
        r2=0.5*self.d2
        for j in range(0,len(pos1),3):
            pos1[j]=cx+r1*v2.x
            pos1[j+1]=cy+r1*v2.y
            pos1[j+2]=cz+r1*v2.z
            pos2[j]=cx+r2*v2.x
            pos2[j+1]=cy+r2*v2.y
            pos2[j+2]=cz+r2*v2.z
            v2.applyAxisAngle(v1,CIRCLE_DTH)


    # 質量・重心周りの慣性モーメントを返す
    # dens - 密度
    # l - 要素長さ
    def massInertia(self,dens,l):
        dl=dens*l
        dly=dl*self.iy
        return [dl*self.area,2*dly,dly,dly]


    # 断面を表す文字列を返す
    def toString(self):
        return self.d1+'\t'+self.d2


#--------------------------------------------------------------------#
# 矩形断面
# ss - データ文字列
class RectSection:
    def __init__(self,ss):
        b1=float(ss[0])
        h1=float(ss[1])
        b2=float(ss[2])
        h2=float(ss[3])
        self.b1=b1	# 外側幅
        self.h1=h1	# 外側高さ
        self.b2=b2	# 内側幅
        self.h2=h2	# 内側高さ
        # 断面積
        self.area=b1*h1-b2*h2
        # 断面２次モーメント
        i11=b1*b1*b1*h1
        i12=b1*h1*h1*h1
        i21=b2*b2*b2*h2
        i22=b2*h2*h2*h2
        self.iy=(i11-i21)/12
        self.iz=(i12-i22)/12
        sw1=(b1>=h1)
        sw2=(b2>=h2)
        if sw1==True:
            k1=rectCoef(b1/h1)
            ip1=k1[0]*i12
            self.zy=k1[1]*h1
            self.zz=k1[3]*self.zy
        else:
            k1=rectCoef(h1/b1)
            ip1=k1[0]*i11
            self.zz=k1[1]*b1
            self.zy=k1[3]*self.zz
        if i22==0:
            ip2=0
        elif sw2==True:
            k2=rectCoef(b2/h2)
            ip2=k2[0]*i22
        else:
            k2=rectCoef(h2/b2)
            ip2=k2[0]*i21
        self.ip=ip1-ip2		# 断面２次極モーメント


    # 断面の頂点数を返す
    def vertexCount(self):
        return 4

    # せん断補正係数を返す
    def shearCoef(self):
        return KS_RECT

    # 歪・応力ベクトルを返す
    # material - 材料
    # ex - 引張圧縮歪
    # thd - 比捩り角
    # kpy,kpz - 曲げによる曲率
    # sy,sz - 断面せん断歪
    def strainStress(self,material,ex,thd,kpy,kpz,sy,sz):
        sby=0.5*kpy*self.b1
        sbz=0.5*kpz*self.h1
        gy=self.zy*thd
        gz=self.zz*thd
        ee=material.ee
        gg=material.gg
        eps=[[ex+sby,sy,sz+gz],[ex+sby+sbz,sy,sz],[ex+sbz,sy-gy,sz],
                        [ex-sby+sbz,sy,sz],[ex-sby,sy,sz-gz],[ex-sby-sbz,sy,sz],
                        [ex-sbz,sy+gy,sz],[ex+sby-sbz,sy,sz]]
        imax=0
        enmax=0
        for i in range(8):
            ei=eps[i]
            en=ee*ei[0]*ei[0]+gg*(ei[1]*ei[1]+ei[2]*ei[2])
            if en>enmax:
                imax=i
                enmax=en

        if eps[imax][0]<0:
            imax=(imax+4)%8
        eps1=eps[imax]
        eps2=eps[(imax+4)%8]
        return [[eps1[0],0,0,eps1[1],0,eps1[2]],
                        [ee*eps1[0],0,0,gg*eps1[1],0,gg*eps1[2]],
                        [eps2[0],0,0,eps2[1],0,eps2[2]],
                        [ee*eps2[0],0,0,gg*eps2[1],0,gg*eps2[2]]]


    # 断面表示モデルの座標系を設定する
    # pos1,pos2 - 外径，内径の座標
    # cx,cy,cz - 中心点座標
    # v1,v2 - 軸方向，断面基準方向ベクトル
    def setCoords(self, pos1, pos2, cx, cy, cz, v1: np.ndarray, v2: np.ndarray):
        v3 = np.cross(v1, v2)
        c1 = [0.5, -0.5, -0.5, 0.5, 0.5]
        c2 = [0.5, 0.5, -0.5, -0.5, 0.5]
        for j in range(len(c1)):
            j3 = 3*j
            pos1[j3] = cx + c1[j] * self.b1 * v2[0] + c2[j] * self.h1 * v3[0]
            pos1[j3+1] = cy + c1[j] * self.b1 * v2[1] + c2[j] * self.h1 * v3[1]
            pos1[j3+2] = cz + c1[j] * self.b1 * v2[2] + c2[j] * self.h1 * v3[2]
            pos2[j3] = cx + c1[j] * self.b2 * v2[0] + c2[j] * self.h2 * v3[0]
            pos2[j3+1] = cy + c1[j] * self.b2 * v2[1] + c2[j] * self.h2 * v3[1]
            pos2[j3+2] = cz + c1[j] * self.b2 * v2[2] + c2[j] * self.h2 * v3[2]


    # 質量・重心周りの慣性モーメントを返す
    # dens - 密度
    # l - 要素長さ
    def massInertia(self, dens, l):
        dl = dens * l
        dly = dl * self.iz
        dlz = dl * self.iy
        return [dl * self.area, dly + dlz, dly, dlz]


    # 断面を表す文字列を返す
    def toString(self):
        return self.b1 + '\t' + self.h1 + '\t' + self.b2 + '\t' + self.h2

