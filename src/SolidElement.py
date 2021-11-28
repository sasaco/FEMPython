// 四面体2次要素の節点のξ,η,ζ座標
var TETRA2_NODE=[[0,0,0],[1,0,0],[0,1,0],[0,0,1],[0.5,0,0],[0.5,0.5,0],
      	      	 [0,0.5,0],[0,0,0.5],[0.5,0,0.5],[0,0.5,0.5]];
// 四面体2次要素の積分点のξ,η,ζ座標,重み係数
var TETRA2_INT=[[GTETRA2[0],GTETRA2[0],GTETRA2[0],C1_24],
      	      	[GTETRA2[1],GTETRA2[0],GTETRA2[0],C1_24],
      	      	[GTETRA2[0],GTETRA2[1],GTETRA2[0],C1_24],
      	      	[GTETRA2[0],GTETRA2[0],GTETRA2[1],C1_24]];
// 楔形1次要素の節点のξ,η,ζ座標
var WEDGE1_NODE=[[0,0,-1],[1,0,-1],[0,1,-1],[0,0,1],[1,0,1],[0,1,1]];
// 楔形1次要素の積分点のξ,η,ζ座標,重み係数
var WEDGE1_INT=[[C1_3,C1_3,GX2[0],0.5],[C1_3,C1_3,GX2[1],0.5]];
// 楔形2次要素の節点のξ,η,ζ座標
var WEDGE2_NODE=[[0,0,-1],[1,0,-1],[0,1,-1],[0,0,1],[1,0,1],[0,1,1],
      	      	 [0.5,0,-1],[0.5,0.5,-1],[0,0.5,-1],
      	      	 [0.5,0,1],[0.5,0.5,1],[0,0.5,1],
      	      	 [0,0,0],[1,0,0],[0,1,0]];
// 楔形2次要素の積分点のξ,η,ζ座標,重み係数
var WEDGE2_INT=[[GTRI2[0],GTRI2[0],GX3[0],C1_6*GW3[0]],
      	      	[GTRI2[1],GTRI2[0],GX3[0],C1_6*GW3[0]],
      	      	[GTRI2[0],GTRI2[1],GX3[0],C1_6*GW3[0]],
      	      	[GTRI2[0],GTRI2[0],GX3[1],C1_6*GW3[1]],
      	      	[GTRI2[1],GTRI2[0],GX3[1],C1_6*GW3[1]],
      	      	[GTRI2[0],GTRI2[1],GX3[1],C1_6*GW3[1]],
      	      	[GTRI2[0],GTRI2[0],GX3[2],C1_6*GW3[2]],
      	      	[GTRI2[1],GTRI2[0],GX3[2],C1_6*GW3[2]],
      	      	[GTRI2[0],GTRI2[1],GX3[2],C1_6*GW3[2]]];
// 六面体1次要素の節点のξ,η,ζ座標
var HEXA1_NODE=[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],
      	      	[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]];
// 六面体1次要素の積分点のξ,η,ζ座標,重み係数
var HEXA1_INT=[[GX2[0],GX2[0],GX2[0],1],[GX2[1],GX2[0],GX2[0],1],
      	       [GX2[0],GX2[1],GX2[0],1],[GX2[1],GX2[1],GX2[0],1],
      	       [GX2[0],GX2[0],GX2[1],1],[GX2[1],GX2[0],GX2[1],1],
      	       [GX2[0],GX2[1],GX2[1],1],[GX2[1],GX2[1],GX2[1],1]];
// 六面体2次要素の節点のξ,η,ζ座標
var HEXA2_NODE=[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],
      	      	[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1],
      	      	[0,-1,-1],[1,0,-1],[0,1,-1],[-1,0,-1],
      	      	[0,-1,1],[1,0,1],[0,1,1],[-1,0,1],
      	      	[-1,-1,0],[1,-1,0],[1,1,0],[-1,1,0]];
