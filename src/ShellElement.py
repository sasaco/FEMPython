// 三角形1次要素の節点のξ,η座標
var TRI1_NODE=[[0,0],[1,0],[0,1]];
// 三角形1次要素の積分点のξ,η座標,重み係数
var TRI1_INT=[[C1_3,C1_3,0.5]];
// 三角形2次要素の積分点のξ,η座標,重み係数
var TRI2_INT=[[GTRI2[0],GTRI2[0],C1_6],[GTRI2[1],GTRI2[0],C1_6],
      	      [GTRI2[0],GTRI2[1],C1_6]];
// 四角形1次要素の節点のξ,η座標
var QUAD1_NODE=[[-1,-1],[1,-1],[1,1],[-1,1]];
// 四角形1次要素の積分点のξ,η座標,重み係数
var QUAD1_INT=[[GX2[0],GX2[0],1],[GX2[1],GX2[0],1],[GX2[0],GX2[1],1],
      	       [GX2[1],GX2[1],1]];
// 三角形1次要素の質量マトリックス係数
var TRI1_MASS1=[[1,0.5,0.5],[0.5,1,0.5],[0.5,0.5,1]];

//--------------------------------------------------------------------//
// シェル要素
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
// nodeP - 節点のξ,η座標
// intP - 積分点のξ,η座標,重み係数
var ShellElement=function(label,material,param,nodes,nodeP,intP){
  FElement.call(this,label,material,nodes);
  this.param=param;
  this.isShell=true;
  this.nodeP=nodeP;
  this.intP=intP;
};

// 要素境界数を返す
ShellElement.prototype.borderCount=function(){
  return 2;
};

// 要素節点の角度を返す
// p - 要素節点
ShellElement.prototype.angle=function(p){
  var th=[],count=this.nodeCount();
  for(var i=0;i<count;i++){
    th[i]=planeAngle(p[i],p[(i+1)%count],p[(i+count-1)%count]);
  }
  return th;
};

// ヤコビ行列を返す
// p - 要素節点
// sf - 形状関数行列
// n - 法線ベクトル
// t - 要素厚さ
ShellElement.prototype.jacobianMatrix=function(p,sf,n,t){
  var count=this.nodeCount(),jac=[0,0,0,0,0,0,0,0,0];
  for(var i=0;i<count;i++){
    var sfi=sf[i],ppi=p[i],pix=ppi.x,piy=ppi.y,piz=ppi.z;
    for(var j=0;j<2;j++){
      var sfij=sfi[j+1];
      jac[j]+=sfij*pix;
      jac[j+3]+=sfij*piy;
      jac[j+6]+=sfij*piz;
    }
  }
  jac[2]=0.5*t*n.x;
  jac[5]=0.5*t*n.y;
  jac[8]=0.5*t*n.z;
  return new THREE.Matrix3().fromArray(jac);
};

// 逆ヤコビ行列を返す
// ja - ヤコビ行列
// d - 方向余弦マトリックス
ShellElement.prototype.jacobInv=function(ja,d){
  var e1=ja.elements;
  var jd=new THREE.Matrix3().set
    (e1[0]*d[0][0]+e1[3]*d[1][0]+e1[6]*d[2][0],
     e1[0]*d[0][1]+e1[3]*d[1][1]+e1[6]*d[2][1],
     e1[0]*d[0][2]+e1[3]*d[1][2]+e1[6]*d[2][2],
     e1[1]*d[0][0]+e1[4]*d[1][0]+e1[7]*d[2][0],
     e1[1]*d[0][1]+e1[4]*d[1][1]+e1[7]*d[2][1],
     e1[1]*d[0][2]+e1[4]*d[1][2]+e1[7]*d[2][2],
     0,0,e1[2]*d[0][2]+e1[5]*d[1][2]+e1[8]*d[2][2]);
  return new THREE.Matrix3().getInverse(jd,true);
};

// 形状関数の勾配 [ dNi/dx dNi/dy ] を返す
// p - 要素節点
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
// t - 要素厚さ
ShellElement.prototype.grad=function(p,ja,sf,d,t){
  var count=this.nodeCount(),gr=[];
  var ji=this.jacobInv(ja,d).elements;
  for(var i=0;i<count;i++){
    var sfi=sf[i],dndxsi=sfi[1],dndeta=sfi[2];
    gr[i]=[ji[0]*dndxsi+ji[3]*dndeta,ji[1]*dndxsi+ji[4]*dndeta,
      	   ji[2]*dndxsi+ji[5]*dndeta];
  }
  return gr;
};

// 歪 - 変位マトリックスの転置行列を返す
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
ShellElement.prototype.strainMatrix1=function(ja,sf,d){
  var count=this.nodeCount(),m=numeric.rep([count,4],0);
  var ji=this.jacobInv(ja,d).elements;
  for(var i=0;i<count;i++){
    var mi=m[i],sfi=sf[i];
    for(var j=0;j<3;j++){
      mi[j]=ji[j]*sfi[1]+ji[j+3]*sfi[2];
    }
    mi[3]=ji[8]*sfi[0];
  }
  return m;
};

