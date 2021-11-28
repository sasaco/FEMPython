var CIRCLE_DIV=16;			// 円形断面表示オブジェクトの分割数
var CIRCLE_DTH=2*Math.PI/CIRCLE_DIV;	// 円形断面の１分割の角度
var COEF_K1=64/Math.pow(Math.PI,5);	// 矩形断面の捩り係数
var COEF_K=8/(Math.PI*Math.PI);

//--------------------------------------------------------------------//
// 円形断面
// ss - データ文字列
var CircleSection=function(ss){
  this.d1=parseFloat(ss[0]);	// 外径
  this.d2=parseFloat(ss[1]);	// 内径
// 断面積
  this.area=0.25*Math.PI*(this.d1*this.d1-this.d2*this.d2);
// 断面２次モーメント
  this.iy=0.0625*this.area*(this.d1*this.d1+this.d2*this.d2);
  this.iz=this.iy;
  this.ip=this.iy+this.iz;	// 断面２次極モーメント
};

// 断面の頂点数を返す
CircleSection.prototype.vertexCount=function(){
  return CIRCLE_DIV;
};

// せん断補正係数を返す
CircleSection.prototype.shearCoef=function(){
  return KS_CIRCLE;
};

// 歪・応力ベクトルを返す
// material - 材料
// ex - 引張圧縮歪
// thd - 比捩り角
// kpy,kpz - 曲げによる曲率
// sy,sz - 断面せん断歪
CircleSection.prototype.strainStress=function(material,ex,thd,kpy,kpz,
      	      	      	      	      	      sy,sz){
  var kp=Math.sqrt(kpy*kpy+kpz*kpz),kpr=0.5*kp*this.d1;
  var gx=0.5*this.d1*thd,gy=0,gz=gx;
  if(kp>0){
    gy=-gx*kpz/kp;
    gz=gx*kpy/kp;
  }
  var ee=material.ee,gg=material.gg;
  return [[ex+kpr,0,0,gy+sy,0,gz+sz],
      	  [ee*(ex+kpr),0,0,gg*(gy+sy),0,gg*(gz+sz)],
      	  [ex-kpr,0,0,-gy+sy,0,-gz+sz],
      	  [ee*(ex-kpr),0,0,gg*(-gy+sy),0,gg*(-gz+sz)]];
};

// 断面表示モデルの座標系を設定する
// pos1,pos2 - 外径，内径の座標
// cx,cy,cz - 中心点座標
// v1,v2 - 軸方向，断面基準方向ベクトル
CircleSection.prototype.setCoords=function(pos1,pos2,cx,cy,cz,v1,v2){
  var r1=0.5*this.d1,r2=0.5*this.d2;
  for(var j=0;j<pos1.length;j+=3){
    pos1[j]=cx+r1*v2.x;
    pos1[j+1]=cy+r1*v2.y;
    pos1[j+2]=cz+r1*v2.z;
    pos2[j]=cx+r2*v2.x;
    pos2[j+1]=cy+r2*v2.y;
    pos2[j+2]=cz+r2*v2.z;
    v2.applyAxisAngle(v1,CIRCLE_DTH);
  }
};

// 質量・重心周りの慣性モーメントを返す
// dens - 密度
// l - 要素長さ
CircleSection.prototype.massInertia=function(dens,l){
  var dl=dens*l,dly=dl*this.iy;
  return [dl*this.area,2*dly,dly,dly];
};

// 断面を表す文字列を返す
CircleSection.prototype.toString=function(){
  return this.d1+'\t'+this.d2;
};

//--------------------------------------------------------------------//
// 矩形断面
// ss - データ文字列
var RectSection=function(ss){
  var b1=parseFloat(ss[0]),h1=parseFloat(ss[1]);
  var b2=parseFloat(ss[2]),h2=parseFloat(ss[3]);
  this.b1=b1;	// 外側幅
  this.h1=h1;	// 外側高さ
  this.b2=b2;	// 内側幅
  this.h2=h2;	// 内側高さ
// 断面積
  this.area=b1*h1-b2*h2;
// 断面２次モーメント
  var i11=b1*b1*b1*h1,i12=b1*h1*h1*h1,i21=b2*b2*b2*h2,i22=b2*h2*h2*h2;
  this.iy=(i11-i21)/12;
  this.iz=(i12-i22)/12;
  var sw1=(b1>=h1),sw2=(b2>=h2),k1,k2,ip1,ip2;
  if(sw1){
    k1=rectCoef(b1/h1);
    ip1=k1[0]*i12;
    this.zy=k1[1]*h1;
    this.zz=k1[3]*this.zy;
  }
  else{
    k1=rectCoef(h1/b1);
    ip1=k1[0]*i11;
    this.zz=k1[1]*b1;
    this.zy=k1[3]*this.zz;
  }
  if(i22===0){
    ip2=0;
  }
  else if(sw2){
    k2=rectCoef(b2/h2);
    ip2=k2[0]*i22;
  }
  else{
    k2=rectCoef(h2/b2);
    ip2=k2[0]*i21;
  }
  this.ip=ip1-ip2;		// 断面２次極モーメント
};