// 六面体2次要素の積分点のξ,η,ζ座標,重み係数
var HEXA2_INT=(function(){
  var a=[];
  for(var k=0;k<3;k++){
    for(var j=0;j<3;j++){
      for(var i=0;i<3;i++){
      	a.push([GX3[i],GX3[j],GX3[k],GW3[i]*GW3[j]*GW3[k]]);
      }
    }
  }
  return a;
}());
// 六面体1次要素の質量マトリックス係数
var HEXA1_MASS_BASE=(function(){
  var v=[],abs=Math.abs;
  for(var i=0;i<8;i++){
    v[i]=[];
    for(var j=0;j<8;j++){
      var s=abs(HEXA1_NODE[i][0]-HEXA1_NODE[j][0])+
      	    abs(HEXA1_NODE[i][1]-HEXA1_NODE[j][1])+
      	    abs(HEXA1_NODE[i][2]-HEXA1_NODE[j][2]);
      v[i][j]=Math.pow(0.5,0.5*s)/27;
    }
  }
  return v;
}());

//--------------------------------------------------------------------//
// ソリッド要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
// nodeP - 節点のξ,η,ζ座標
// intP - 積分点のξ,η,ζ座標,重み係数
var SolidElement=function(label,material,nodes,nodeP,intP){
  FElement.call(this,label,material,nodes);
  this.nodeP=nodeP;
  this.intP=intP;
};

// ヤコビ行列を返す
// p - 要素節点
// sf - 形状関数行列
SolidElement.prototype.jacobianMatrix=function(p,sf){
  var count=this.nodeCount(),jac=[0,0,0,0,0,0,0,0,0];
  for(var i=0;i<count;i++){
    var sfi=sf[i],pix=p[i].x,piy=p[i].y,piz=p[i].z;
    for(var j=0;j<3;j++){
      var sfij=sfi[j+1];
      jac[j]+=sfij*pix;
      jac[j+3]+=sfij*piy;
      jac[j+6]+=sfij*piz;
    }
  }
  return new THREE.Matrix3().fromArray(jac);
};

// 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
// p - 要素節点
// ja - ヤコビ行列
// sf - 形状関数行列
SolidElement.prototype.grad=function(p,ja,sf){
  var count=this.nodeCount(),gr=[];
  var ji=new THREE.Matrix3().getInverse(ja,true).elements;
  for(var i=0;i<count;i++){
    gr[i]=[ji[0]*sf[i][1]+ji[3]*sf[i][2]+ji[6]*sf[i][3],
      	   ji[1]*sf[i][1]+ji[4]*sf[i][2]+ji[7]*sf[i][3],
      	   ji[2]*sf[i][1]+ji[5]*sf[i][2]+ji[8]*sf[i][3]];
  }
  return gr;
};

// 歪 - 変位マトリックスの転置行列を返す
// grad - 形状関数の勾配
SolidElement.prototype.strainMatrix=function(grad){
  var count=this.nodeCount(),m=numeric.rep([3*count,6],0);
  for(var i=0;i<count;i++){
    var i3=3*i,gr=grad[i];
    m[i3][0]=gr[0];
    m[i3+1][1]=gr[1];
    m[i3+2][2]=gr[2];
    m[i3][3]=gr[1];
    m[i3+1][3]=gr[0];
    m[i3+1][4]=gr[2];
    m[i3+2][4]=gr[1];
    m[i3][5]=gr[2];
    m[i3+2][5]=gr[0];
  }
  return m;
};

