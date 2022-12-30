﻿//--------------------------------------------------------------------//
// 境界条件
var BoundaryCondition=function(){
  this.restraints=[];		// 拘束条件
  this.loads=[];		// 荷重条件
  this.pressures=[];		// 面圧条件
  this.temperature=[];		// 節点温度条件
  this.htcs=[];			// 熱伝達境界条件
  this.loadMax=0;		// 最大荷重
  this.pressMax=0;		// 最大面圧
  this.dof=[];			// 節点の自由度
  this.nodeIndex=[];		// 荷重ベクトルの節点ポインタ
  this.bcList=[];		// 境界条件を設定した節点のリスト
};

// データを消去する
BoundaryCondition.prototype.clear=function(){
  this.restraints.length=0;
  this.loads.length=0;
  this.pressures.length=0;
  this.temperature.length=0;
  this.htcs.length=0;
  this.loadMax=0;
  this.pressMax=0;
};

// 境界条件を初期化する
BoundaryCondition.prototype.init=function(){
  this.restraints.sort(compareNodeLabel);
  this.loads.sort(compareNodeLabel);
  this.pressures.sort(compareElementLabel);
  this.temperature.sort(compareNodeLabel);
  this.htcs.sort(compareElementLabel);
  this.loadMax=0;
  this.pressMax=0;
  var i;
  for(i=0;i<this.loads.length;i++){
    this.loadMax=Math.max(this.loadMax,this.loads[i].magnitude());
  }
  for(i=0;i<this.pressures.length;i++){
    this.pressMax=Math.max(this.pressMax,this.pressures[i].press);
  }
};

// 構造解析の節点ポインタを設定する
// count - 節点数
BoundaryCondition.prototype.setPointerStructure=function(count){
  this.nodeIndex.length=0;
  this.bcList.length=0;
  var i,dofAll=0;
  for(i=0;i<count;i++){
    this.nodeIndex[i]=dofAll;
    dofAll+=this.dof[i];
  }
  for(i=0;i<dofAll;i++){
    this.bcList[i]=-1;
  }
  for(i=0;i<this.restraints.length;i++){
    var r=this.restraints[i];
    var index0=this.nodeIndex[r.node];
    var rdof=this.dof[r.node];
    for(var j=0;j<rdof;j++){
      if(r.rest[j]) this.bcList[index0+j]=6*i+j;
    }
  }
  return dofAll;
};

// 熱解析の節点ポインタを設定する
// count - 節点数
BoundaryCondition.prototype.setPointerHeat=function(count){
  this.dof.length=0;
  this.nodeIndex.length=0;
  this.bcList.length=0;
  var i,temps=this.temperature.length;
  for(i=0;i<count;i++){
    this.bcList[i]=-1;
  }
  for(i=0;i<temps;i++){
    var t=this.temperature[i];
    this.bcList[t.node]=i;
  }
  return temps;
};

// 強制変位を返す
// bc - 変位自由度ポインタ
BoundaryCondition.prototype.getRestDisp=function(bc){
  return this.restraints[parseInt(bc/6)].x[bc%6];
};

// データ文字列を返す
// nodes - 節点
// elems - 要素
BoundaryCondition.prototype.toStrings=function(nodes,elems){
  var s=[],i;
  for(i=0;i<this.restraints.length;i++){
    s.push(this.restraints[i].toString(nodes));
  }
  for(i=0;i<this.loads.length;i++){
    s.push(this.loads[i].toString(nodes));
  }
  for(i=0;i<this.pressures.length;i++){
    s.push(this.pressures[i].toString(elems));
  }
  for(i=0;i<this.temperature.length;i++){
    s.push(this.temperature[i].toString(nodes));
  }
  for(i=0;i<this.htcs.length;i++){
    s.push(this.htcs[i].toString(elems));
  }
  return s;
};

// 節点ラベルを比較する
// bc1,bc2 - 比較する境界条件
function compareNodeLabel(bc1,bc2){
  if(bc1.node<bc2.node)      return -1;
  else if(bc1.node>bc2.node) return 1;
  else                       return 0;
}

// 要素ラベルを比較する
// bc1,bc2 - 比較する境界条件
function compareElementLabel(bc1,bc2){
  if(bc1.element<bc2.element)      return -1;
  else if(bc1.element>bc2.element) return 1;
  else                             return 0;
}

//--------------------------------------------------------------------//
// ３次元ベクトル（並進＋回転）
// x,y,z - x,y,z成分
// rx,ry,rz - x,y,z軸周り回転角
var Vector3R=function(x,y,z,rx,ry,rz){
  this.x=[x||0,y||0,z||0,rx||0,ry||0,rz||0];
};

// 並進成分の大きさを返す
Vector3R.prototype.magnitude=function(){
  return Math.sqrt(this.magnitudeSq());
};

// 並進成分の大きさの2乗を返す
Vector3R.prototype.magnitudeSq=function(){
  return this.x[0]*this.x[0]+this.x[1]*this.x[1]+this.x[2]*this.x[2];
};

