from Element import FElement, dirMatrix, toDir3, C1_3
from ElementBorder import EdgeBorder1
from Result import Strain, Stress
#--------------------------------------------------------------------#

I_YMZ=[1,5,7,11]	# y軸方向,z軸周り成分のインデックス
I_ZMY=[2,4,8,10]	# z軸方向,y軸周り成分のインデックス

#--------------------------------------------------------------------#
# 梁要素
# label - 要素ラベル
# material - 材料のインデックス
# param - 梁パラメータのインデックス
# nodes - 節点番号
# axis - 断面基準方向ベクトル
class BarElement(FElement):
	def __init__(self,label,material,param,nodes,axis):
		super().__init__(label,material,nodes)
		self.param=param
		self.isBar=True
		if axis!=None:
			axis.normalize()
		self.axis=axis


	# 節点数を返す
	def nodeCount(self):
		return 2

	# 要素境界数を返す
	def borderCount(self):
		return 1

	# 要素境界を返す
	# element - 要素ラベル
	# index - 要素境界のインデックス
	def border(self, element, index):
		p=self.nodes
		if(index==0):
			return EdgeBorder1(element,[p[0],p[1]])

		return None

	# 要素境界辺を返す 梁要素では要素境界と同じ
	# element - 要素ラベル
	# index - 要素境界辺のインデックス
	def borderEdge(self, element, index):
		return self.border(element,index)


	# 要素節点の角度を返す 梁要素では１
	# p - 要素節点
	def angle(self, p):
		return [1,1]


	# 剛性マトリックスを返す
	# p - 要素節点
	# material - 材料
	# sect - 梁断面パラメータ
	def stiffnessMatrix(self, p, material, sect):
		kk=numeric.rep([12,12],0)
		l=p[0].distanceTo(p[1])
		d=dirMatrix(p,self.axis)
		kx=material.ee*sect.area/l
		kt=material.gg*sect.ip/l
		kk[0][0]=kx
		kk[0][6]=-kx
		kk[6][0]=-kx
		kk[6][6]=kx
		kk[3][3]=kt
		kk[3][9]=-kt
		kk[9][3]=-kt
		kk[9][9]=kt
		km=self.stiffBend(l,material,sect)
		for i in range(4):
			kk1=kk[I_YMZ[i]]
			kk2=kk[I_ZMY[i]]
			kmi1=km[i]
			kmi2=km[i+4]
			for j in range(4):
				kk1[I_YMZ[j]]=kmi1[j]
				kk2[I_ZMY[j]]=kmi2[j]

		toDir3(d,kk)
		return kk


	# 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
	# p - 要素節点
	# coef - 係数
	# sect - 梁断面パラメータ
	def gradMatrix(self, p, coef, sect):
		c=coef*sect.area/p[0].distanceTo(p[1])
		return [[c,-c],[-c,c]]


	# 幾何剛性マトリックスを返す
	# p - 要素節点
	# u - 節点変位
	# material - 材料
	# sect - 梁断面パラメータ
	def geomStiffnessMatrix(self, p, u, material, sect):
		l2=p[0].distanceToSquared(p[1])
		d=dirMatrix(p,self.axis)
		v=self.toLocalArray(u,d)
		kk=numeric.rep([12,12],0)
		sx=material.ee*sect.area*(v[6]-v[0])/l2
		for i in range(3):
			kk[i][i]=sx
			kk[i+6][i]=-sx
			kk[i][i+6]=-sx
			kk[i+6][i+6]=sx

		toDir3(d,kk)
		return kk


	# 節点歪・応力を返す
	# p - 要素節点
	# u - 節点変位
	# material - 材料
	# sect - 梁断面パラメータ
	def strainStress(self, p, u, material, sect):
		l=p[0].distanceTo(p[1])
		d=dirMatrix(p,self.axis)
		v=self.toLocalArray(u,d)
		strain1=[]
		stress1=[]
		energy1=[]
		strain2=[]
		stress2=[]
		energy2=[]
		ex=(v[6]-v[0])/l
		thd=(v[9]-v[3])/l
		ks=self.bendCurveShare(v,l,material,sect)
		kpy=ks[0]
		kpz=ks[1]
		sy=ks[2]
		sz=ks[3]
		for i in range(2):
			str=sect.strainStress(material,ex,thd,kpy[i],kpz[i],sy,sz)
			strain1[i]= Strain(str[0])
			stress1[i]= Stress(str[1])
			strain2[i]= Strain(str[2])
			stress2[i]= Stress(str[3])
			strain1[i].rotate(d)
			stress1[i].rotate(d)
			strain2[i].rotate(d)
			stress2[i].rotate(d)
			energy1[i]=0.5*strain1[i].innerProduct(stress1[i])
			energy2[i]=0.5*strain2[i].innerProduct(stress2[i])

		return [strain1,stress1,energy1,strain2,stress2,energy2]


	# 要素歪・応力を返す
	# p - 要素節点
	# u - 節点変位
	# material - 材料
	# sect - 梁断面パラメータ
	def elementStrainStress(self, p, u, material, sect):
		l=p[0].distanceTo(p[1])
		d=dirMatrix(p,self.axis)
		v=self.toLocalArray(u,d)
		str=[]
		ex=(v[6]-v[0])/l
		thd=(v[9]-v[3])/l
		ks=self.bendCurveShare(v,l,material,sect)
		kpy=ks[0]
		kpz=ks[1]
		sy=ks[2]
		sz=ks[3]
		for i in range(2):
			str[i]=sect.strainStress(material,ex,thd,kpy[i],kpz[i],sy,sz)

		strain1=Strain(numeric.mul(0.5,numeric.add(str[0][0],str[1][0])))
		stress1=Stress(numeric.mul(0.5,numeric.add(str[0][1],str[1][1])))
		strain2=Strain(numeric.mul(0.5,numeric.add(str[0][2],str[1][2])))
		stress2=Stress(numeric.mul(0.5,numeric.add(str[0][3],str[1][3])))
		strain1.rotate(d)
		stress1.rotate(d)
		strain2.rotate(d)
		stress2.rotate(d)
		energy1=0.5*strain1.innerProduct(stress1)
		energy2=0.5*strain2.innerProduct(stress2)
		return [strain1,stress1,energy1,strain2,stress2,energy2]


	# 要素を表す文字列を返す
	# materials - 材料
	# params - 梁パラメータ
	# p - 節点
	def toString(self, materials,params,p):
		s=self.getName()+'\t'+self.label.toString(10)+'\t'+ \
					materials[self.material].label.toString(10)+'\t'+ \
					params[self.param].label.toString(10)
		for i in range(len(self.nodes)):
			s+='\t'+p[self.nodes[i]].label.toString(10)

		return s