// 歪 - 変位マトリックスの転置行列を返す
// ただし歪は要素面座標、変位は全体座標
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
// zeta - 節点のζ座標
// t - 要素厚さ
ShellElement.prototype.strainMatrix=function(ja,sf,d,zeta,t){
  var b=this.strainMatrix1(ja,sf,d),z=0.5*t*zeta;
  var count=this.nodeCount(),m1=numeric.rep([5,6],0);
  var matrix=numeric.rep([6*count,5],0);
  for(var i=0;i<count;i++){
    var bi=b[i];
    m1[0][0]=bi[0];
    m1[0][4]=z*bi[0];
    m1[1][1]=bi[1];
    m1[1][3]=-z*bi[1];
    m1[2][0]=bi[1];
    m1[2][1]=bi[0];
    m1[2][3]=-z*bi[0];
    m1[2][4]=z*bi[1];
    m1[3][1]=bi[2];
    m1[3][2]=bi[1];
    m1[3][3]=-0.5*t*bi[3]-z*bi[2];
    m1[4][0]=bi[2];
    m1[4][2]=bi[0];
    m1[4][4]=0.5*t*bi[3]+z*bi[2];
    var ib=6*i;
    for(var i1=0;i1<5;i1++){
      var m1i=m1[i1];
      for(var j1=0;j1<3;j1++){
      	var dj=d[j1],s1=0,s2=0;
      	for(var k1=0;k1<3;k1++){
      	  s1+=m1i[k1]*dj[k1];
      	  s2+=m1i[k1+3]*dj[k1];
      	}
      	matrix[ib+j1][i1]+=s1;
      	matrix[ib+3+j1][i1]+=s2;
      }
    }
  }
  return matrix;
};

// 積分点の形状関数マトリックス [ NiNj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
// t - 要素厚さ
ShellElement.prototype.shapePart=function(p,x,w,t){
  var sf=this.shapeFunction(x[0],x[1]);
  var ja=this.jacobianMatrix(p,sf,normalVector(p),t);
  var count=this.nodeCount(),matrix=[];
  var coef=2*w*Math.abs(ja.determinant());
  for(var i=0;i<count;i++){
    var matr=[],cf2=coef*sf[i][0];
    for(var j=0;j<count;j++){
      matr[j]=cf2*sf[j][0];
    }
    matrix[i]=matr;
  }
  return matrix;
};

