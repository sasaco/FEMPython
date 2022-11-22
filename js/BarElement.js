var I_YMZ=[1,5,7,11];	// y軸方向,z軸周り成分のインデックス
var I_ZMY=[2,4,8,10];	// z軸方向,y軸周り成分のインデックス

//--------------------------------------------------------------------//
// 梁要素
// label - 要素ラベル
// material - 材料のインデックス
// param - 梁パラメータのインデックス
// nodes - 節点番号
// axis - 断面基準方向ベクトル
var BarElement=function(label,material,param,nodes,axis){
  FElement.call(this,label,material,nodes);
  this.param=param;
  this.isBar=true;
  if((axis!==null) && (axis!==undefined)) axis.normalize();
  this.axis=axis;
};

// 節点数を返す
BarElement.prototype.nodeCount=function(){
  return 2;
};

// 要素境界数を返す
BarElement.prototype.borderCount=function(){
  return 1;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
BarElement.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new EdgeBorder1(element,[p[0],p[1]]);
  }
};

// 要素境界辺を返す 梁要素では要素境界と同じ
// element - 要素ラベル
// index - 要素境界辺のインデックス
BarElement.prototype.borderEdge=function(element,index){
  return this.border(element,index);
};

// 要素節点の角度を返す 梁要素では１
// p - 要素節点
BarElement.prototype.angle=function(p){
  return [1,1];
};

