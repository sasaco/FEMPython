from Element import FElement, toDir, toDir3, swap, dirMatrix, planeAngle, GTRI2, GX2, C1_3, C1_6, C1_12
from FemDataModel import normalVector, addMatrix
from Result import Strain, Stress
from ElementBorder import TriangleBorder1, EdgeBorder1, QuadangleBorder1
import math
#--------------------------------------------------------------------#

# 三角形1次要素の節点のξ,η座標
TRI1_NODE=[[0,0],[1,0],[0,1]]
# 三角形1次要素の積分点のξ,η座標,重み係数
TRI1_INT=[[C1_3,C1_3,0.5]]
# 三角形2次要素の積分点のξ,η座標,重み係数
TRI2_INT=[[GTRI2[0],GTRI2[0],C1_6],[GTRI2[1],GTRI2[0],C1_6],[GTRI2[0],GTRI2[1],C1_6]]
# 四角形1次要素の節点のξ,η座標
QUAD1_NODE=[[-1,-1],[1,-1],[1,1],[-1,1]]
# 四角形1次要素の積分点のξ,η座標,重み係数
QUAD1_INT=[[GX2[0],GX2[0],1],[GX2[1],GX2[0],1],[GX2[0],GX2[1],1],[GX2[1],GX2[1],1]]
# 三角形1次要素の質量マトリックス係数
TRI1_MASS1=[[1,0.5,0.5],[0.5,1,0.5],[0.5,0.5,1]]

# 節点座標を局所座標系に変換する
# d - 方向余弦マトリックス
# p - 要素節点
def toLocal(d,p):
  x=[]
  for i in range(len(p)):
    x[i]=THREE.Vector3().set(
      d[0][0]*p[i].x+d[1][0]*p[i].y+d[2][0]*p[i].z,
      d[0][1]*p[i].x+d[1][1]*p[i].y+d[2][1]*p[i].z,
      d[0][2]*p[i].x+d[1][2]*p[i].y+d[2][2]*p[i].z)

  return x