// 積分点の拡散マトリックス [ ∇Ni・∇Nj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
// t - 要素厚さ
ShellElement.prototype.gradPart=function(p,x,w,t){
  var sf=this.shapeFunction(x[0],x[1]);
  var ja=this.jacobianMatrix(p,sf,normalVector(p),t);
  var gr=this.grad(p,ja,sf,dirMatrix(p),t);
  var count=this.nodeCount(),matrix=[];
  var coef=2*w*Math.abs(ja.determinant());
  for(var i=0;i<count;i++){
    var matr=[],gri=gr[i];
    var c1=coef*gri[0],c2=coef*gri[1],c3=coef*gri[2];
    for(var j=0;j<count;j++){
      var grj=gr[j];
      matr[j]=c1*grj[0]+c2*grj[1]+c3*grj[2];
    }
    matrix[i]=matr;
  }
  return matrix;
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
ShellElement.prototype.shapeFunctionMatrix=function(p,coef,sp){
  var count=this.nodeCount(),s=numeric.rep([count,count],0);
  var t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    addMatrix(s,this.shapePart(p,this.intP[i],coef*this.intP[i][2],t));
  }
  return s;
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
ShellElement.prototype.gradMatrix=function(p,coef,sp){
  var count=this.nodeCount(),g=numeric.rep([count,count],0);
  var t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    addMatrix(g,this.gradPart(p,this.intP[i],coef*this.intP[i][2],t));
  }
  return g;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
ShellElement.prototype.geomStiffnessMatrix=function(p,u,d1,sp){
  var count=this.nodeCount(),kk=numeric.rep([6*count,6*count],0);
  var d=dirMatrix(p),n=normalVector(p);
  var v=this.toArray(u,6),t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    var ip=this.intP[i];
    var sf=this.shapeFunction(ip[0],ip[1]);
    var ja=this.jacobianMatrix(p,sf,n,t);
    var gr=this.grad(p,ja,sf,d,t);
    var sm=this.strainMatrix(ja,sf,d,0,t);
    var str=this.toStress(numeric.dotMV(d1,numeric.dotVM(v,sm)));
    var w=2*ip[2]*Math.abs(ja.determinant());
    for(var i1=0;i1<count;i1++){
      var i6=6*i1,gri=gr[i1];
      for(var j1=0;j1<count;j1++){
      	var j6=6*j1,grj=gr[j1];
      	var s=w*(gri[0]*(str.xx*grj[0]+str.xy*grj[1]+str.zx*grj[2])+
      	      	 gri[1]*(str.xy*grj[0]+str.yy*grj[1]+str.yz*grj[2])+
      	      	 gri[2]*(str.zx*grj[0]+str.yz*grj[1]+str.zz*grj[2]));
      	kk[i6][j6]+=s;
      	kk[i6+1][j6+1]+=s;
      	kk[i6+2][j6+2]+=s;
      }
    }
  }
  toDir3(d,kk);
  return kk;
};

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
ShellElement.prototype.strainStress=function(p,u,d1,sp){
  var count=this.nodeCount(),d=dirMatrix(p),n=normalVector(p);
  var v=this.toArray(u,6),t=sp.thickness;
  var strain1=[],stress1=[],energy1=[],strain2=[],stress2=[],energy2=[];
  for(var i=0;i<count;i++){
    var np=this.nodeP[i];
    var eps1=this.strainPart(p,v,n,d,np[0],np[1],1,t);
    var eps2=this.strainPart(p,v,n,d,np[0],np[1],-1,t);
    strain1[i]=this.toStrain(eps1);
    stress1[i]=this.toStress(numeric.dotMV(d1,eps1));
    strain2[i]=this.toStrain(eps2);
    stress2[i]=this.toStress(numeric.dotMV(d1,eps2));
    strain1[i].rotate(d);
    stress1[i].rotate(d);
    strain2[i].rotate(d);
    stress2[i].rotate(d);
    energy1[i]=0.5*strain1[i].innerProduct(stress1[i]);
    energy2[i]=0.5*strain2[i].innerProduct(stress2[i]);
  }
  return [strain1,stress1,energy1,strain2,stress2,energy2];
};

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// n - 法線ベクトル
// d - 方向余弦マトリックス
// xsi,eta,zeta - ξ,η,ζ座標
// t - 要素厚さ
ShellElement.prototype.strainPart=function(p,v,n,d,xsi,eta,zeta,t){
  var sf=this.shapeFunction(xsi,eta);
  var ja=this.jacobianMatrix(p,sf,n,t);
  var sm=this.strainMatrix(ja,sf,d,zeta,t);
  return numeric.dotVM(v,sm);
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
ShellElement.prototype.elementStrainStress=function(p,u,d1,sp){
  var d=dirMatrix(p),n=normalVector(p),v=this.toArray(u,6);
  var t=sp.thickness,cf=1/this.intP.length;
  var strain1=[0,0,0,0,0,0],stress1=[0,0,0,0,0,0],energy1=0;
  var strain2=[0,0,0,0,0,0],stress2=[0,0,0,0,0,0],energy2=0;
  for(var i=0;i<this.intP.length;i++){
    var ip=this.intP[i];
    var eps1=this.strainPart(p,v,n,d,ip[0],ip[1],1,t);
    var eps2=this.strainPart(p,v,n,d,ip[0],ip[1],-1,t);
    strain1=numeric.add(strain1,eps1);
    strain2=numeric.add(strain2,eps2);
    var str1=numeric.dotMV(d1,eps1);
    var str2=numeric.dotMV(d1,eps2);
    stress1=numeric.add(stress1,str1);
    stress2=numeric.add(stress2,str2);
    energy1+=numeric.dotVV(eps1,str1);
    energy2+=numeric.dotVV(eps2,str2);
  }
  strain1=numeric.mul(strain1,cf);
  stress1=numeric.mul(stress1,cf);
  energy1*=0.5*cf;
  strain2=numeric.mul(strain1,cf);
  stress2=numeric.mul(stress1,cf);
  energy2*=0.5*cf;
  return [this.toStrain(strain1),this.toStress(stress1),energy1,
      	  this.toStrain(strain2),this.toStress(stress2),energy2];
};

// ベクトルを歪に変換する
// s - 歪ベクトル
ShellElement.prototype.toStrain=function(s){
  return new Strain([s[0],s[1],0,s[2],s[3],s[4]]);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
ShellElement.prototype.toStress=function(s){
  return new Stress([s[0],s[1],0,s[2],s[3],s[4]]);
};

// 要素を表す文字列を返す
// materials - 材料
// params - シェルパラメータ
// p - 節点
ShellElement.prototype.toString=function(materials,params,p){
  var s=this.getName()+'\t'+this.label.toString(10)+'\t'+
      	materials[this.material].label.toString(10)+'\t'+
      	params[this.param].label.toString(10);
  for(var i=0;i<this.nodes.length;i++){
    s+='\t'+p[this.nodes[i]].label.toString(10);
  }
  return s;
};

//--------------------------------------------------------------------//
// 三角形1次要素 (薄肉シェル)
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
var TriElement1=function(label,material,param,nodes){
  ShellElement.call(this,label,material,param,nodes,TRI1_NODE,TRI1_INT);
};

// 要素名称を返す
TriElement1.prototype.getName=function(){
  return 'TriElement1';
};

// 節点数を返す
TriElement1.prototype.nodeCount=function(){
  return 3;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
TriElement1.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new TriangleBorder1(element,[p[0],p[1],p[2]]);
    case 1:
      return new TriangleBorder1(element,[p[0],p[2],p[1]]);
  }
};

// 要素境界辺を返す
// element - 要素ラベル
// index - 要素境界辺のインデックス
TriElement1.prototype.borderEdge=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new EdgeBorder1(element,[p[0],p[1]]);
    case 1:
      return new EdgeBorder1(element,[p[1],p[2]]);
    case 2:
      return new EdgeBorder1(element,[p[2],p[0]]);
  }
};