// 積分点の形状関数マトリックス [ NiNj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
SolidElement.prototype.shapePart=function(p,x,w){
  var sf=this.shapeFunction(x[0],x[1],x[2]);
  var ja=this.jacobianMatrix(p,sf);
  var count=this.nodeCount(),matrix=[];
  var coef=w*Math.abs(ja.determinant());
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
SolidElement.prototype.gradPart=function(p,x,w){
  var sf=this.shapeFunction(x[0],x[1],x[2]);
  var ja=this.jacobianMatrix(p,sf);
  var gr=this.grad(p,ja,sf);
  var count=this.nodeCount(),matrix=[];
  var coef=w*Math.abs(ja.determinant());
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

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
SolidElement.prototype.massMatrix=function(p,dens){
  var count=this.nodeCount(),m=numeric.rep([3*count,3*count],0);
  for(var i=0;i<this.intP.length;i++){
    var sf=this.shapeFunction(this.intP[i][0],this.intP[i][1],
      	      	      	      this.intP[i][2]);
    var ja=this.jacobianMatrix(p,sf);
    var coef=this.intP[i][3]*dens*Math.abs(ja.determinant());
    for(var i1=0;i1<count;i1++){
      for(var j1=0;j1<count;j1++){
      	var value=coef*sf[i1][0]*sf[j1][0],i3=3*i1,j3=3*j1;
      	m[i3][j3]+=value;
      	m[i3+1][j3+1]+=value;
      	m[i3+2][j3+2]+=value;
      }
    }
  }
  return m;
};

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
SolidElement.prototype.stiffnessMatrix=function(p,d1){
  var count=3*this.nodeCount(),kk=numeric.rep([count,count],0);
  for(var i=0;i<this.intP.length;i++){
    var ip=this.intP[i];
    var sf=this.shapeFunction(ip[0],ip[1],ip[2]);
    var ja=this.jacobianMatrix(p,sf);
    var ks=this.stiffPart(d1,this.strainMatrix(this.grad(p,ja,sf)),
      	      	      	  ip[3]*Math.abs(ja.determinant()));
    addMatrix(kk,ks);
  }
  return kk;
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
SolidElement.prototype.shapeFunctionMatrix=function(p,coef){
  var count=this.nodeCount(),s=numeric.rep([count,count],0);
  for(var i=0;i<this.intP.length;i++){
    addMatrix(s,this.shapePart(p,this.intP[i],coef*this.intP[i][3]));
  }
  return s;
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
SolidElement.prototype.gradMatrix=function(p,coef){
  var count=this.nodeCount(),g=numeric.rep([count,count],0);
  for(var i=0;i<this.intP.length;i++){
    addMatrix(g,this.gradPart(p,this.intP[i],coef*this.intP[i][3]));
  }
  return g;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
SolidElement.prototype.geomStiffnessMatrix=function(p,u,d1){
  var count=this.nodeCount(),kk=numeric.rep([3*count,3*count],0);
  var v=this.toArray(u,3);
  for(var i=0;i<this.intP.length;i++){
    var ip=this.intP[i];
    var sf=this.shapeFunction(ip[0],ip[1],ip[2]);
    var ja=this.jacobianMatrix(p,sf);
    var gr=this.grad(p,ja,sf);
    var sm=this.strainMatrix(gr);
    var str=numeric.dotMV(d1,numeric.dotVM(v,sm));
    var w=ip[3]*Math.abs(ja.determinant());
    for(var i1=0;i1<count;i1++){
      var i3=3*i1,gri=gr[i1];
      for(var j1=0;j1<count;j1++){
      	var j3=3*j1,grj=gr[j1];
      	var s=w*(gri[0]*(str[0]*grj[0]+str[3]*grj[1]+str[5]*grj[2])+
      	      	 gri[1]*(str[3]*grj[0]+str[1]*grj[1]+str[4]*grj[2])+
      	      	 gri[2]*(str[5]*grj[0]+str[4]*grj[1]+str[2]*grj[2]));
      	kk[i3][j3]+=s;
      	kk[i3+1][j3+1]+=s;
      	kk[i3+2][j3+2]+=s;
      }
    }
  }
  return kk;
};

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
SolidElement.prototype.strainStress=function(p,u,d1){
  var count=this.nodeCount(),v=this.toArray(u,3);
  var strain=[],stress=[],energy=[];
  for(var i=0;i<count;i++){
    var eps=this.strainPart(p,v,this.nodeP[i]);
    strain[i]=new Strain(eps);
    var str=numeric.dotMV(d1,eps);
    stress[i]=new Stress(str);
    energy[i]=0.5*strain[i].innerProduct(stress[i]);
  }
  return [strain,stress,energy];
};

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// x - ξ,η,ζ座標
SolidElement.prototype.strainPart=function(p,v,x){
  var sf=this.shapeFunction(x[0],x[1],x[2]);
  var ja=this.jacobianMatrix(p,sf);
  var sm=this.strainMatrix(this.grad(p,ja,sf));
  return numeric.dotVM(v,sm);
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
SolidElement.prototype.elementStrainStress=function(p,u,d1){
  var v=this.toArray(u,3),cf=1/this.intP.length;
  var strain=[0,0,0,0,0,0],stress=[0,0,0,0,0,0],energy=0;
  for(var i=0;i<this.intP.length;i++){
    var eps=this.strainPart(p,v,this.intP[i]);
    strain=numeric.add(strain,eps);
    var str=numeric.dotMV(d1,eps);
    stress=numeric.add(stress,str);
    energy+=numeric.dotVV(eps,str);
  }
  strain=numeric.mul(strain,cf);
  stress=numeric.mul(stress,cf);
  energy*=0.5*cf;
  return [new Strain(strain),new Stress(stress),energy];
};

// 要素を表す文字列を返す
// materials - 材料
// p - 節点
SolidElement.prototype.toString=function(materials,p){
  var s=this.getName()+'\t'+this.label.toString(10)+'\t'+
      	materials[this.material].label.toString(10);
  for(var i=0;i<this.nodes.length;i++){
    s+='\t'+p[this.nodes[i]].label.toString(10);
  }
  return s;
};

//--------------------------------------------------------------------//
// 四面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var TetraElement1=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,null,null);
};

// 要素名称を返す
TetraElement1.prototype.getName=function(){
  return 'TetraElement1';
};

// 節点数を返す
TetraElement1.prototype.nodeCount=function(){
  return 4;
};

// 要素境界数を返す
TetraElement1.prototype.borderCount=function(){
  return 4;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
TetraElement1.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new TriangleBorder1(element,[p[0],p[2],p[1]]);
    case 1:
      return new TriangleBorder1(element,[p[0],p[1],p[3]]);
    case 2:
      return new TriangleBorder1(element,[p[1],p[2],p[3]]);
    case 3:
      return new TriangleBorder1(element,[p[2],p[0],p[3]]);
  }
};

// 要素を鏡像反転する
TetraElement1.prototype.mirror=function(){
  swap(this.nodes,1,2);
};

// 要素節点の角度を返す
// p - 要素節点
TetraElement1.prototype.angle=function(p){
  var th=[];
  for(var i=0;i<4;i++){
    th[i]=solidAngle(p[i],p[(i+1)%4],p[(i+2)%4],p[(i+3)%4]);
  }
  return th;
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
TetraElement1.prototype.shapeFunction=function(xsi,eta,zeta){
  return [[1-xsi-eta-zeta,-1,-1,-1],[xsi,1,0,0],[eta,0,1,0],
      	  [zeta,0,0,1]];
};

// ヤコビアンを返す
// p - 要素節点
TetraElement1.prototype.jacobian=function(p){
  var p0x=p[0].x,p0y=p[0].y,p0z=p[0].z;
  var j11=(p[2].y-p0y)*(p[3].z-p0z)-(p[3].y-p0y)*(p[2].z-p0z);
  var j21=(p[3].y-p0y)*(p[1].z-p0z)-(p[1].y-p0y)*(p[3].z-p0z);
  var j31=(p[1].y-p0y)*(p[2].z-p0z)-(p[2].y-p0y)*(p[1].z-p0z);
  return (p[1].x-p0x)*j11+(p[2].x-p0x)*j21+(p[3].x-p0x)*j31;
};

// 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
// p - 要素節点
// ja - ヤコビアン
TetraElement1.prototype.grad=function(p,ja){
  var count=this.nodeCount(),gr=[],ji=1/ja;
  for(var i=0;i<count;i++){
    ji=-ji;
    var i2=(i+1)%4,i3=(i+2)%4,i4=(i+3)%4;
    var p2x=p[i2].x,p2y=p[i2].y,p2z=p[i2].z;
    gr[i]=[ji*((p[i3].y-p2y)*(p[i4].z-p2z)-
      	       (p[i4].y-p2y)*(p[i3].z-p2z)),
      	   ji*((p[i3].z-p2z)*(p[i4].x-p2x)-
      	       (p[i4].z-p2z)*(p[i3].x-p2x)),
      	   ji*((p[i3].x-p2x)*(p[i4].y-p2y)-
      	       (p[i4].x-p2x)*(p[i3].y-p2y))];
  }
  return gr;
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
TetraElement1.prototype.massMatrix=function(p,dens){
  var m=numeric.rep([12,12],0);
  var value=dens*this.jacobian(p)/60,vh=0.5*value;
  for(var i=0;i<12;i+=3){
    m[i][i]=value;
    m[i+1][i+1]=value;
    m[i+2][i+2]=value;
    for(var j=i+3;j<12;j+=3){
      m[i][j]=vh;
      m[i+1][j+1]=vh;
      m[i+2][j+2]=vh;
      m[j][i]=vh;
      m[j+1][i+1]=vh;
      m[j+2][i+2]=vh;
    }
  }
  return m;
};

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.stiffnessMatrix=function(p,d1){
  var ja=this.jacobian(p);
  return this.stiffPart(d1,this.strainMatrix(this.grad(p,ja)),
      	      	      	C1_6*Math.abs(ja));
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
TetraElement1.prototype.shapeFunctionMatrix=function(p,coef){
  var value=0.1*C1_6*coef*Math.abs(this.jacobian(p)),vh=0.5*value;
  return [[value,vh,vh,vh],[vh,value,vh,vh],[vh,vh,value,vh],
      	  [vh,vh,vh,value]];
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
TetraElement1.prototype.gradMatrix=function(p,coef){
  var g=[],ja=this.jacobian(p),gr=this.grad(p,ja);
  var coef2=C1_6*coef*Math.abs(ja);
  for(var i=0;i<4;i++){
    g[i]=[];
    for(var j=0;j<4;j++){
      g[i][j]=coef2*(gr[i][0]*gr[j][0]+gr[i][1]*gr[j][1]+
      	      	     gr[i][2]*gr[j][2]);
    }
  }
  return g;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.geomStiffnessMatrix=function(p,u,d1){
  var count=this.nodeCount(),kk=numeric.rep([3*count,3*count],0);
  var ja=this.jacobianMatrix(p);
  var gr=this.grad(p,ja);
  var sm=this.strainMatrix(gr);
  var str=numeric.dotMV(d1,numeric.dotVM(this.toArray(u,3),sm));
  var w=C1_6*Math.abs(ja);
  for(var i1=0;i1<count;i1++){
    var i3=3*i1,gri=gr[i1];
    for(var j1=0;j1<count;j1++){
      var j3=3*j1,grj=gr[j1];
      var s=w*(gri[0]*(str[0]*grj[0]+str[3]*grj[1]+str[5]*grj[2])+
      	       gri[1]*(str[3]*grj[0]+str[1]*grj[1]+str[4]*grj[2])+
      	       gri[2]*(str[5]*grj[0]+str[4]*grj[1]+str[2]*grj[2]));
      kk[i3][j3]=s;
      kk[i3+1][j3+1]=s;
      kk[i3+2][j3+2]=s;
    }
  }
  return kk;
};

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.strainStress=function(p,u,d1){
  var sm=this.strainMatrix(this.grad(p,this.jacobian(p)));
  var eps=numeric.dotVM(this.toArray(u,3),sm);
  var strain=new Strain(eps);
  var str=numeric.dotMV(d1,eps);
  var stress=new Stress(str);
  var energy=0.5*strain.innerProduct(stress);
  return [[strain,strain,strain,strain],[stress,stress,stress,stress],
      	  [energy,energy,energy,energy]];
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.elementStrainStress=function(p,u,d1){
  var sm=this.strainMatrix(this.grad(p,this.jacobian(p)));
  var eps=numeric.dotVM(this.toArray(u,3),sm);
  var str=numeric.dotMV(d1,eps);
  var energy=0.5*numeric.dotVV(eps,str);
  return [new Strain(eps),new Stress(str),energy];
};

//--------------------------------------------------------------------//
// 四面体2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var TetraElement2=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,TETRA2_NODE,TETRA2_INT);
};

// 要素名称を返す
TetraElement2.prototype.getName=function(){
  return 'TetraElement2';
};

// 節点数を返す
TetraElement2.prototype.nodeCount=function(){
  return 10;
};

// 要素境界数を返す
TetraElement2.prototype.borderCount=function(){
  return 4;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
TetraElement2.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new TriangleBorder2
      	(element,[p[0],p[2],p[1],p[6],p[5],p[4]]);
    case 1:
      return new TriangleBorder2
      	(element,[p[0],p[1],p[3],p[4],p[8],p[7]]);
    case 2:
      return new TriangleBorder2
      	(element,[p[1],p[2],p[3],p[5],p[9],p[8]]);
    case 3:
      return new TriangleBorder2
      	(element,[p[2],p[0],p[3],p[6],p[7],p[9]]);
  }
};

// 要素を鏡像反転する
TetraElement2.prototype.mirror=function(){
  swap(this.nodes,1,2);
  swap(this.nodes,4,6);
  swap(this.nodes,8,9);
};

// 要素節点の角度を返す
// p - 要素節点
TetraElement2.prototype.angle=function(p){
  return [solidAngle(p[0],p[4],p[6],p[7]),
      	  solidAngle(p[1],p[5],p[4],p[8]),
      	  solidAngle(p[2],p[6],p[5],p[9]),
      	  solidAngle(p[3],p[7],p[8],p[9]),
      	  planeAngle(p[4],p[2],p[3]),planeAngle(p[5],p[0],p[3]),
      	  planeAngle(p[6],p[1],p[3]),planeAngle(p[7],p[1],p[2]),
      	  planeAngle(p[8],p[2],p[0]),planeAngle(p[9],p[0],p[1])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
TetraElement2.prototype.shapeFunction=function(xsi,eta,zeta){
  var xez=1-xsi-eta-zeta;
  return [[xez*(2*xez-1),1-4*xez,1-4*xez,1-4*xez],
      	  [xsi*(2*xsi-1),4*xsi-1,0,0],
      	  [eta*(2*eta-1),0,4*eta-1,0],
      	  [zeta*(2*zeta-1),0,0,4*zeta-1],
      	  [4*xez*xsi,4*(xez-xsi),-4*xsi,-4*xsi],
      	  [4*xsi*eta,4*eta,4*xsi,0],
      	  [4*xez*eta,-4*eta,4*(xez-eta),-4*eta],
      	  [4*xez*zeta,-4*zeta,-4*zeta,4*(xez-zeta)],
      	  [4*xsi*zeta,4*zeta,0,4*xsi],
      	  [4*eta*zeta,0,4*zeta,4*eta]];
};

//--------------------------------------------------------------------//
// 楔形1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var WedgeElement1=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,WEDGE1_NODE,WEDGE1_INT);
};

// 要素名称を返す
WedgeElement1.prototype.getName=function(){
  return 'WedgeElement1';
};

// 節点数を返す
WedgeElement1.prototype.nodeCount=function(){
  return 6;
};

// 要素境界数を返す
WedgeElement1.prototype.borderCount=function(){
  return 5;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
WedgeElement1.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new TriangleBorder1(element,[p[0],p[2],p[1]]);
    case 1:
      return new TriangleBorder1(element,[p[3],p[4],p[5]]);
    case 2:
      return new QuadangleBorder1(element,[p[0],p[1],p[4],p[3]]);
    case 3:
      return new QuadangleBorder1(element,[p[1],p[2],p[5],p[4]]);
    case 4:
      return new QuadangleBorder1(element,[p[2],p[0],p[3],p[5]]);
  }
};

// 要素を鏡像反転する
WedgeElement1.prototype.mirror=function(){
  swap(this.nodes,1,2);
  swap(this.nodes,4,5);
};

// 要素節点の角度を返す
// p - 要素節点
WedgeElement1.prototype.angle=function(p){
  var th=[];
  for(var i=0;i<3;i++){
    th[i]=solidAngle(p[i],p[(i+1)%3],p[(i+2)%3],p[i+3]);
    th[i+3]=solidAngle(p[i+3],p[(i+1)%3+3],p[(i+2)%3+3],p[i]);
  }
  return th;
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
WedgeElement1.prototype.shapeFunction=function(xsi,eta,zeta){
  return [[0.5*(1-xsi-eta)*(1-zeta),-0.5*(1-zeta),-0.5*(1-zeta),
      	   -0.5*(1-xsi-eta)],
      	  [0.5*xsi*(1-zeta),0.5*(1-zeta),0,-0.5*xsi],
      	  [0.5*eta*(1-zeta),0,0.5*(1-zeta),-0.5*eta],
      	  [0.5*(1-xsi-eta)*(1+zeta),-0.5*(1+zeta),-0.5*(1+zeta),
      	   0.5*(1-xsi-eta)],
      	  [0.5*xsi*(1+zeta),0.5*(1+zeta),0,0.5*xsi],
      	  [0.5*eta*(1+zeta),0,0.5*(1+zeta),0.5*eta]];
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
WedgeElement1.prototype.massMatrix=function(p,dens){
  var ja=0,i;
  for(i=0;i<2;i++){
    var sf=this.shapeFunction(WEDGE1_INT[i][0],WEDGE1_INT[i][1],
      	      	      	      WEDGE1_INT[i][2]);
    ja+=this.jacobianMatrix(p,sf).determinant();
  }
  var v0=dens*ja/36,m=numeric.rep([18,18],0);
  for(i=0;i<6;i++){
    for(var j=0;j<6;j++){
      var value=v0,i3=3*i,j3=3*j;
      if(i%3!=j%3) value*=0.5;
      if(Math.floor(i/3)!=Math.floor(j/3)) value*=0.5;
      m[i3][j3]+=value;
      m[i3+1][j3+1]+=value;
      m[i3+2][j3+2]+=value;
    }
  }
  return m;
};

//--------------------------------------------------------------------//
// 楔形2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var WedgeElement2=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,WEDGE2_NODE,WEDGE2_INT);
};

// 要素名称を返す
WedgeElement2.prototype.getName=function(){
  return 'WedgeElement2';
};

// 節点数を返す
WedgeElement2.prototype.nodeCount=function(){
  return 15;
};

// 要素境界数を返す
WedgeElement2.prototype.borderCount=function(){
  return 5;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
WedgeElement2.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new TriangleBorder2
      	(element,[p[0],p[2],p[1],p[8],p[7],p[6]]);
    case 1:
      return new TriangleBorder2
      	(element,[p[3],p[4],p[5],p[9],p[10],p[11]]);
    case 2:
      return new QuadangleBorder2
      	(element,[p[0],p[1],p[4],p[3],p[6],p[13],p[9],p[12]]);
    case 3:
      return new QuadangleBorder2
      	(element,[p[1],p[2],p[5],p[4],p[7],p[14],p[10],p[13]]);
    case 4:
      return new QuadangleBorder2
      	(element,[p[2],p[0],p[3],p[5],p[8],p[12],p[11],p[14]]);
  }
};

// 要素を鏡像反転する
WedgeElement2.prototype.mirror=function(){
  swap(this.nodes,1,2);
  swap(this.nodes,4,5);
  swap(this.nodes,6,8);
  swap(this.nodes,9,11);
  swap(this.nodes,13,14);
};

// 要素節点の角度を返す
// p - 要素節点
WedgeElement2.prototype.angle=function(p){
  return [solidAngle(p[0],p[6],p[8],p[12]),
      	  solidAngle(p[1],p[6],p[7],p[14]),
      	  solidAngle(p[2],p[7],p[8],p[14]),
      	  solidAngle(p[3],p[9],p[11],p[12]),
      	  solidAngle(p[4],p[10],p[9],p[13]),
      	  solidAngle(p[5],p[11],p[10],p[14]),
      	  planeAngle(p[6],p[2],p[9]),planeAngle(p[7],p[0],p[10]),
      	  planeAngle(p[8],p[1],p[11]),planeAngle(p[9],p[6],p[5]),
      	  planeAngle(p[10],p[7],p[3]),planeAngle(p[11],p[8],p[4]),
      	  planeAngle(p[12],p[13],p[14]),planeAngle(p[13],p[14],p[12]),
      	  planeAngle(p[14],p[12],p[13])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
WedgeElement2.prototype.shapeFunction=function(xsi,eta,zeta){
  var xe=1-xsi-eta;
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
      	  [eta*(1+zeta)*(1-zeta),0,(1+zeta)*(1-zeta),-2*eta*zeta]];
};

//--------------------------------------------------------------------//
// 六面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var HexaElement1=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,HEXA1_NODE,HEXA1_INT);
};

// 要素名称を返す
HexaElement1.prototype.getName=function(){
  return 'HexaElement1';
};

// 節点数を返す
HexaElement1.prototype.nodeCount=function(){
  return 8;
};

// 要素境界数を返す
HexaElement1.prototype.borderCount=function(){
  return 6;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
HexaElement1.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new QuadangleBorder1(element,[p[0],p[3],p[2],p[1]]);
    case 1:
      return new QuadangleBorder1(element,[p[4],p[5],p[6],p[7]]);
    case 2:
      return new QuadangleBorder1(element,[p[0],p[1],p[5],p[4]]);
    case 3:
      return new QuadangleBorder1(element,[p[1],p[2],p[6],p[5]]);
    case 4:
      return new QuadangleBorder1(element,[p[2],p[3],p[7],p[6]]);
    case 5:
      return new QuadangleBorder1(element,[p[3],p[0],p[4],p[7]]);
  }
};

// 要素を鏡像反転する
HexaElement1.prototype.mirror=function(){
  swap(this.nodes,1,3);
  swap(this.nodes,5,7);
};

// 要素節点の角度を返す
// p - 要素節点
HexaElement1.prototype.angle=function(p){
  var th=[];
  for(var i=0;i<4;i++){
    th[i]=solidAngle(p[i],p[(i+1)%4],p[(i+3)%4],p[i+4]);
    th[i+4]=solidAngle(p[i+4],p[(i+1)%4+4],p[(i+3)%4+4],p[i]);
  }
  return th;
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
HexaElement1.prototype.shapeFunction=function(xsi,eta,zeta){
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
      	   0.125*(1-xsi)*(1+zeta),0.125*(1-xsi)*(1+eta)]];
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
HexaElement1.prototype.massMatrix=function(p,dens){
  var ja=0,i;
  for(i=0;i<8;i++){
    var sf=this.shapeFunction(HEXA1_INT[i][0],HEXA1_INT[i][1],
      	      	      	      HEXA1_INT[i][2]);
    ja+=Math.abs(this.jacobianMatrix(p,sf).determinant());
  }
  var coef=dens*ja,m=numeric.rep([24,24],0);
  for(i=0;i<8;i++){
    var i3=3*i;
    for(var j=0;j<8;j++){
      var value=coef*HEXA1_MASS_BASE[i][j],j3=3*j;
      m[i3][j3]+=value;
      m[i3+1][j3+1]+=value;
      m[i3+2][j3+2]+=value;
    }
  }
  return m;
};

//--------------------------------------------------------------------//
// 六面体セレンディピティ族2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var HexaElement2=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,HEXA2_NODE,HEXA2_INT);
};

// 要素名称を返す
HexaElement2.prototype.getName=function(){
  return 'HexaElement2';
};

// 節点数を返す
HexaElement2.prototype.nodeCount=function(){
  return 20;
};

// 要素境界数を返す
HexaElement2.prototype.borderCount=function(){
  return 6;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
HexaElement2.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new QuadangleBorder2
      	(element,[p[0],p[3],p[2],p[1],p[11],p[10],p[9],p[8]]);
    case 1:
      return new QuadangleBorder2
      	(element,[p[4],p[5],p[6],p[7],p[12],p[13],p[14],p[15]]);
    case 2:
      return new QuadangleBorder2
      	(element,[p[0],p[1],p[5],p[4],p[8],p[17],p[12],p[16]]);
    case 3:
      return new QuadangleBorder2
      	(element,[p[1],p[2],p[6],p[5],p[9],p[18],p[13],p[17]]);
    case 4:
      return new QuadangleBorder2
      	(element,[p[2],p[3],p[7],p[6],p[10],p[19],p[14],p[18]]);
    case 5:
      return new QuadangleBorder2
      	(element,[p[3],p[0],p[4],p[7],p[11],p[16],p[15],p[19]]);
  }
};

// 要素を鏡像反転する
HexaElement2.prototype.mirror=function(){
  swap(this.nodes,1,3);
  swap(this.nodes,5,7);
  swap(this.nodes,8,11);
  swap(this.nodes,9,10);
  swap(this.nodes,12,15);
  swap(this.nodes,13,14);
  swap(this.nodes,17,19);
};

// 要素節点の角度を返す
// p - 要素節点
HexaElement2.prototype.angle=function(p){
  return [solidAngle(p[0],p[8],p[11],p[16]),
      	  solidAngle(p[1],p[9],p[8],p[17]),
      	  solidAngle(p[2],p[10],p[9],p[18]),
      	  solidAngle(p[3],p[11],p[10],p[19]),
      	  solidAngle(p[4],p[12],p[15],p[16]),
      	  solidAngle(p[5],p[13],p[12],p[17]),
      	  solidAngle(p[6],p[14],p[13],p[18]),
      	  solidAngle(p[7],p[15],p[14],p[19]),
      	  planeAngle(p[8],p[12],p[10]),planeAngle(p[9],p[13],p[11]),
      	  planeAngle(p[10],p[14],p[8]),planeAngle(p[11],p[15],p[9]),
      	  planeAngle(p[12],p[8],p[14]),planeAngle(p[13],p[9],p[15]),
      	  planeAngle(p[14],p[10],p[12]),planeAngle(p[15],p[11],p[13]),
      	  planeAngle(p[16],p[17],p[19]),planeAngle(p[17],p[18],p[16]),
      	  planeAngle(p[18],p[19],p[17]),planeAngle(p[19],p[16],p[18])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
HexaElement2.prototype.shapeFunction=function(xsi,eta,zeta){
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
      	   -0.5*(1-xsi)*(1+eta)*zeta]];
};

inherits(SolidElement,FElement);
inherits(TetraElement1,SolidElement);
inherits(TetraElement2,SolidElement);
inherits(WedgeElement1,SolidElement);
inherits(WedgeElement2,SolidElement);
inherits(HexaElement1,SolidElement);
inherits(HexaElement2,SolidElement);