#--------------------------------------------------------------------#
# シェル要素
# label - 要素ラベル
# material - 材料のインデックス
# param - シェルパラメータのインデックス
# nodes - 節点番号
# nodeP - 節点のξ,η座標
# intP - 積分点のξ,η座標,重み係数
class ShellElement(FElement):
  def __init__(self, label,material,param,nodes,nodeP,intP):
    super().__init__(label,material,nodes)
    self.param=param
    self.isShell=True
    self.nodeP=nodeP
    self.intP=intP


  # 要素境界数を返す
  def borderCount(self):
    return 2


  # 要素節点の角度を返す
  # p - 要素節点
  def angle(self, p):
    th=[]
    count=self.nodeCount()
    for i in range(count):
      th[i] = planeAngle(p[i], p[(i+1)%count], p[(i+count-1)%count])

    return th


  # ヤコビ行列を返す
  # p - 要素節点
  # sf - 形状関数行列
  # n - 法線ベクトル
  # t - 要素厚さ
  def jacobianMatrix(self, p, sf, n, t):
    count=self.nodeCount()
    jac=[0,0,0,0,0,0,0,0,0]
    for i in range(count):
      sfi=sf[i]
      ppi=p[i]
      pix=ppi.x
      piy=ppi.y
      piz=ppi.z
      for j in range(2):
        sfij=sfi[j+1]
        jac[j]+=sfij*pix
        jac[j+3]+=sfij*piy
        jac[j+6]+=sfij*piz

    jac[2]=0.5*t*n.x
    jac[5]=0.5*t*n.y
    jac[8]=0.5*t*n.z
    return THREE.Matrix3().fromArray(jac)


  # 逆ヤコビ行列を返す
  # ja - ヤコビ行列
  # d - 方向余弦マトリックス
  def jacobInv(self, ja,d):
    e1=ja.elements
    jd= THREE.Matrix3().set(e1[0]*d[0][0]+e1[3]*d[1][0]+e1[6]*d[2][0],
      e1[0]*d[0][1]+e1[3]*d[1][1]+e1[6]*d[2][1],
      e1[0]*d[0][2]+e1[3]*d[1][2]+e1[6]*d[2][2],
      e1[1]*d[0][0]+e1[4]*d[1][0]+e1[7]*d[2][0],
      e1[1]*d[0][1]+e1[4]*d[1][1]+e1[7]*d[2][1],
      e1[1]*d[0][2]+e1[4]*d[1][2]+e1[7]*d[2][2],
      0,0,e1[2]*d[0][2]+e1[5]*d[1][2]+e1[8]*d[2][2])
    return THREE.Matrix3().getInverse(jd,True)


  # 形状関数の勾配 [ dNi/dx dNi/dy ] を返す
  # p - 要素節点
  # ja - ヤコビ行列
  # sf - 形状関数行列
  # d - 方向余弦マトリックス
  # t - 要素厚さ
  def grad(self, p, ja, sf, d, t):
    count=self.nodeCount()
    gr=[]
    ji=self.jacobInv(ja,d).elements
    for i in range(count):
      sfi=sf[i]
      dndxsi=sfi[1]
      dndeta=sfi[2]
      gr[i]=[
        ji[0]*dndxsi+ji[3]*dndeta,
        ji[1]*dndxsi+ji[4]*dndeta,
        ji[2]*dndxsi+ji[5]*dndeta
      ]

    return gr


  # 歪 - 変位マトリックスの転置行列を返す
  # ja - ヤコビ行列
  # sf - 形状関数行列
  # d - 方向余弦マトリックス
  def strainMatrix1(self, ja, sf, d):
    count=self.nodeCount()
    m=numeric.rep([count,4],0)
    ji=self.jacobInv(ja,d).elements
    for i in range(count):
      mi=m[i]
      sfi=sf[i]
      for j in range(3):
        mi[j] = ji[j] * sfi[1] + ji[j+3] * sfi[2]

      mi[3]=ji[8]*sfi[0]

    return m


  # 歪 - 変位マトリックスの転置行列を返す
  # ただし歪は要素面座標、変位は全体座標
  # ja - ヤコビ行列
  # sf - 形状関数行列
  # d - 方向余弦マトリックス
  # zeta - 節点のζ座標
  # t - 要素厚さ
  def strainMatrix(self, ja, sf, d, zeta, t):
    b=self.strainMatrix1(ja,sf,d)
    z=0.5*t*zeta
    count=self.nodeCount()
    m1=numeric.rep([5,6],0)
    matrix=numeric.rep([6*count,5],0)
    for i in range(count):
      bi=b[i]
      m1[0][0]=bi[0]
      m1[0][4]=z*bi[0]
      m1[1][1]=bi[1]
      m1[1][3]=-z*bi[1]
      m1[2][0]=bi[1]
      m1[2][1]=bi[0]
      m1[2][3]=-z*bi[0]
      m1[2][4]=z*bi[1]
      m1[3][1]=bi[2]
      m1[3][2]=bi[1]
      m1[3][3]=-0.5*t*bi[3]-z*bi[2]
      m1[4][0]=bi[2]
      m1[4][2]=bi[0]
      m1[4][4]=0.5*t*bi[3]+z*bi[2]
      ib=6*i
      for i1 in range(5):
        m1i=m1[i1]
        for j1 in range(3):
          dj=d[j1]
          s1=0
          s2=0
          for k1 in range(3):
            s1+=m1i[k1]*dj[k1]
            s2+=m1i[k1+3]*dj[k1]

          matrix[ib+j1][i1]+=s1
          matrix[ib+3+j1][i1]+=s2

    return matrix


  # 積分点の形状関数マトリックス [ NiNj ] を返す
  # p - 要素節点
  # x - ξ,η,ζ座標
  # w - 重み係数
  # t - 要素厚さ
  def shapePart(self, p,x,w,t):
    sf=self.shapeFunction(x[0], x[1])
    ja=self.jacobianMatrix(p, sf, normalVector(p), t)
    count=self.nodeCount()
    matrix=[]
    coef = 2 * w * abs(ja.determinant())
    for i in range(count):
      matr=[]
      cf2 = coef * sf[i][0]
      for j in range(count):
        matr[j]=cf2*sf[j][0]

      matrix[i]=matr

    return matrix


  # 積分点の拡散マトリックス [ ∇Ni・∇Nj ] を返す
  # p - 要素節点
  # x - ξ,η,ζ座標
  # w - 重み係数
  # t - 要素厚さ
  def gradPart(self, p, x, w, t):
    sf=self.shapeFunction(x[0],x[1])
    ja=self.jacobianMatrix(p,sf,normalVector(p),t)
    gr=self.grad(p,ja,sf,dirMatrix(p),t)
    count=self.nodeCount()
    matrix=[]
    coef=2*w*abs(ja.determinant())
    for i in range(count):
      matr=[]
      gri=gr[i]
      c1=coef*gri[0]
      c2=coef*gri[1]
      c3=coef*gri[2]
      for j in range(count):
        grj=gr[j]
        matr[j] = c1 * grj[0] + c2 * grj[1] + c3*grj[2]

      matrix[i]=matr

    return matrix


  # 形状関数マトリックス [ ∫NiNjdV ] を返す
  # p - 要素節点
  # coef - 係数
  # sp - シェルパラメータ
  def shapeFunctionMatrix(self, p,coef,sp):
    count=self.nodeCount()
    s=numeric.rep([count,count],0)
    t=sp.thickness
    for i in range(len(self.intP)):
      addMatrix(s,self.shapePart(p,self.intP[i],coef*self.intP[i][2],t))

    return s


  # 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
  # p - 要素節点
  # coef - 係数
  # sp - シェルパラメータ
  def gradMatrix(self, p,coef,sp):
    count=self.nodeCount()
    g=numeric.rep([count,count],0)
    t=sp.thickness
    for i in range(len(self.intP)):
      addMatrix(g,self.gradPart(p,self.intP[i],coef*self.intP[i][2],t))

    return g


  # 幾何剛性マトリックスを返す
  # p - 要素節点
  # u - 節点変位
  # d1 - 応力 - 歪マトリックス
  # sp - シェルパラメータ
  def geomStiffnessMatrix(self, p,u,d1,sp):
    count=self.nodeCount()
    kk=numeric.rep([6*count,6*count],0)
    d=dirMatrix(p)
    n=normalVector(p)
    v=self.toArray(u,6)
    t=sp.thickness
    for i in range(len(self.intP)):
      ip=self.intP[i]
      sf=self.shapeFunction(ip[0],ip[1])
      ja=self.jacobianMatrix(p,sf,n,t)
      gr=self.grad(p,ja,sf,d,t)
      sm=self.strainMatrix(ja,sf,d,0,t)
      str=self.toStress(numeric.dotMV(d1,numeric.dotVM(v,sm)))
      w=2*ip[2]*abs(ja.determinant())
      for i1 in range(count):
        i6=6*i1
        gri=gr[i1]
        for j1 in range(count):
          j6=6*j1
          grj=gr[j1]
          s = w * (
            gri[0]*(str.xx*grj[0]+str.xy*grj[1]+str.zx*grj[2])+
            gri[1]*(str.xy*grj[0]+str.yy*grj[1]+str.yz*grj[2])+
            gri[2]*(str.zx*grj[0]+str.yz*grj[1]+str.zz*grj[2]))
          kk[i6][j6]+=s
          kk[i6+1][j6+1]+=s
          kk[i6+2][j6+2]+=s

    toDir3(d,kk)
    return kk


  # 節点歪・応力を返す
  # p - 要素節点
  # u - 節点変位
  # d1 - 応力 - 歪マトリックス
  # sp - シェルパラメータ
  def strainStress(self, p, u, d1, sp):
    count=self.nodeCount()
    d=dirMatrix(p)
    n=normalVector(p)
    v=self.toArray(u,6)
    t=sp.thickness
    strain1=[]
    stress1=[]
    energy1=[]
    strain2=[]
    stress2=[]
    energy2=[]
    for i in range(count):
      np=self.nodeP[i]
      eps1=self.strainPart(p,v,n,d,np[0],np[1],1,t)
      eps2=self.strainPart(p,v,n,d,np[0],np[1],-1,t)
      strain1[i]=self.toStrain(eps1)
      stress1[i]=self.toStress(numeric.dotMV(d1,eps1))
      strain2[i]=self.toStrain(eps2)
      stress2[i]=self.toStress(numeric.dotMV(d1,eps2))
      strain1[i].rotate(d)
      stress1[i].rotate(d)
      strain2[i].rotate(d)
      stress2[i].rotate(d)
      energy1[i]=0.5*strain1[i].innerProduct(stress1[i])
      energy2[i]=0.5*strain2[i].innerProduct(stress2[i])

    return [strain1,stress1,energy1,strain2,stress2,energy2]


  # 要素内の歪ベクトルを返す
  # p - 要素節点
  # v - 節点変位ベクトル
  # n - 法線ベクトル
  # d - 方向余弦マトリックス
  # xsi,eta,zeta - ξ,η,ζ座標
  # t - 要素厚さ
  def strainPart(self, p,v,n,d,xsi,eta,zeta,t):
    sf=self.shapeFunction(xsi,eta)
    ja=self.jacobianMatrix(p,sf,n,t)
    sm=self.strainMatrix(ja,sf,d,zeta,t)
    return numeric.dotVM(v,sm)


  # 要素歪・応力を返す
  # p - 要素節点
  # u - 節点変位
  # d1 - 応力 - 歪マトリックス
  # sp - シェルパラメータ
  def elementStrainStress(self, p,u,d1,sp):
    d=dirMatrix(p)
    n=normalVector(p)
    v=self.toArray(u,6)
    t=sp.thickness
    cf=1/len(self.intP)
    strain1=[0,0,0,0,0,0]
    stress1=[0,0,0,0,0,0]
    energy1=0
    strain2=[0,0,0,0,0,0]
    stress2=[0,0,0,0,0,0]
    energy2=0
    for i in range(len(self.intP)):
      ip=self.intP[i]
      eps1=self.strainPart(p,v,n,d,ip[0],ip[1],1,t)
      eps2=self.strainPart(p,v,n,d,ip[0],ip[1],-1,t)
      strain1=numeric.add(strain1,eps1)
      strain2=numeric.add(strain2,eps2)
      str1=numeric.dotMV(d1,eps1)
      str2=numeric.dotMV(d1,eps2)
      stress1=numeric.add(stress1,str1)
      stress2=numeric.add(stress2,str2)
      energy1+=numeric.dotVV(eps1,str1)
      energy2+=numeric.dotVV(eps2,str2)

    strain1=numeric.mul(strain1,cf)
    stress1=numeric.mul(stress1,cf)
    energy1*=0.5*cf
    strain2=numeric.mul(strain1,cf)
    stress2=numeric.mul(stress1,cf)
    energy2*=0.5*cf
    return [self.toStrain(strain1),self.toStress(stress1),energy1,
            self.toStrain(strain2),self.toStress(stress2),energy2]


  # ベクトルを歪に変換する
  # s - 歪ベクトル
  def toStrain(self, s):
    return Strain([s[0],s[1],0,s[2],s[3],s[4]])


  # ベクトルを歪に変換する
  # s - 歪ベクトル
  def toStress(s):
    return Stress([s[0],s[1],0,s[2],s[3],s[4]])


  # 要素を表す文字列を返す
  # materials - 材料
  # params - シェルパラメータ
  # p - 節点
  def toString(self, materials, params, p):
    s=self.getName()+'\t'+self.label.toString(10)+'\t'+ \
          materials[self.material].label.toString(10)+'\t'+ \
          params[self.param].label.toString(10)
    for i in range(len(self.nodes)):
      s+='\t'+p[self.nodes[i]].label.toString(10)

    return s