// 断面の頂点数を返す
RectSection.prototype.vertexCount=function(){
  return 4;
};

// せん断補正係数を返す
RectSection.prototype.shearCoef=function(){
  return KS_RECT;
};

// 歪・応力ベクトルを返す
// material - 材料
// ex - 引張圧縮歪
// thd - 比捩り角
// kpy,kpz - 曲げによる曲率
// sy,sz - 断面せん断歪
RectSection.prototype.strainStress=function(material,ex,thd,kpy,kpz,
      	      	      	      	      	    sy,sz){
  var sby=0.5*kpy*this.b1,sbz=0.5*kpz*this.h1;
  var gy=this.zy*thd,gz=this.zz*thd;
  var ee=material.ee,gg=material.gg;
  var eps=[[ex+sby,sy,sz+gz],[ex+sby+sbz,sy,sz],[ex+sbz,sy-gy,sz],
      	   [ex-sby+sbz,sy,sz],[ex-sby,sy,sz-gz],[ex-sby-sbz,sy,sz],
      	   [ex-sbz,sy+gy,sz],[ex+sby-sbz,sy,sz]];
  var imax=0,enmax=0;
  for(var i=0;i<8;i++){
    var ei=eps[i],en=ee*ei[0]*ei[0]+gg*(ei[1]*ei[1]+ei[2]*ei[2]);
    if(en>enmax){
      imax=i;
      enmax=en;
    }
  }
  if(eps[imax][0]<0) imax=(imax+4)%8;
  var eps1=eps[imax],eps2=eps[(imax+4)%8];
  return [[eps1[0],0,0,eps1[1],0,eps1[2]],
      	  [ee*eps1[0],0,0,gg*eps1[1],0,gg*eps1[2]],
      	  [eps2[0],0,0,eps2[1],0,eps2[2]],
      	  [ee*eps2[0],0,0,gg*eps2[1],0,gg*eps2[2]]];
};

// 断面表示モデルの座標系を設定する
// pos1,pos2 - 外径，内径の座標
// cx,cy,cz - 中心点座標
// v1,v2 - 軸方向，断面基準方向ベクトル
RectSection.prototype.setCoords=function(pos1,pos2,cx,cy,cz,v1,v2){
  var v3=new THREE.Vector3().crossVectors(v1,v2);
  var c1=[0.5,-0.5,-0.5,0.5,0.5],c2=[0.5,0.5,-0.5,-0.5,0.5];
  for(var j=0;j<c1.length;j++){
    var j3=3*j;
    pos1[j3]=cx+c1[j]*this.b1*v2.x+c2[j]*this.h1*v3.x;
    pos1[j3+1]=cy+c1[j]*this.b1*v2.y+c2[j]*this.h1*v3.y;
    pos1[j3+2]=cz+c1[j]*this.b1*v2.z+c2[j]*this.h1*v3.z;
    pos2[j3]=cx+c1[j]*this.b2*v2.x+c2[j]*this.h2*v3.x;
    pos2[j3+1]=cy+c1[j]*this.b2*v2.y+c2[j]*this.h2*v3.y;
    pos2[j3+2]=cz+c1[j]*this.b2*v2.z+c2[j]*this.h2*v3.z;
  }
};

// 質量・重心周りの慣性モーメントを返す
// dens - 密度
// l - 要素長さ
RectSection.prototype.massInertia=function(dens,l){
  var dl=dens*l,dly=dl*this.iz,dlz=dl*this.iy;
  return [dl*this.area,dly+dlz,dly,dlz];
};

// 断面を表す文字列を返す
RectSection.prototype.toString=function(){
  return this.b1+'\t'+this.h1+'\t'+this.b2+'\t'+this.h2;
};

// 矩形断面の捩り係数を求める
// ba - 辺の長さ比b/a
function rectCoef(ba){
  var dk1s=0,dks=0,dbs=0,pba=0.5*Math.PI*ba;
  var i=1,dk1,dk,db,ex,sg=1;
  do{
    ex=Math.exp(-2*pba*i);
    dk1=(1-ex)/((i+ex)*Math.pow(i,5));	// IEは双曲線関数未実装
    dk1s+=dk1;
    i+=2;
  }while(dk1/dk1s>1e-10);
  i=1;
  do{
    dk=2/((Math.exp(pba*i)+Math.exp(-pba*i))*i*i);
    dks+=dk;
    i+=2;
  }while(dk/dks>1e-10);
  i=1;
  do{
    ex=Math.exp(-2*pba*i);
    db=sg*(1-ex)/((i+ex)*i*i);
    dbs+=db;
    i+=2;
    sg=-sg;
  }while(Math.abs(db/dbs)>1e-12);
  var k1=1/3-COEF_K1*dk1s/ba;
  var k=1-COEF_K*dks;
  var b=COEF_K*dbs;
  return [k1,k,k1/k,b/k];
}