// 要素を鏡像反転する
TriElement1.prototype.mirror=function(){
  swap(this.nodes,1,2);
};

// １次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
TriElement1.prototype.shapeFunction=function(xsi,eta){
  return [[1-xsi-eta,-1,-1],[xsi,1,0],[eta,0,1]];
};

// ２次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
TriElement1.prototype.shapeFunction2=function(xsi,eta){
  var xe=1-xsi-eta;
  return [[xe*(2*xe-1),1-4*xe,1-4*xe],[xsi*(2*xsi-1),4*xsi-1,0],
      	  [eta*(2*eta-1),0,4*eta-1],[4*xe*xsi,4*(xe-xsi),-4*xsi],
      	  [4*xsi*eta,4*eta,4*xsi],[4*xe*eta,-4*eta,4*(xe-eta)]];
};

// 角度の形状関数行列 [ Hxi Hyi dHxi/dξ dHyi/dξ dHxi/dη dHyi/dη ] を返す
// p - 要素節点
// d - 方向余弦マトリックス
// xsi,eta - 要素内部ξ,η座標
TriElement1.prototype.shapeFunction3=function(p,d,xsi,eta){
  var count=this.nodeCount(),m=numeric.rep([3*count,6],0);
  var sf2=this.shapeFunction2(xsi,eta);
  var d12=p[1].clone().sub(p[0]);
  var d23=p[2].clone().sub(p[1]);
  var d31=p[0].clone().sub(p[2]);
  var l=[1/d12.lengthSq(),1/d23.lengthSq(),1/d31.lengthSq()];
  var x=[d[0][0]*d12.x+d[1][0]*d12.y+d[2][0]*d12.z,
      	 d[0][0]*d23.x+d[1][0]*d23.y+d[2][0]*d23.z,
      	 d[0][0]*d31.x+d[1][0]*d31.y+d[2][0]*d31.z];
  var y=[d[0][1]*d12.x+d[1][1]*d12.y+d[2][1]*d12.z,
      	 d[0][1]*d23.x+d[1][1]*d23.y+d[2][1]*d23.z,
      	 d[0][1]*d31.x+d[1][1]*d31.y+d[2][1]*d31.z];
  var a=[1.5*l[0]*y[0],1.5*l[1]*y[1],1.5*l[2]*y[2]];
  var b=[-1.5*l[0]*x[0],-1.5*l[1]*x[1],-1.5*l[2]*x[2]];
  var c=[0.75*l[0]*y[0]*y[0]-0.5,0.75*l[1]*y[1]*y[1]-0.5,
      	 0.75*l[2]*y[2]*y[2]-0.5];
  var d1=[0.75*l[0]*x[0]*y[0],0.75*l[1]*x[1]*y[1],0.75*l[2]*x[2]*y[2]];
  var e=[0.25-0.75*l[0]*y[0]*y[0],0.25-0.75*l[1]*y[1]*y[1],
      	 0.25-0.75*l[2]*y[2]*y[2]];
  for(var i=0;i<3;i++){
    var i1=(i+2)%3;
    var i3=3*i;
    for(var j=0;j<3;j++){
      var j2=2*j;
      m[i3][j2]=a[i1]*sf2[3+i1][j]-a[i]*sf2[3+i][j];
      m[i3][j2+1]=b[i1]*sf2[3+i1][j]-b[i]*sf2[3+i][j];
      m[i3+1][j2]=sf2[i][j]-c[i1]*sf2[3+i1][j]-c[i]*sf2[3+i][j];
      var dn=d1[i1]*sf2[3+i1][j]+d1[i]*sf2[3+i][j];
      m[i3+1][j2+1]=dn;
      m[i3+2][j2]=dn;
      m[i3+2][j2+1]=sf2[i][j]-e[i1]*sf2[3+i1][j]-e[i]*sf2[3+i][j];
    }
  }
  return m;
};