#--------------------------------------------------------------------#
# Bernoulli-Euler梁要素
# label - 要素ラベル
# material - 材料のインデックス
# param - 梁パラメータのインデックス
# nodes - 節点番号
# axis - 断面基準方向ベクトル
class BEBarElement(BarElement):
	def __init__(self,label,material,param,nodes,axis):
		super().__init__(label,material,param,nodes,axis)


	# 要素境界名称を返す
	def getName(self):
		return 'BEBarElement'

	# 剛性マトリックスの梁曲げ成分を返す
	# l - 要素長さ
	# material - 材料
	# sect - 梁断面パラメータ
	def stiffBend(self, l,material,sect):
		kc=material.ee/(l*l*l)
		kcy=kc*sect.iy
		kcz=kc*sect.iz
		kcy12=12*kcy
		kcy6l=6*kcy*l
		kcyll=kcy*l*l
		kcz12=12*kcz
		kcz6l=6*kcz*l
		kczll=kcz*l*l
		return [[kcy12,kcy6l,-kcy12,kcy6l],[kcy6l,4*kcyll,-kcy6l,2*kcyll],
						[-kcy12,-kcy6l,kcy12,-kcy6l],[kcy6l,2*kcyll,-kcy6l,4*kcyll],
						[kcz12,-kcz6l,-kcz12,-kcz6l],[-kcz6l,4*kczll,kcz6l,2*kczll],
						[-kcz12,kcz6l,kcz12,kcz6l],[-kcz6l,2*kczll,kcz6l,4*kczll]]


	# 曲げ曲率・せん断歪を返す
	# v - 局所座標系の変位
	# l - 要素長さ
	# material - 材料
	# sect - 梁断面パラメータ
	def bendCurveShare(self, v,l,material,sect):
		ckap1=6/(l*l)
		ckap2=4/l
		ckap3=0.5*ckap2
		kpy=[ckap1*(v[1]-v[7])+ckap2*v[5]+ckap3*v[11],
						ckap1*(v[1]-v[7])+ckap3*v[5]+ckap2*v[11]]
		kpz=[-ckap1*(v[2]-v[8])+ckap2*v[4]+ckap3*v[10],
						-ckap1*(v[2]-v[8])+ckap3*v[4]+ckap2*v[10]]
		return [kpy,kpz,0,0]


	# 質量マトリックスを返す
	# p - 要素節点
	# dens - 材料の密度
	# sect - 梁断面パラメータ
	def massMatrix(self, p, dens, sect):
		l=p[0].distanceTo(p[1])
		d=dirMatrix(p,self.axis)
		mi=sect.massInertia(dens,l)
		m0=mi[0]
		dm=C1_3*m0
		dix=C1_3*mi[1]
		m=numeric.rep([12,12],0)
		m[0][0]=dm
		m[0][6]=0.5*dm
		m[6][0]=0.5*dm
		m[6][6]=dm
		m[3][3]=dix
		m[3][9]=0.5*dix
		m[9][3]=0.5*dix
		m[9][9]=dix
		m1=m0*13/35
		m2=m0*11*l/210
		m3=m0*9/70
		m4=m0*13*l/420
		m5=m0*l*l/105
		m6=m0*l*l/140
		mm1=[[m1,m2,m3,-m4],[m2,m5,m4,-m6],
						[m3,m4,m1,-m2],[-m4,-m6,-m2,m5]]
		mm2=[[m1,-m2,m3,m4],[-m2,m5,-m4,-m6],
						[m3,-m4,m1,m2],[m4,-m6,m2,m5]]
		for i in range(4):
			mi1=m[I_YMZ[i]]
			mi2=m[I_ZMY[i]]
			mmi1=mm1[i]
			mmi2=mm2[i]
			for j in range(4):
				mi1[I_YMZ[j]]=mmi1[j]
				mi2[I_ZMY[j]]=mmi2[j]

		toDir3(d,m)
		return m