#--------------------------------------------------------------------#
# 三角形1次要素 (薄肉シェル)
# label - 要素ラベル
# material - 材料のインデックス
# param - シェルパラメータのインデックス
# nodes - 節点番号
class TriElement1(ShellElement):
  def __init__(self,label,material,param,nodes):
    super().__init__(label,material,param,nodes, TRI1_NODE, TRI1_INT)


  # 要素名称を返す
  def getName(self):
    return 'TriElement1'

  # 節点数を返す
  def nodeCount(self):
    return 3

  # 要素境界を返す
  # element - 要素ラベル
  # index - 要素境界のインデックス
  def border(self, element, index):
    p=self.nodes
    if(index==0):
      return TriangleBorder1(element,[p[0],p[1],p[2]])
    elif(index==1):
      return TriangleBorder1(element,[p[0],p[2],p[1]])

    return None


  # 要素境界辺を返す
  # element - 要素ラベル
  # index - 要素境界辺のインデックス
  def borderEdge(self, element, index):
    p=self.nodes
    if(index ==0):
      return EdgeBorder1(element,[p[0],p[1]])
    elif(index==1):
      return EdgeBorder1(element,[p[1],p[2]])
    elif(index==2):
      return EdgeBorder1(element,[p[2],p[0]])

    return None


  # 要素を鏡像反転する
  def mirror(self):
    swap(self.nodes,1,2)


  # １次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
  # xsi,eta - 要素内部ξ,η座標
  def shapeFunction(self, xsi,eta):
    return [[1-xsi-eta,-1,-1],[xsi,1,0],[eta,0,1]]


  # ２次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
  # xsi,eta - 要素内部ξ,η座標
  def shapeFunction2(self,xsi,eta):
    xe=1-xsi-eta
    return [[xe*(2*xe-1),1-4*xe,1-4*xe],[xsi*(2*xsi-1),4*xsi-1,0],
            [eta*(2*eta-1),0,4*eta-1],[4*xe*xsi,4*(xe-xsi),-4*xsi],
            [4*xsi*eta,4*eta,4*xsi],[4*xe*eta,-4*eta,4*(xe-eta)]]


  # 角度の形状関数行列 [ Hxi Hyi dHxi/dξ dHyi/dξ dHxi/dη dHyi/dη ] を返す
  # p - 要素節点
  # d - 方向余弦マトリックス
  # xsi,eta - 要素内部ξ,η座標
  def shapeFunction3(self, p,d,xsi,eta):
    count=self.nodeCount()
    m=numeric.rep([3*count,6],0)
    sf2=self.shapeFunction2(xsi,eta)
    d12=p[1].clone().sub(p[0])
    d23=p[2].clone().sub(p[1])
    d31=p[0].clone().sub(p[2])
    l=[1/d12.lengthSq(),1/d23.lengthSq(),1/d31.lengthSq()]
    x=[d[0][0]*d12.x+d[1][0]*d12.y+d[2][0]*d12.z,
          d[0][0]*d23.x+d[1][0]*d23.y+d[2][0]*d23.z,
          d[0][0]*d31.x+d[1][0]*d31.y+d[2][0]*d31.z]
    y=[d[0][1]*d12.x+d[1][1]*d12.y+d[2][1]*d12.z,
          d[0][1]*d23.x+d[1][1]*d23.y+d[2][1]*d23.z,
          d[0][1]*d31.x+d[1][1]*d31.y+d[2][1]*d31.z]
    a=[1.5*l[0]*y[0],1.5*l[1]*y[1],1.5*l[2]*y[2]]
    b=[-1.5*l[0]*x[0],-1.5*l[1]*x[1],-1.5*l[2]*x[2]]
    c=[0.75*l[0]*y[0]*y[0]-0.5,0.75*l[1]*y[1]*y[1]-0.5,
          0.75*l[2]*y[2]*y[2]-0.5]
    d1=[0.75*l[0]*x[0]*y[0],0.75*l[1]*x[1]*y[1],0.75*l[2]*x[2]*y[2]]
    e=[0.25-0.75*l[0]*y[0]*y[0],0.25-0.75*l[1]*y[1]*y[1],
          0.25-0.75*l[2]*y[2]*y[2]]
    for i in range(3):
      i1=(i+2)%3
      i3=3*i
      for j in range(3):
        j2=2*j
        m[i3][j2]=a[i1]*sf2[3+i1][j]-a[i]*sf2[3+i][j]
        m[i3][j2+1]=b[i1]*sf2[3+i1][j]-b[i]*sf2[3+i][j]
        m[i3+1][j2]=sf2[i][j]-c[i1]*sf2[3+i1][j]-c[i]*sf2[3+i][j]
        dn=d1[i1]*sf2[3+i1][j]+d1[i]*sf2[3+i][j]
        m[i3+1][j2+1]=dn
        m[i3+2][j2]=dn
        m[i3+2][j2+1]=sf2[i][j]-e[i1]*sf2[3+i1][j]-e[i]*sf2[3+i][j]

    return m


  # ヤコビアンを返す
  # p - 要素節点
  def jacobian(self, p):
    p0x=p[0].x
    p0y=p[0].y
    p0z=p[0].z
    j1=(p[1].y-p0y)*(p[2].z-p0z)-(p[1].z-p0z)*(p[2].y-p0y)
    j2=(p[1].z-p0z)*(p[2].x-p0x)-(p[1].x-p0x)*(p[2].z-p0z)
    j3=(p[1].x-p0x)*(p[2].y-p0y)-(p[1].y-p0y)*(p[2].x-p0x)
    return math.sqrt(j1*j1+j2*j2+j3*j3)


  # 質量マトリックスを返す
  # p - 要素節点
  # dens - 材料の密度
  # t - 要素厚さ
  def massMatrix(self, p, dens, t):
    count=self.nodeCount()
    m=numeric.rep([6*count,6*count],0)
    mb=numeric.rep([3*count,3*count],0)
    d=dirMatrix(p)
    djt=dens*t*self.jacobian(p)
    tt=C1_12*t*t
    dm=C1_12*djt

    for k in range(3):
      ipi=TRI2_INT[k]
      sf3=self.shapeFunction3(p,d,ipi[0],ipi[1])
      sf=self.shapeFunction(ipi[0],ipi[1])
      hz=[sf[0][0],0,0,sf[1][0],0,0,sf[2][0],0,0],cfm=djt*ipi[2]
      for i in range(3*count):
        for j in range(3*count):
          hxhy = sf3[i][0] * sf3[j][0] + sf3[i][1] * sf3[j][1]
          mb[i][j] += cfm * ( tt * hxhy + hz[i] * hz[j] )

    for i in range(count):
      i3=3*i
      i6=6*i
      for j in range(count):
        j3=3*j
        j6=6*j
        cf1=TRI1_MASS1[i][j]
        dme=cf1*dm
        m[i6][j6]=dme
        m[i6+1][j6+1]=dme
        for i1 in range(3):
          for j1 in range(3):
            m[i6+2+i1][j6+2+j1]=mb[i3+i1][j3+j1]

        m[i6+5][j6+5]=1e-5*dm*tt

    toDir3(d,m)
    return m


  # 剛性マトリックスを返す
  # p - 要素節点
  # d1 - 応力 - 歪マトリックス
  # sp - シェルパラメータ
  def stiffnessMatrix(self, p, d1, sp):
    d=dirMatrix(p)
    n=normalVector(p)
    t=sp.thickness

    sf1=self.shapeFunction(C1_3,C1_3)
    ja1=self.jacobianMatrix(p,sf1,n,t)
    jac1=ja1.determinant()
    jinv=self.jacobInv(ja1,d)
    b1=self.strainMatrix1(sf1,jinv)
    k1=self.stiffPart(d1,b1,abs(jac1))

    count=self.nodeCount()
    k2=numeric.rep([3*count,3*count],0)
    coef=t*t*abs(jac1)/36
    for i in range(3):
      ipi=TRI2_INT[i]
      sf3=self.shapeFunction3(p,d,ipi[0],ipi[1])
      b2=self.strainMatrix2(sf3,jinv)
      addMatrix(k2,self.stiffPart(d1,b2,coef))

    ce1 = 1e-3*t*t*abs(jac1)*d1[2][2]
    ce2 =- ce1/2
    kk=numeric.rep([6*count,6*count],0)
    ks=numeric.rep([6,6],0)
    dir=numeric.rep([6,6],0)
    for i in range(3):
      for j in range(3):
        dir[i][j]=d[i][j]
        dir[i+3][j+3]=d[i][j]

    for i in range(3):
      i2=2*i
      i3=3*i
      i6=6*i
      for j in range(count):
        j2=2*j
        j3=3*j
        j6=6*j
        for ii in range(6):
          for jj in range(6):
            ks[ii][jj]=0

        ks[0][0]=k1[i2][j2]
        ks[0][1]=k1[i2][j2+1]
        ks[1][0]=k1[i2+1][j2]
        ks[1][1]=k1[i2+1][j2+1]
        for ii in range(3):
          for jj in range(3):
            ks[2+ii][2+jj]=k2[i3+ii][j3+jj]

        if(i==j):
          ks[5][5]=ce1
        else:
          ks[5][5]=ce2
        toDir(dir,ks)
        for ii in range(6):
          for jj in range(6):
            kk[i6+ii][j6+jj]=ks[ii][jj]

    return kk


  # 歪 - 変位マトリックスの転置行列を返す
  # sf - 形状関数行列
  # jinv - 逆ヤコビ行列
  def strainMatrix1(self, sf, jinv):
    count=self.nodeCount()
    b=numeric.rep([2*count,3],0)
    ji=jinv.elements
    for i in range(count):
      sfi=sf[i]
      dndx=ji[0]*sfi[1]+ji[3]*sfi[2]
      dndy=ji[1]*sfi[1]+ji[4]*sfi[2]
      i2=2*i
      b[i2][0]=dndx
      b[i2+1][1]=dndy
      b[i2][2]=dndy
      b[i2+1][2]=dndx

    return b


  # 面外歪 - 変位マトリックスの転置行列を返す
  # sf - 形状関数行列
  # jinv - 逆ヤコビ行列
  def strainMatrix2(self, sf,jinv):
    count=3*self.nodeCount()
    b=[]
    ji=jinv.elements
    for i in range(count):
      sfi=sf[i]
      hxx=ji[0]*sfi[2]+ji[3]*sfi[4]
      hxy=ji[1]*sfi[2]+ji[4]*sfi[4]
      hyx=ji[0]*sfi[3]+ji[3]*sfi[5]
      hyy=ji[1]*sfi[3]+ji[4]*sfi[5]
      b[i]=[hyx,-hxy,hyy-hxx]

    return b


  # 形状関数マトリックス [ ∫NiNjdV ] を返す
  # p - 要素節点
  # coef - 係数
  # t - 要素厚さ
  def shapeFunctionMatrix(self, p, coef, t):
    ds=coef*self.jacobian(p)/12
    count=3*self.nodeCount()
    s=numeric.rep([count],0.5*ds)
    for i in range(count):
      s[i][i]=ds
    return s


  # 幾何剛性マトリックスを返す
  # p - 要素節点
  # u - 節点変位
  # d1 - 応力 - 歪マトリックス
  # sp - シェルパラメータ
  def geomStiffnessMatrix(self, p, u, d1, sp):
    count=self.nodeCount()
    kk=numeric.rep([6*count,6*count],0)
    d=dirMatrix(p)
    n=normalVector(p)
    v=self.toArray(u,6)
    t=sp.thickness
    ip=self.intP[0]
    sf1=self.shapeFunction(ip[0],ip[1])
    ja=self.jacobianMatrix(p,sf1,n,t)
    gr=self.grad(p,ja,sf1,d,t)
    jinv=self.jacobInv(ja,d)
    sf3=self.shapeFunction3(p,d,ip[0],ip[1])
    sm=self.strainMatrix(sf1,sf3,jinv,d,0,t)
    str=self.toStress(numeric.dotMV(d1,numeric.dotVM(v,sm)))
    w=abs(ja.determinant())
    for i1 in range(count):
      i6=6*i1,gri=gr[i1]
      for j1 in range(count):
        j6=6*j1,grj=gr[j1]
        s=w*(gri[0]*(str.xx*grj[0]+str.xy*grj[1]+str.zx*grj[2])+
                gri[1]*(str.xy*grj[0]+str.yy*grj[1]+str.yz*grj[2])+
                gri[2]*(str.zx*grj[0]+str.yz*grj[1]+str.zz*grj[2]))
        kk[i6][j6]=s
        kk[i6+1][j6+1]=s
        kk[i6+2][j6+2]=s

    toDir3(d,kk)
    return kk


  # 要素内の歪ベクトルを返す
  # p - 要素節点
  # v - 節点変位ベクトル
  # n - 法線ベクトル
  # d - 方向余弦マトリックス
  # xsi,eta,zeta - ξ,η,ζ座標
  # t - 要素厚さ
  def strainPart(self, p,v,n,d,xsi,eta,zeta,t):
    sf1=self.shapeFunction(xsi,eta)
    ja=self.jacobianMatrix(p,sf1,n,t)
    jinv=self.jacobInv(ja,d)
    sf3=self.shapeFunction3(p,d,xsi,eta)
    sm=self.strainMatrix(sf1,sf3,jinv,d,zeta,t)
    return numeric.dotVM(v,sm)


  # 歪 - 変位マトリックスの転置行列を返す
  # ただし歪は要素面座標、変位は全体座標
  # sf1 - 面内変形の形状関数行列
  # sf3 - 面外変形の形状関数行列
  # jinv - 逆ヤコビ行列
  # d - 方向余弦マトリックス
  # zeta - 節点のζ座標
  # t - 要素厚さ
  def strainMatrix(self, sf1,sf3,jinv,d,zeta,t):
    b1=self.strainMatrix1(sf1,jinv)
    b2=self.strainMatrix2(sf3,jinv)
    count=self.nodeCount()
    m1=numeric.rep([3,6],0)
    matrix=numeric.rep([6*count,3],0)
    z=0.5*t*zeta
    
    for i in range(count):
      i2=2*i
      i3=3*i
      i6=6*i
      for i1 in range(3):
        m1[i1][0]=b1[i2][i1]
        m1[i1][1]=b1[i2+1][i1]
        m1[i1][2]=z*b2[i3][i1]
        m1[i1][3]=z*b2[i3+1][i1]
        m1[i1][4]=z*b2[i3+2][i1]
        m1[i1][5]=0

      for i1 in range(3):
        m1i=m1[i1]
        for j1 in range(3):
          dj=d[j1]
          s1=0
          s2=0
          for k1 in range(3):
            s1+=m1i[k1]*dj[k1]
            s2+=m1i[k1+3]*dj[k1]

          matrix[i6+j1][i1]+=s1
          matrix[i6+3+j1][i1]+=s2

    return matrix


  # ベクトルを歪に変換する
  # s - 歪ベクトル
  def toStrain(self, s):
    return Strain([s[0],s[1],0,s[2],0,0])

  # ベクトルを歪に変換する
  # s - 歪ベクトル
  def toStress(self, s):
    return Stress([s[0],s[1],0,s[2],0,0])