// ヤコビアンを返す
// p - 要素節点
TriElement1.prototype.jacobian=function(p){
  var p0x=p[0].x,p0y=p[0].y,p0z=p[0].z;
  var j1=(p[1].y-p0y)*(p[2].z-p0z)-(p[1].z-p0z)*(p[2].y-p0y);
  var j2=(p[1].z-p0z)*(p[2].x-p0x)-(p[1].x-p0x)*(p[2].z-p0z);
  var j3=(p[1].x-p0x)*(p[2].y-p0y)-(p[1].y-p0y)*(p[2].x-p0x);
  return Math.sqrt(j1*j1+j2*j2+j3*j3);
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
// t - 要素厚さ
TriElement1.prototype.massMatrix=function(p,dens,t){
  var count=this.nodeCount(),m=numeric.rep([6*count,6*count],0);
  var mb=numeric.rep([3*count,3*count],0),d=dirMatrix(p);
  var djt=dens*t*this.jacobian(p),tt=C1_12*t*t,dm=C1_12*djt,i,j;

  for(var k=0;k<3;k++){
    var ipi=TRI2_INT[k],sf3=this.shapeFunction3(p,d,ipi[0],ipi[1]);
    var sf=this.shapeFunction(ipi[0],ipi[1]);
    var hz=[sf[0][0],0,0,sf[1][0],0,0,sf[2][0],0,0],cfm=djt*ipi[2];
    for(i=0;i<3*count;i++){
      for(j=0;j<3*count;j++){
      	var hxhy=sf3[i][0]*sf3[j][0]+sf3[i][1]*sf3[j][1];
      	mb[i][j]+=cfm*(tt*hxhy+hz[i]*hz[j]);
      }
    }
  }

  for(i=0;i<count;i++){
    var i3=3*i,i6=6*i;
    for(j=0;j<count;j++){
      var j3=3*j,j6=6*j,cf1=TRI1_MASS1[i][j];
      var dme=cf1*dm;
      m[i6][j6]=dme;
      m[i6+1][j6+1]=dme;
      for(var i1=0;i1<3;i1++){
      	for(var j1=0;j1<3;j1++){
      	  m[i6+2+i1][j6+2+j1]=mb[i3+i1][j3+j1];
      	}
      }
      m[i6+5][j6+5]=1e-5*dm*tt;
    }
  }
  toDir3(d,m);
  return m;
};

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
TriElement1.prototype.stiffnessMatrix=function(p,d1,sp){
  var d=dirMatrix(p),n=normalVector(p),t=sp.thickness,i,j,ii,jj;

  var sf1=this.shapeFunction(C1_3,C1_3);
  var ja1=this.jacobianMatrix(p,sf1,n,t);
  var jac1=ja1.determinant();
  var jinv=this.jacobInv(ja1,d);
  var b1=this.strainMatrix1(sf1,jinv);
  var k1=this.stiffPart(d1,b1,Math.abs(jac1));

  var count=this.nodeCount(),k2=numeric.rep([3*count,3*count],0);
  var coef=t*t*Math.abs(jac1)/36;
  for(i=0;i<3;i++){
    var ipi=TRI2_INT[i],sf3=this.shapeFunction3(p,d,ipi[0],ipi[1]);
    var b2=this.strainMatrix2(sf3,jinv);
    addMatrix(k2,this.stiffPart(d1,b2,coef));
  }

  var ce1=1e-3*t*t*Math.abs(jac1)*d1[2][2],ce2=-ce1/2;
  var kk=numeric.rep([6*count,6*count],0),ks=numeric.rep([6,6],0);
  var dir=numeric.rep([6,6],0);
  for(i=0;i<3;i++){
    for(j=0;j<3;j++){
      dir[i][j]=d[i][j];
      dir[i+3][j+3]=d[i][j];
    }
  }
  for(i=0;i<3;i++){
    var i2=2*i;
    var i3=3*i;
    var i6=6*i;
    for(j=0;j<count;j++){
      var j2=2*j;
      var j3=3*j;
      var j6=6*j;
      for(ii=0;ii<6;ii++){
      	for(jj=0;jj<6;jj++){
      	  ks[ii][jj]=0;
      	}
      }
      ks[0][0]=k1[i2][j2];
      ks[0][1]=k1[i2][j2+1];
      ks[1][0]=k1[i2+1][j2];
      ks[1][1]=k1[i2+1][j2+1];
      for(ii=0;ii<3;ii++){
      	for(jj=0;jj<3;jj++){
      	  ks[2+ii][2+jj]=k2[i3+ii][j3+jj];
      	}
      }
      if(i==j) ks[5][5]=ce1;
      else     ks[5][5]=ce2;
      toDir(dir,ks);
      for(ii=0;ii<6;ii++){
      	for(jj=0;jj<6;jj++){
      	  kk[i6+ii][j6+jj]=ks[ii][jj];
      	}
      }
    }
  }
  return kk;
};

// 歪 - 変位マトリックスの転置行列を返す
// sf - 形状関数行列
// jinv - 逆ヤコビ行列
TriElement1.prototype.strainMatrix1=function(sf,jinv){
  var count=this.nodeCount(),b=numeric.rep([2*count,3],0);
  var ji=jinv.elements;
  for(var i=0;i<count;i++){
    var sfi=sf[i];
    var dndx=ji[0]*sfi[1]+ji[3]*sfi[2];
    var dndy=ji[1]*sfi[1]+ji[4]*sfi[2];
    var i2=2*i;
    b[i2][0]=dndx;
    b[i2+1][1]=dndy;
    b[i2][2]=dndy;
    b[i2+1][2]=dndx;
  }
  return b;
};

// 面外歪 - 変位マトリックスの転置行列を返す
// sf - 形状関数行列
// jinv - 逆ヤコビ行列
TriElement1.prototype.strainMatrix2=function(sf,jinv){
  var count=3*this.nodeCount(),b=[];
  var ji=jinv.elements;
  for(var i=0;i<count;i++){
    var sfi=sf[i];
    var hxx=ji[0]*sfi[2]+ji[3]*sfi[4];
    var hxy=ji[1]*sfi[2]+ji[4]*sfi[4];
    var hyx=ji[0]*sfi[3]+ji[3]*sfi[5];
    var hyy=ji[1]*sfi[3]+ji[4]*sfi[5];
    b[i]=[hyx,-hxy,hyy-hxx];
  }
  return b;
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// t - 要素厚さ
TriElement1.prototype.shapeFunctionMatrix=function(p,coef,t){
  var ds=coef*this.jacobian(p)/12;
  var count=3*this.nodeCount(),s=numeric.rep([count],0.5*ds);
  for(var i=0;i<count;i++) s[i][i]=ds;
  return s;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
TriElement1.prototype.geomStiffnessMatrix=function(p,u,d1,sp){
  var count=this.nodeCount(),kk=numeric.rep([6*count,6*count],0);
  var d=dirMatrix(p),n=normalVector(p);
  var v=this.toArray(u,6),t=sp.thickness;
  var ip=this.intP[0];
  var sf1=this.shapeFunction(ip[0],ip[1]);
  var ja=this.jacobianMatrix(p,sf1,n,t);
  var gr=this.grad(p,ja,sf1,d,t);
  var jinv=this.jacobInv(ja,d);
  var sf3=this.shapeFunction3(p,d,ip[0],ip[1]);
  var sm=this.strainMatrix(sf1,sf3,jinv,d,0,t);
  var str=this.toStress(numeric.dotMV(d1,numeric.dotVM(v,sm)));
  var w=Math.abs(ja.determinant());
  for(var i1=0;i1<count;i1++){
    var i6=6*i1,gri=gr[i1];
    for(var j1=0;j1<count;j1++){
      var j6=6*j1,grj=gr[j1];
      var s=w*(gri[0]*(str.xx*grj[0]+str.xy*grj[1]+str.zx*grj[2])+
      	       gri[1]*(str.xy*grj[0]+str.yy*grj[1]+str.yz*grj[2])+
      	       gri[2]*(str.zx*grj[0]+str.yz*grj[1]+str.zz*grj[2]));
      kk[i6][j6]=s;
      kk[i6+1][j6+1]=s;
      kk[i6+2][j6+2]=s;
    }
  }
  toDir3(d,kk);
  return kk;
};

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// n - 法線ベクトル
// d - 方向余弦マトリックス
// xsi,eta,zeta - ξ,η,ζ座標
// t - 要素厚さ
TriElement1.prototype.strainPart=function(p,v,n,d,xsi,eta,zeta,t){
  var sf1=this.shapeFunction(xsi,eta);
  var ja=this.jacobianMatrix(p,sf1,n,t);
  var jinv=this.jacobInv(ja,d);
  var sf3=this.shapeFunction3(p,d,xsi,eta);
  var sm=this.strainMatrix(sf1,sf3,jinv,d,zeta,t);
  return numeric.dotVM(v,sm);
};

// 歪 - 変位マトリックスの転置行列を返す
// ただし歪は要素面座標、変位は全体座標
// sf1 - 面内変形の形状関数行列
// sf3 - 面外変形の形状関数行列
// jinv - 逆ヤコビ行列
// d - 方向余弦マトリックス
// zeta - 節点のζ座標
// t - 要素厚さ
TriElement1.prototype.strainMatrix=function(sf1,sf3,jinv,d,zeta,t){
  var b1=this.strainMatrix1(sf1,jinv);
  var b2=this.strainMatrix2(sf3,jinv);
  var count=this.nodeCount(),m1=numeric.rep([3,6],0);
  var matrix=numeric.rep([6*count,3],0),z=0.5*t*zeta,i1;
  for(var i=0;i<count;i++){
    var i2=2*i;
    var i3=3*i;
    var i6=6*i;
    for(i1=0;i1<3;i1++){
      m1[i1][0]=b1[i2][i1];
      m1[i1][1]=b1[i2+1][i1];
      m1[i1][2]=z*b2[i3][i1];
      m1[i1][3]=z*b2[i3+1][i1];
      m1[i1][4]=z*b2[i3+2][i1];
      m1[i1][5]=0;
    }
    for(i1=0;i1<3;i1++){
      var m1i=m1[i1];
      for(var j1=0;j1<3;j1++){
      	var dj=d[j1],s1=0,s2=0;
      	for(var k1=0;k1<3;k1++){
      	  s1+=m1i[k1]*dj[k1];
      	  s2+=m1i[k1+3]*dj[k1];
      	}
      	matrix[i6+j1][i1]+=s1;
      	matrix[i6+3+j1][i1]+=s2;
      }
    }
  }
  return matrix;
};

// ベクトルを歪に変換する
// s - 歪ベクトル
TriElement1.prototype.toStrain=function(s){
  return new Strain([s[0],s[1],0,s[2],0,0]);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
TriElement1.prototype.toStress=function(s){
  return new Stress([s[0],s[1],0,s[2],0,0]);
};

//--------------------------------------------------------------------//
// 四角形1次要素 (MITC4)
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
var QuadElement1=function(label,material,param,nodes){
  ShellElement.call(this,label,material,param,nodes,
      	      	    QUAD1_NODE,QUAD1_INT);
};

// 要素名称を返す
QuadElement1.prototype.getName=function(){
  return 'QuadElement1';
};

// 節点数を返す
QuadElement1.prototype.nodeCount=function(){
  return 4;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
QuadElement1.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new QuadangleBorder1(element,[p[0],p[1],p[2],p[3]]);
    case 1:
      return new QuadangleBorder1(element,[p[0],p[3],p[2],p[1]]);
  }
};

// 要素境界辺を返す
// element - 要素ラベル
// index - 要素境界辺のインデックス
QuadElement1.prototype.borderEdge=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new EdgeBorder1(element,[p[0],p[1]]);
    case 1:
      return new EdgeBorder1(element,[p[1],p[2]]);
    case 2:
      return new EdgeBorder1(element,[p[2],p[3]]);
    case 3:
      return new EdgeBorder1(element,[p[3],p[0]]);
  }
};

// 要素を鏡像反転する
QuadElement1.prototype.mirror=function(){
  swap(this.nodes,1,3);
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
QuadElement1.prototype.shapeFunction=function(xsi,eta){
  return [[0.25*(1-xsi)*(1-eta),-0.25*(1-eta),-0.25*(1-xsi)],
      	  [0.25*(1+xsi)*(1-eta),0.25*(1-eta),-0.25*(1+xsi)],
      	  [0.25*(1+xsi)*(1+eta),0.25*(1+eta),0.25*(1+xsi)],
      	  [0.25*(1-xsi)*(1+eta),-0.25*(1+eta),0.25*(1-xsi)]];
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
// t - 要素厚さ
QuadElement1.prototype.massMatrix=function(p,dens,t){
  var count=this.nodeCount(),m=numeric.rep([6*count,6*count],0);
  var d=dirMatrix(p),n=normalVector(p),tt=C1_12*t*t;
  for(var i=0;i<this.intP.length;i++){
    var ipi=this.intP[i],sf=this.shapeFunction(ipi[0],ipi[1]);
    var nn=[sf[0][0],sf[1][0],sf[2][0],sf[3][0]];
    var jac=Math.abs(this.jacobianMatrix(p,sf,n,t).determinant());
    jac*=2*ipi[2];
    for(var i1=0;i1<count;i1++){
      var i6=6*i1,nja=nn[i1]*jac;
      for(var j1=0;j1<count;j1++){
      	var j6=6*j1,nnja=nja*nn[j1],dm=dens*nnja,dii=tt*dm;
      	m[i6][j6]+=dm;
      	m[i6+1][j6+1]+=dm;
      	m[i6+2][j6+2]+=dm;
      	m[i6+3][j6+3]+=dii;
      	m[i6+4][j6+4]+=dii;
      	m[i6+5][j6+5]+=1e-4*dii;
      }
    }
  }
  toDir3(d,m);
  return m;
};

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
QuadElement1.prototype.stiffnessMatrix=function(p,d1,sp){
  var size=6*this.nodeCount(),kk=numeric.rep([size,size],0);
  var n=normalVector(p),t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    var ks=this.stiffPart(p,d1,n,this.intP[i][0],this.intP[i][1],t);
    addMatrix(kk,ks);
  }
  return kk;
};

// 積分点の剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// n - 法線ベクトル
// xsi,eta - 要素内部ξ,η座標
// t - 要素厚さ
QuadElement1.prototype.stiffPart=function(p,d1,n,xsi,eta,t){
  var d=dirMatrix(p);
  var sf=this.shapeFunction(xsi,eta);
  var ja=this.jacobianMatrix(p,sf,n,t);
  var bc0=this.strainMatrix1(ja,sf,d);
  var sf1=this.shapeFunction(xsi,0);
  var ja1=this.jacobianMatrix(p,sf1,n,t);
  var sf2=this.shapeFunction(0,eta);
  var ja2=this.jacobianMatrix(p,sf2,n,t);
  var bc=[this.strainMatrix1(ja1,sf1,d),this.strainMatrix1(ja2,sf2,d)];
  var count=this.nodeCount();
  var kk=numeric.rep([6*count,6*count],0);
  var jacob=Math.abs(ja.determinant());

  var tt6=t*t/6.0,ce1=1e-3*t*t*d1[3][3],ce2=-ce1/(count-1),j1,j2;
  var k1=[[0,0,0],[0,0,0],[0,0,0]];
  var k2=[[0,0,0],[0,0,0],[0,0,0]];
  var k3=[[0,0,0],[0,0,0],[0,0,0]];
  var k4=[[0,0,0],[0,0,0],[0,0,0]];
  for(var i=0;i<count;i++){
    for(var j=0;j<count;j++){
      for(j1=0;j1<3;j1++){
      	for(j2=0;j2<3;j2++){
      	  k1[j1][j2]=0;
      	  k2[j1][j2]=0;
      	  k3[j1][j2]=0;
      	  k4[j1][j2]=0;
      	}
      }
      for(j1=0;j1<2;j1++){
      	for(j2=0;j2<2;j2++){
      	  k1[j1][j2]=bc0[i][j1]*d1[j1][j2]*bc0[j][j2]+
      	      	     bc0[i][1-j1]*d1[2][2]*bc0[j][1-j2];
      	}
      	var dd=d1[4-j1][4-j1];
      	k1[j1][j1]+=bc[1-j1][i][2]*dd*bc[1-j1][j][2];
      	k1[j1][2]=bc[1-j1][i][2]*dd*bc[j1][j][j1];
      	k1[2][j1]=bc[j1][i][j1]*dd*bc[1-j1][j][2];
      	k2[j1][j1]=bc[1-j1][i][2]*dd*bc[1-j1][j][3];
      	k2[2][j1]=bc[1-j1][i][j1]*dd*bc[1-j1][j][3];
      	k3[j1][j1]=bc[1-j1][i][3]*dd*bc[1-j1][j][2];
      	k3[j1][2]=bc[1-j1][i][3]*dd*bc[1-j1][j][j1];
      }
      k1[2][2]=bc[0][i][1]*d1[3][3]*bc[0][j][1]+
      	       bc[1][i][0]*d1[4][4]*bc[1][j][0];
      k4[0][0]=k1[1][1]+3*bc[0][i][3]*d1[3][3]*bc[0][j][3];
      k4[0][1]=-k1[1][0];
      k4[1][0]=-k1[0][1];
      k4[1][1]=k1[0][0]+3*bc[1][i][3]*d1[4][4]*bc[1][j][3];
      for(j1=0;j1<3;j1++){
      	var kt=k2[j1][0];
      	k2[j1][0]=-k2[j1][1];
      	k2[j1][1]=kt;
      	kt=k3[0][j1];
      	k3[0][j1]=-k3[1][j1];
      	k3[1][j1]=kt;
      }

      if(i==j) k4[2][2]=ce1;
      else     k4[2][2]=ce2;
      toDir(d,k1);
      toDir(d,k2);
      toDir(d,k3);
      toDir(d,k4);
      var i0=6*i;
      var j0=6*j;
      for(j1=0;j1<3;j1++){
      	for(j2=0;j2<3;j2++){
      	  kk[i0+j1][j0+j2]=2*jacob*k1[j1][j2];
      	  kk[i0+j1][j0+3+j2]=t*jacob*k2[j1][j2];
      	  kk[i0+3+j1][j0+j2]=t*jacob*k3[j1][j2];
      	  kk[i0+3+j1][j0+3+j2]=tt6*jacob*k4[j1][j2];
      	}
      }
    }
  }
  return kk;
};

// 節点座標を局所座標系に変換する
// d - 方向余弦マトリックス
// p - 要素節点
function toLocal(d,p){
  var x=[];
  for(var i=0;i<p.length;i++){
    x[i]=new THREE.Vector3().set
      (d[0][0]*p[i].x+d[1][0]*p[i].y+d[2][0]*p[i].z,
       d[0][1]*p[i].x+d[1][1]*p[i].y+d[2][1]*p[i].z,
       d[0][2]*p[i].x+d[1][2]*p[i].y+d[2][2]*p[i].z);
  }
  return x;
}

inherits(ShellElement,FElement);
inherits(TriElement1,ShellElement);
inherits(QuadElement1,ShellElement);