#--------------------------------------------------------------------#
# Timoshenko梁要素
# label - 要素ラベル
# material - 材料のインデックス
# param - 梁パラメータのインデックス
# nodes - 節点番号
# axis - 断面基準方向ベクトル
class TBarElement(BarElement):
	def __init__(self, label,material,param,nodes,axis):
		super().__init__(label,material,param,nodes,axis)


	# 要素境界名称を返す
	def getName(self):
		return 'TBarElement'


	# 剛性マトリックスの梁曲げ成分を返す
	# l - 要素長さ
	# material - 材料
	# sect - 梁断面パラメータ
	def stiffBend(self, l, material, sect):
		kb=material.ee/l
		kby=kb*sect.iy
		kbz=kb*sect.iz
		ksc1=sect.shearCoef()*material.gg*sect.area/l
		ksc2y=12*kby/l
		kscy=ksc1*ksc2y/(ksc1+ksc2y)	# MacNealの補正
		ksc2z=12*kbz/l
		kscz=ksc1*ksc2z/(ksc1+ksc2z)
		lh=0.5*l
		ksyl=kscy*lh
		ksyl2=ksyl*lh
		kszl=kscz*lh
		kszl2=kszl*lh
		return [[kscy,ksyl,-kscy,ksyl],[ksyl,kby+ksyl2,-ksyl,-kby+ksyl2],
						[-kscy,-ksyl,kscy,-ksyl],[ksyl,-kby+ksyl2,-ksyl,kby+ksyl2],
						[kscz,-kszl,-kscz,-kszl],[-kszl,kbz+kszl2,kszl,-kbz+kszl2],
						[-kscz,kszl,kscz,kszl],[-kszl,-kbz+kszl2,kszl,kbz+kszl2]]

	# 曲げ曲率・せん断歪を返す
	# v - 局所座標系の変位
	# l - 要素長さ
	# material - 材料
	# sect - 梁断面パラメータ
	def bendCurveShare(self, v, l, material, sect):
		cs1=sect.shearCoef()/l
		cs2=0.5*sect.shearCoef()
		ckap1=cs2 * material.gg * sect.area * l / material.ee
		ckap1y=ckap1/sect.iy
		ckap1z=ckap1/sect.iz
		ckap2y=ckap1y*0.5*l
		ckap2z=ckap1z*0.5*l
		ckap3=1/l
		sy=cs1*(v[7]-v[1])-cs2*(v[5]+v[11])
		sz=cs1*(v[8]-v[2])+cs2*(v[4]+v[10])
		kpy=[ckap1y*(v[1]-v[7])+(ckap2y+ckap3)*v[5]+
						(ckap2y-ckap3)*v[11],
						ckap1y*(v[1]-v[7])+(ckap2y-ckap3)*v[5]+
						(ckap2y+ckap3)*v[11]]
		kpz=[-ckap1z*(v[2]-v[8])+(ckap2z+ckap3)*v[4]+
						(ckap2z-ckap3)*v[10],
						-ckap1z*(v[2]-v[8])+(ckap2z-ckap3)*v[4]+
						(ckap2z+ckap3)*v[10]]
		return [kpy,kpz,sy,sz]


	# 質量マトリックスを返す
	# p - 要素節点
	# dens - 材料の密度
	# sect - 梁断面パラメータ
	def massMatrix(self, p, dens, sect):
		l=p[0].distanceTo(p[1])
		d=dirMatrix(p,self.axis)
		mi=sect.massInertia(dens,l)
		dm=C1_3*mi[0]
		di=[C1_3*mi[1],C1_3*mi[2],C1_3*mi[3]]
		m=numeric.diag([dm,dm,dm,di[0],di[1],di[2],
												dm,dm,dm,di[0],di[1],di[2]])
		for i in range(3):
			m[i][i+6]=0.5*dm
			m[i+6][i]=0.5*dm
			m[i+3][i+9]=0.5*di[i]
			m[i+9][i+3]=0.5*di[i]

		toDir3(d,m)
		return m