#--------------------------------------------------------------------#
# 四角形1次要素 (MITC4)
# label - 要素ラベル
# material - 材料のインデックス
# param - シェルパラメータのインデックス
# nodes - 節点番号
class QuadElement1(ShellElement):
  def __init__(self,label,material,param,nodes):
    super().__init__(label,material,param,nodes, QUAD1_NODE, QUAD1_INT)

  # 要素名称を返す
  def getName(self):
    return 'QuadElement1'

  # 節点数を返す
  def nodeCount(self):
    return 4


  # 要素境界を返す
  # element - 要素ラベル
  # index - 要素境界のインデックス
  def border(self, element, index):
    p=self.nodes
    if(index==0):
      return QuadangleBorder1(element,[p[0],p[1],p[2],p[3]])
    elif(index==1):
      return QuadangleBorder1(element,[p[0],p[3],p[2],p[1]])
    return None


  # 要素境界辺を返す
  # element - 要素ラベル
  # index - 要素境界辺のインデックス
  def borderEdge(self, element, index):
    p=self.nodes
    if(index==0):
        return EdgeBorder1(element,[p[0],p[1]])
    elif(index==1):
        return EdgeBorder1(element,[p[1],p[2]])
    elif(index==2):
        return EdgeBorder1(element,[p[2],p[3]])
    elif(index==3):
        return EdgeBorder1(element,[p[3],p[0]])

    return None


  # 要素を鏡像反転する
  def mirror(self):
    swap(self.nodes,1,3)


  # 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
  # xsi,eta - 要素内部ξ,η座標
  def shapeFunction(self, xsi, eta):
    return [[0.25*(1-xsi)*(1-eta),-0.25*(1-eta),-0.25*(1-xsi)],
            [0.25*(1+xsi)*(1-eta),0.25*(1-eta),-0.25*(1+xsi)],
            [0.25*(1+xsi)*(1+eta),0.25*(1+eta),0.25*(1+xsi)],
            [0.25*(1-xsi)*(1+eta),-0.25*(1+eta),0.25*(1-xsi)]]


  # 質量マトリックスを返す
  # p - 要素節点
  # dens - 材料の密度
  # t - 要素厚さ
  def massMatrix(self, p, dens, t):
    count=self.nodeCount()
    m=numeric.rep([6*count,6*count],0)
    d=dirMatrix(p)
    n=normalVector(p)
    tt=C1_12*t*t
    for i in range(len(self.intP)):
      ipi=self.intP[i]
      sf=self.shapeFunction(ipi[0],ipi[1])
      nn=[sf[0][0],sf[1][0],sf[2][0],sf[3][0]]
      jac=abs(self.jacobianMatrix(p,sf,n,t).determinant())
      jac *= 2*ipi[2]
      for i1 in range(count):
        i6=6*i1
        nja=nn[i1]*jac
        for j1 in range(count):
          j6=6*j1
          nnja=nja*nn[j1]
          dm=dens*nnja
          dii=tt*dm
          m[i6][j6]+=dm
          m[i6+1][j6+1]+=dm
          m[i6+2][j6+2]+=dm
          m[i6+3][j6+3]+=dii
          m[i6+4][j6+4]+=dii
          m[i6+5][j6+5]+=1e-4*dii

    toDir3(d,m)
    return m


  # 剛性マトリックスを返す
  # p - 要素節点
  # d1 - 応力 - 歪マトリックス
  # sp - シェルパラメータ
  def stiffnessMatrix(self, p,d1,sp):
    size=6*self.nodeCount()
    kk=numeric.rep([size,size],0)
    n=normalVector(p)
    t=sp.thickness
    for i in range(len(self.intP)):
      ks=self.stiffPart(p,d1,n,self.intP[i][0],self.intP[i][1],t)
      addMatrix(kk,ks)

    return kk


  # 積分点の剛性マトリックスを返す
  # p - 要素節点
  # d1 - 応力 - 歪マトリックス
  # n - 法線ベクトル
  # xsi,eta - 要素内部ξ,η座標
  # t - 要素厚さ
  def stiffPart(self, p,d1,n,xsi,eta,t):
    d=dirMatrix(p)
    sf=self.shapeFunction(xsi,eta)
    ja=self.jacobianMatrix(p,sf,n,t)
    bc0=self.strainMatrix1(ja,sf,d)
    sf1=self.shapeFunction(xsi,0)
    ja1=self.jacobianMatrix(p,sf1,n,t)
    sf2=self.shapeFunction(0,eta)
    ja2=self.jacobianMatrix(p,sf2,n,t)
    bc=[self.strainMatrix1(ja1,sf1,d),self.strainMatrix1(ja2,sf2,d)]
    count=self.nodeCount()
    kk=numeric.rep([6*count,6*count],0)
    jacob=abs(ja.determinant())

    tt6=t*t/6.0
    ce1=1e-3*t*t*d1[3][3]
    ce2=-ce1/(count-1)
    k1=[[0,0,0],[0,0,0],[0,0,0]]
    k2=[[0,0,0],[0,0,0],[0,0,0]]
    k3=[[0,0,0],[0,0,0],[0,0,0]]
    k4=[[0,0,0],[0,0,0],[0,0,0]]
    for i in range(count):
      for j in range(count):
        for j1 in range(3):
          for j2 in range(3):
            k1[j1][j2]=0
            k2[j1][j2]=0
            k3[j1][j2]=0
            k4[j1][j2]=0

        for j1 in range(2):
          for j2 in range(2):
            k1[j1][j2] = bc0[i][j1] * d1[j1][j2] * bc0[j][j2] + \
              bc0[i][1-j1] * d1[2][2] * bc0[j][1-j2]

          dd=d1[4-j1][4-j1]
          k1[j1][j1]+=bc[1-j1][i][2]*dd*bc[1-j1][j][2]
          k1[j1][2]=bc[1-j1][i][2]*dd*bc[j1][j][j1]
          k1[2][j1]=bc[j1][i][j1]*dd*bc[1-j1][j][2]
          k2[j1][j1]=bc[1-j1][i][2]*dd*bc[1-j1][j][3]
          k2[2][j1]=bc[1-j1][i][j1]*dd*bc[1-j1][j][3]
          k3[j1][j1]=bc[1-j1][i][3]*dd*bc[1-j1][j][2]
          k3[j1][2]=bc[1-j1][i][3]*dd*bc[1-j1][j][j1]

        k1[2][2]=bc[0][i][1]*d1[3][3]*bc[0][j][1]+ \
                bc[1][i][0]*d1[4][4]*bc[1][j][0]
        k4[0][0]=k1[1][1]+3*bc[0][i][3]*d1[3][3]*bc[0][j][3]
        k4[0][1]=-k1[1][0]
        k4[1][0]=-k1[0][1]
        k4[1][1]=k1[0][0]+3*bc[1][i][3]*d1[4][4]*bc[1][j][3]

        for j1 in range(3):
          kt=k2[j1][0]
          k2[j1][0]=-k2[j1][1]
          k2[j1][1]=kt
          kt=k3[0][j1]
          k3[0][j1]=-k3[1][j1]
          k3[1][j1]=kt

        if(i==j):
          k4[2][2]=ce1
        else:
          k4[2][2]=ce2
          
        toDir(d,k1)
        toDir(d,k2)
        toDir(d,k3)
        toDir(d,k4)
        i0=6*i
        j0=6*j
        for j1 in range(3):
          for j2 in range(3):
            kk[i0+j1][j0+j2]=2*jacob*k1[j1][j2]
            kk[i0+j1][j0+3+j2]=t*jacob*k2[j1][j2]
            kk[i0+3+j1][j0+j2]=t*jacob*k3[j1][j2]
            kk[i0+3+j1][j0+3+j2]=tt6*jacob*k4[j1][j2]

    return kk