// 剛性マトリックスを返す
// p - 要素節点
// material - 材料
// sect - 梁断面パラメータ
BarElement.prototype.stiffnessMatrix=function(p,material,sect){
  var kk=numeric.rep([12,12],0),l=p[0].distanceTo(p[1]);
  var d=dirMatrix(p,this.axis);
  var kx=material.ee*sect.area/l,kt=material.gg*sect.ip/l;
  kk[0][0]=kx;
  kk[0][6]=-kx;
  kk[6][0]=-kx;
  kk[6][6]=kx;
  kk[3][3]=kt;
  kk[3][9]=-kt;
  kk[9][3]=-kt;
  kk[9][9]=kt;
  var km=this.stiffBend(l,material,sect);
  for(var i=0;i<4;i++){
    var kk1=kk[I_YMZ[i]],kk2=kk[I_ZMY[i]],kmi1=km[i],kmi2=km[i+4];
    for(var j=0;j<4;j++){
      kk1[I_YMZ[j]]=kmi1[j];
      kk2[I_ZMY[j]]=kmi2[j];
    }
  }
  toDir3(d,kk);
  return kk;
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
// sect - 梁断面パラメータ
BarElement.prototype.gradMatrix=function(p,coef,sect){
  var c=coef*sect.area/p[0].distanceTo(p[1]);
  return [[c,-c],[-c,c]];
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
BarElement.prototype.geomStiffnessMatrix=function(p,u,material,sect){
  var l2=p[0].distanceToSquared(p[1]),d=dirMatrix(p,this.axis);
  var v=this.toLocalArray(u,d),kk=numeric.rep([12,12],0);
  var sx=material.ee*sect.area*(v[6]-v[0])/l2;
  for(var i=0;i<3;i++){
    kk[i][i]=sx;
    kk[i+6][i]=-sx;
    kk[i][i+6]=-sx;
    kk[i+6][i+6]=sx;
  }
  toDir3(d,kk);
  return kk;
};

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
BarElement.prototype.strainStress=function(p,u,material,sect){
  var l=p[0].distanceTo(p[1]),d=dirMatrix(p,this.axis);
  var v=this.toLocalArray(u,d);
  var strain1=[],stress1=[],energy1=[],strain2=[],stress2=[],energy2=[];
  var ex=(v[6]-v[0])/l,thd=(v[9]-v[3])/l;
  var ks=this.bendCurveShare(v,l,material,sect);
  var kpy=ks[0],kpz=ks[1],sy=ks[2],sz=ks[3];
  for(var i=0;i<2;i++){
    var str=sect.strainStress(material,ex,thd,kpy[i],kpz[i],sy,sz);
    strain1[i]=new Strain(str[0]);
    stress1[i]=new Stress(str[1]);
    strain2[i]=new Strain(str[2]);
    stress2[i]=new Stress(str[3]);
    strain1[i].rotate(d);
    stress1[i].rotate(d);
    strain2[i].rotate(d);
    stress2[i].rotate(d);
    energy1[i]=0.5*strain1[i].innerProduct(stress1[i]);
    energy2[i]=0.5*strain2[i].innerProduct(stress2[i]);
  }
  return [strain1,stress1,energy1,strain2,stress2,energy2];
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
BarElement.prototype.elementStrainStress=function(p,u,material,sect){
  var l=p[0].distanceTo(p[1]),d=dirMatrix(p,this.axis);
  var v=this.toLocalArray(u,d),i;
  var str=[];
  var ex=(v[6]-v[0])/l,thd=(v[9]-v[3])/l;
  var ks=this.bendCurveShare(v,l,material,sect);
  var kpy=ks[0],kpz=ks[1],sy=ks[2],sz=ks[3];
  for(i=0;i<2;i++){
    str[i]=sect.strainStress(material,ex,thd,kpy[i],kpz[i],sy,sz);
  }
  var strain1=new Strain
    (numeric.mul(0.5,numeric.add(str[0][0],str[1][0])));
  var stress1=new Stress
    (numeric.mul(0.5,numeric.add(str[0][1],str[1][1])));
  var strain2=new Strain
    (numeric.mul(0.5,numeric.add(str[0][2],str[1][2])));
  var stress2=new Stress
    (numeric.mul(0.5,numeric.add(str[0][3],str[1][3])));
  strain1.rotate(d);
  stress1.rotate(d);
  strain2.rotate(d);
  stress2.rotate(d);
  var energy1=0.5*strain1.innerProduct(stress1);
  var energy2=0.5*strain2.innerProduct(stress2);
  return [strain1,stress1,energy1,strain2,stress2,energy2];
};

// 要素を表す文字列を返す
// materials - 材料
// params - 梁パラメータ
// p - 節点
BarElement.prototype.toString=function(materials,params,p){
  var s=this.getName()+'\t'+this.label.toString(10)+'\t'+
      	materials[this.material].label.toString(10)+'\t'+
      	params[this.param].label.toString(10);
  for(var i=0;i<this.nodes.length;i++){
    s+='\t'+p[this.nodes[i]].label.toString(10);
  }
  return s;
};

//--------------------------------------------------------------------//
// Bernoulli-Euler梁要素
// label - 要素ラベル
// material - 材料のインデックス
// param - 梁パラメータのインデックス
// nodes - 節点番号
// axis - 断面基準方向ベクトル
var BEBarElement=function(label,material,param,nodes,axis){
  BarElement.call(this,label,material,param,nodes,axis);
};

// 要素境界名称を返す
BEBarElement.prototype.getName=function(){
  return 'BEBarElement';
};

// 剛性マトリックスの梁曲げ成分を返す
// l - 要素長さ
// material - 材料
// sect - 梁断面パラメータ
BEBarElement.prototype.stiffBend=function(l,material,sect){
  var kc=material.ee/(l*l*l),kcy=kc*sect.iy,kcz=kc*sect.iz;
  var kcy12=12*kcy,kcy6l=6*kcy*l,kcyll=kcy*l*l;
  var kcz12=12*kcz,kcz6l=6*kcz*l,kczll=kcz*l*l;
  return [[kcy12,kcy6l,-kcy12,kcy6l],[kcy6l,4*kcyll,-kcy6l,2*kcyll],
      	  [-kcy12,-kcy6l,kcy12,-kcy6l],[kcy6l,2*kcyll,-kcy6l,4*kcyll],
      	  [kcz12,-kcz6l,-kcz12,-kcz6l],[-kcz6l,4*kczll,kcz6l,2*kczll],
      	  [-kcz12,kcz6l,kcz12,kcz6l],[-kcz6l,2*kczll,kcz6l,4*kczll]];
};


// 曲げ曲率・せん断歪を返す
// v - 局所座標系の変位
// l - 要素長さ
// material - 材料
// sect - 梁断面パラメータ
BEBarElement.prototype.bendCurveShare=function(v,l,material,sect){
  var ckap1=6/(l*l),ckap2=4/l,ckap3=0.5*ckap2;
  var kpy=[ckap1*(v[1]-v[7])+ckap2*v[5]+ckap3*v[11],
      	   ckap1*(v[1]-v[7])+ckap3*v[5]+ckap2*v[11]];
  var kpz=[-ckap1*(v[2]-v[8])+ckap2*v[4]+ckap3*v[10],
      	   -ckap1*(v[2]-v[8])+ckap3*v[4]+ckap2*v[10]];
  return [kpy,kpz,0,0];
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
// sect - 梁断面パラメータ
BEBarElement.prototype.massMatrix=function(p,dens,sect){
  var l=p[0].distanceTo(p[1]),d=dirMatrix(p,this.axis);
  var mi=sect.massInertia(dens,l),m0=mi[0],dm=C1_3*m0,dix=C1_3*mi[1];
  var m=numeric.rep([12,12],0);
  m[0][0]=dm;
  m[0][6]=0.5*dm;
  m[6][0]=0.5*dm;
  m[6][6]=dm;
  m[3][3]=dix;
  m[3][9]=0.5*dix;
  m[9][3]=0.5*dix;
  m[9][9]=dix;
  var m1=m0*13/35,m2=m0*11*l/210,m3=m0*9/70,m4=m0*13*l/420;
  var m5=m0*l*l/105,m6=m0*l*l/140;
  var mm1=[[m1,m2,m3,-m4],[m2,m5,m4,-m6],
      	   [m3,m4,m1,-m2],[-m4,-m6,-m2,m5]];
  var mm2=[[m1,-m2,m3,m4],[-m2,m5,-m4,-m6],
      	   [m3,-m4,m1,m2],[m4,-m6,m2,m5]];
  for(var i=0;i<4;i++){
    var mi1=m[I_YMZ[i]],mi2=m[I_ZMY[i]],mmi1=mm1[i],mmi2=mm2[i];
    for(var j=0;j<4;j++){
      mi1[I_YMZ[j]]=mmi1[j];
      mi2[I_ZMY[j]]=mmi2[j];
    }
  }
  toDir3(d,m);
  return m;
};

//--------------------------------------------------------------------//
// Timoshenko梁要素
// label - 要素ラベル
// material - 材料のインデックス
// param - 梁パラメータのインデックス
// nodes - 節点番号
// axis - 断面基準方向ベクトル
var TBarElement=function(label,material,param,nodes,axis){
  BarElement.call(this,label,material,param,nodes,axis);
};

// 要素境界名称を返す
TBarElement.prototype.getName=function(){
  return 'TBarElement';
};

// 剛性マトリックスの梁曲げ成分を返す
// l - 要素長さ
// material - 材料
// sect - 梁断面パラメータ
TBarElement.prototype.stiffBend=function(l,material,sect){
  var kb=material.ee/l,kby=kb*sect.iy,kbz=kb*sect.iz;
  var ksc1=sect.shearCoef()*material.gg*sect.area/l;
  var ksc2y=12*kby/l,kscy=ksc1*ksc2y/(ksc1+ksc2y);	// MacNealの補正
  var ksc2z=12*kbz/l,kscz=ksc1*ksc2z/(ksc1+ksc2z);
  var lh=0.5*l,ksyl=kscy*lh,ksyl2=ksyl*lh,kszl=kscz*lh,kszl2=kszl*lh;
  return [[kscy,ksyl,-kscy,ksyl],[ksyl,kby+ksyl2,-ksyl,-kby+ksyl2],
      	  [-kscy,-ksyl,kscy,-ksyl],[ksyl,-kby+ksyl2,-ksyl,kby+ksyl2],
      	  [kscz,-kszl,-kscz,-kszl],[-kszl,kbz+kszl2,kszl,-kbz+kszl2],
      	  [-kscz,kszl,kscz,kszl],[-kszl,-kbz+kszl2,kszl,kbz+kszl2]];
};

// 曲げ曲率・せん断歪を返す
// v - 局所座標系の変位
// l - 要素長さ
// material - 材料
// sect - 梁断面パラメータ
TBarElement.prototype.bendCurveShare=function(v,l,material,sect){
  var cs1=sect.shearCoef()/l,cs2=0.5*sect.shearCoef();
  var ckap1=cs2*material.gg*sect.area*l/material.ee;
  var ckap1y=ckap1/sect.iy,ckap1z=ckap1/sect.iz;
  var ckap2y=ckap1y*0.5*l,ckap2z=ckap1z*0.5*l,ckap3=1/l;
  var sy=cs1*(v[7]-v[1])-cs2*(v[5]+v[11]);
  var sz=cs1*(v[8]-v[2])+cs2*(v[4]+v[10]);
  var kpy=[ckap1y*(v[1]-v[7])+(ckap2y+ckap3)*v[5]+
      	   (ckap2y-ckap3)*v[11],
      	   ckap1y*(v[1]-v[7])+(ckap2y-ckap3)*v[5]+
      	   (ckap2y+ckap3)*v[11]];
  var kpz=[-ckap1z*(v[2]-v[8])+(ckap2z+ckap3)*v[4]+
      	   (ckap2z-ckap3)*v[10],
      	   -ckap1z*(v[2]-v[8])+(ckap2z-ckap3)*v[4]+
      	   (ckap2z+ckap3)*v[10]];
  return [kpy,kpz,sy,sz];
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
// sect - 梁断面パラメータ
TBarElement.prototype.massMatrix=function(p,dens,sect){
  var l=p[0].distanceTo(p[1]),d=dirMatrix(p,this.axis);
  var mi=sect.massInertia(dens,l),dm=C1_3*mi[0];
  var di=[C1_3*mi[1],C1_3*mi[2],C1_3*mi[3]];
  var m=numeric.diag([dm,dm,dm,di[0],di[1],di[2],
      	      	      dm,dm,dm,di[0],di[1],di[2]]);
  for(var i=0;i<3;i++){
    m[i][i+6]=0.5*dm;
    m[i+6][i]=0.5*dm;
    m[i+3][i+9]=0.5*di[i];
    m[i+9][i+3]=0.5*di[i];
  }
  toDir3(d,m);
  return m;
};

inherits(BarElement,FElement);
inherits(BEBarElement,BarElement);
inherits(TBarElement,BarElement);