// 回転成分の大きさを返す
Vector3R.prototype.magnitudeR=function(){
  return Math.sqrt(this.magnitudeSqR());
};

// 回転成分の大きさの2乗を返す
Vector3R.prototype.magnitudeSqR=function(){
  return this.x[3]*this.x[3]+this.x[4]*this.x[4]+this.x[5]*this.x[5];
};

// ベクトルのコピーを返す
Vector3R.prototype.clone=function(){
  return new this.constructor(this.x[0],this.x[1],this.x[2],
      	      	      	      this.x[3],this.x[4],this.x[5]);
};

//--------------------------------------------------------------------//
// 要素境界条件
// element - 要素ラベル
// face - 要素境界面
var ElementBorderBound=function(element,face){
  this.element=element;
  this.face=face;
};

// 要素境界を返す
// elem - 要素
ElementBorderBound.prototype.getBorder=function(elem){
  if(this.face.length===2){
    var j;
    if(this.face.charAt(0)==='F'){
      j=parseInt(this.face.charAt(1))-1;
      return elem.border(this.element,j);
    }
    else if(this.face.charAt(0)==='E'){
      j=parseInt(this.face.charAt(1))-1;
      return elem.borderEdge(this.element,j);
    }
  }
  return null;
};

//--------------------------------------------------------------------//
// 拘束条件
// node - 節点ラベル
// coords - 局所座標系
// restx,resty,restz - x,y,z方向の拘束の有無
// x,y,z - 強制変位のx,y,z成分
// restrx,restry,restrz - x,y,z方向の回転拘束の有無
// rx,ry,rz - 強制変位のx,y,z軸周り回転角
var Restraint=function(node,coords,restx,resty,restz,x,y,z,
      	      	       restrx,restry,restrz,rx,ry,rz){
  Vector3R.call(this,x,y,z,rx,ry,rz);
  this.node=node;
  this.coords=coords;
  this.rest=[restx,resty,restz,restrx,restry,restrz];
  this.globalX=this.x;
};

// 拘束条件を表す文字列を返す
// nodes - 節点
Restraint.prototype.toString=function(nodes){
  var s='Restraint\t'+nodes[this.node].label.toString(10);
  for(var i=0;i<6;i++){
    if(this.rest[i]){
      s+='\t1\t'+this.x[i];
    }
    else{
      s+='\t0\t'+this.x[i];
    }
  }
  if(this.coords){
    s+='\t'+this.coords.label.toString(10);
  }
  return s;
};

//--------------------------------------------------------------------//
// 荷重条件
// node - 節点ラベル
// coords - 局所座標系
// x,y,z - x,y,z成分
// rx,ry,rz - x,y,z軸周り回転成分
var Load=function(node,coords,x,y,z,rx,ry,rz){
  Vector3R.call(this,x,y,z,rx,ry,rz);
  this.node=node;
  this.coords=coords;
  this.globalX=this.x;
};

// 荷重条件を表す文字列を返す
// nodes - 節点
Load.prototype.toString=function(nodes){
  var s='Load\t'+nodes[this.node].label.toString(10)+'\t'+
      	this.x.join('\t');
  if(this.coords){
    s+='\t'+this.coords.label.toString(10);
  }
  return s;
};

//--------------------------------------------------------------------//
// 面圧条件
// element - 要素ラベル
// face - 要素境界面
// press - 面圧
var Pressure=function(element,face,press){
  ElementBorderBound.call(this,element,face);
  this.press=press;
};

// 面圧条件を表す文字列を返す
// elems - 要素
Pressure.prototype.toString=function(elems){
  return 'Pressure\t'+elems[this.element].label.toString(10)+'\t'+
      	  this.face+'\t'+this.press;
};

//--------------------------------------------------------------------//
// 温度固定条件
// node - 節点ラベル
// t - 温度
var Temperature=function(node,t){
  this.node=node;
  this.t=t;
};

// 温度固定条件を表す文字列を返す
// nodes - 節点
Temperature.prototype.toString=function(nodes){
  return 'Temperature\t'+nodes[this.node].label.toString(10)+'\t'+
      	 this.t;
};

//--------------------------------------------------------------------//
// 熱伝達境界条件
// element - 要素ラベル
// face - 要素境界面
// htc - 熱伝達率
// outTemp - 外部温度
var HeatTransferBound=function(element,face,htc,outTemp){
  ElementBorderBound.call(this,element,face);
  this.htc=htc;
  this.outTemp=outTemp;
};

// 熱伝達境界条件を表す文字列を返す
// elems - 要素
HeatTransferBound.prototype.toString=function(elems){
  return 'HTC\t'+elems[this.element].label.toString(10)+'\t'+
      	  this.face+'\t'+this.htc+'\t'+this.outTemp;
};

inherits(Restraint,Vector3R);
inherits(Load,Vector3R);
inherits(Pressure,ElementBorderBound);
inherits(HeatTransferBound,ElementBorderBound);
