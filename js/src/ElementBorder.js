// 四角形2次要素の積分点のξ,η座標,重み係数
var QUAD2_INT=[[GX3[0],GX3[0],GW3[0]*GW3[0]],
      	       [GX3[1],GX3[0],GW3[1]*GW3[0]],
      	       [GX3[2],GX3[0],GW3[2]*GW3[0]],
      	       [GX3[0],GX3[1],GW3[0]*GW3[1]],
      	       [GX3[1],GX3[1],GW3[1]*GW3[1]],
      	       [GX3[2],GX3[1],GW3[2]*GW3[1]],
      	       [GX3[0],GX3[2],GW3[0]*GW3[2]],
      	       [GX3[1],GX3[2],GW3[1]*GW3[2]],
      	       [GX3[2],GX3[2],GW3[2]*GW3[2]]];

//--------------------------------------------------------------------//
// 要素境界
// element - 要素ラベル
// nodes - 節点番号
// intP - 積分点のξ,η座標,重み係数
var ElementBorder=function(element,nodes,intP){
  Nodes.call(this,nodes);
  this.element=element;
  this.intP=intP;
  this.isEdge=false;		// 辺ではない
};

// 周回順に並んだ節点ラベルを返す
ElementBorder.prototype.cycleNodes=function(){
  return this.nodes;
};

// 要素境界のコピーを返す
ElementBorder.prototype.clone=function(){
  return new this.constructor(this.element,this.nodes.concat());
};

// 要素境界を比較する
// b - 比較対象の要素境界
ElementBorder.prototype.compare=function(b){
  var p1=this.nodes.concat(),p2=b.nodes.concat();
  p1.sort(function(a,b){return a-b;});
  p2.sort(function(a,b){return a-b;});
  var len=Math.min(p1.length,p2.length);
  for(var i=0;i<len;i++){
    var d=p1[i]-p2[i];
    if(d!==0) return d;
  }
  return p1.length-p2.length;
};

// 擬似ヤコビアンを返す
// p - 節点
// sf - 形状関数行列
ElementBorder.prototype.jacobian=function(p,sf){
  var count=this.nodeCount(),jac=[0,0,0,0,0,0];
  for(var i=0;i<count;i++){
    for(var j=0;j<2;j++){
      jac[j]+=sf[i][j+1]*p[i].x;
      jac[j+2]+=sf[i][j+1]*p[i].y;
      jac[j+4]+=sf[i][j+1]*p[i].z;
    }
  }
  var j1=jac[2]*jac[5]-jac[4]*jac[3];
  var j2=jac[4]*jac[1]-jac[0]*jac[5];
  var j3=jac[0]*jac[3]-jac[2]*jac[1];
  return Math.sqrt(j1*j1+j2*j2+j3*j3);
};

// 積分点の形状関数マトリックス [ NiNj ] を返す
// p - 節点
// x - ξ,η座標
// w - 重み係数
ElementBorder.prototype.shapeMatrixPart=function(p,x,w){
  var sf=this.shapeFunction(x[0],x[1]);
  var coef=w*this.jacobian(p,sf);
  var count=this.nodeCount(),matrix=[];
  for(var i=0;i<count;i++){
    matrix[i]=[];
    var cf2=coef*sf[i][0];
    for(var j=0;j<count;j++){
      matrix[i][j]=cf2*sf[j][0];
    }
  }
  return matrix;
};

// 積分点の形状関数ベクトル [ Ni ] を返す
// p - 節点
// x - ξ,η座標
// w - 重み係数
ElementBorder.prototype.shapeVectorPart=function(p,x,w){
  var sf=this.shapeFunction(x[0],x[1]);
  var coef=w*this.jacobian(p,sf);
  var count=this.nodeCount(),vector=[];
  for(var i=0;i<count;i++){
    vector[i]=coef*sf[i][0];
  }
  return vector;
};

// 形状関数マトリックス [ ∫NiNjdS ] を返す
// p - 節点
// coef - 係数
ElementBorder.prototype.shapeFunctionMatrix=function(p,coef){
  var count=this.nodeCount(),s=numeric.rep([count,count],0);
  for(var i=0;i<this.intP.length;i++){
    addMatrix(s,this.shapeMatrixPart(p,this.intP[i],
      	      	      	      	     coef*this.intP[i][2]));
  }
  return s;
};

// 形状関数ベクトル [ ∫NidS ] を返す
// p - 節点
// coef - 係数
ElementBorder.prototype.shapeFunctionVector=function(p,coef){
  var count=this.nodeCount(),s=numeric.rep([count],0);
  for(var i=0;i<this.intP.length;i++){
    addVector(s,this.shapeVectorPart(p,this.intP[i],
      	      	      	      	     coef*this.intP[i][2]));
  }
  return s;
};

//--------------------------------------------------------------------//
// 辺1次要素境界
// element - 要素ラベル
// nodes - 節点番号
var EdgeBorder1=function(element,nodes){
  ElementBorder.call(this,element,nodes,null);
  this.isEdge=true;
};

// 要素境界名称を返す
EdgeBorder1.prototype.getName=function(){
  return 'EdgeBorder1';
};

// 節点数を返す
EdgeBorder1.prototype.nodeCount=function(){
  return 2;
};

// 要素境界を分割する
EdgeBorder1.prototype.splitBorder=function(){
  return [this.clone()];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
EdgeBorder1.prototype.shapeFunction=function(xsi,eta){
  if((eta===null) || (eta===undefined)){
    return [[0.5*(1-xsi),-0.5],[0.5*(1+xsi),0.5,1]];
  }
  else{
    return [[0.5*(1-xsi),-0.5,0],[0.5*(1+xsi),0.5,0],[0,0,0]];
  }
};

// 形状関数マトリックス [ ∫NiNjdS ] を返す
// p - 節点
// coef - 係数
EdgeBorder1.prototype.shapeFunctionMatrix=function(p,coef){
  var value=C1_3*coef*p[0].distanceTo(p[1]),vh=0.5*value;
  return [[value,vh],[vh,value]];
};

// 形状関数ベクトル [ ∫NidS ] を返す
// p - 節点
// coef - 係数
EdgeBorder1.prototype.shapeFunctionVector=function(p,coef){
  var value=0.5*coef*p[0].distanceTo(p[1]);
  return [value,value];
};

// 辺の法線ベクトルを返す
// p - 節点
// ep - 要素の節点
EdgeBorder1.prototype.normalVector=function(p,ep){
  var ne=normalVector(ep);
  var dir=p[1].clone().sub(p[0]);
  return dir.cross(ne).normalize();
};

//--------------------------------------------------------------------//
// 三角形1次要素境界
// element - 要素ラベル
// nodes - 節点番号
var TriangleBorder1=function(element,nodes){
  ElementBorder.call(this,element,nodes,null);
};

// 要素境界名称を返す
TriangleBorder1.prototype.getName=function(){
  return 'TriangleBorder1';
};

// 節点数を返す
TriangleBorder1.prototype.nodeCount=function(){
  return 3;
};

// 要素境界を分割する
TriangleBorder1.prototype.splitBorder=function(){
  return [this.clone()];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
TriangleBorder1.prototype.shapeFunction=function(xsi,eta){
  return [[1-xsi-eta,-1,-1],[xsi,1,0],[eta,0,1]];
};

// 擬似ヤコビアンを返す
// p - 節点
TriangleBorder1.prototype.jacobian=function(p){
  var p0x=p[0].x,p0y=p[0].y,p0z=p[0].z;
  var j1=(p[1].y-p0y)*(p[2].z-p0z)-(p[1].z-p0z)*(p[2].y-p0y);
  var j2=(p[1].z-p0z)*(p[2].x-p0x)-(p[1].x-p0x)*(p[2].z-p0z);
  var j3=(p[1].x-p0x)*(p[2].y-p0y)-(p[1].y-p0y)*(p[2].x-p0x);
  return Math.sqrt(j1*j1+j2*j2+j3*j3);
};

// 形状関数マトリックス [ ∫NiNjdS ] を返す
// p - 節点
// coef - 係数
TriangleBorder1.prototype.shapeFunctionMatrix=function(p,coef){
  var value=coef*this.jacobian(p)/12,vh=0.5*value;
  var count=this.nodeCount(),s=numeric.rep([count,count],vh);
  for(var i=0;i<count;i++){
    s[i][i]=value;
  }
  return s;
};

// 形状関数ベクトル [ ∫NidS ] を返す
// p - 節点
// coef - 係数
TriangleBorder1.prototype.shapeFunctionVector=function(p,coef){
  return numeric.rep([this.nodeCount()],C1_6*coef*this.jacobian(p));
};

//--------------------------------------------------------------------//
// 三角形2次要素境界
// element - 要素ラベル
// nodes - 節点番号
var TriangleBorder2=function(element,nodes){
  ElementBorder.call(this,element,nodes,TRI2_INT);
};

// 要素境界名称を返す
TriangleBorder2.prototype.getName=function(){
  return 'TriangleBorder2';
};

// 節点数を返す
TriangleBorder2.prototype.nodeCount=function(){
  return 6;
};

// 周回順に並んだ節点ラベルを返す
TriangleBorder2.prototype.cycleNodes=function(){
  return [this.nodes[0],this.nodes[3],this.nodes[1],this.nodes[4],
      	  this.nodes[2],this.nodes[5]];
};

// 要素境界を分割する
TriangleBorder2.prototype.splitBorder=function(){
  var p=this.nodes;
  return [new TriangleBorder1(this.element,[p[0],p[3],p[5]]),
      	  new TriangleBorder1(this.element,[p[1],p[4],p[3]]),
      	  new TriangleBorder1(this.element,[p[2],p[5],p[4]]),
      	  new TriangleBorder1(this.element,[p[3],p[4],p[5]])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
TriangleBorder2.prototype.shapeFunction=function(xsi,eta){
  var xe=1-xsi-eta;
  return [[xe*(2*xe-1),1-4*xe,1-4*xe],[xsi*(2*xsi-1),4*xsi-1,0],
      	  [eta*(2*eta-1),0,4*eta-1],[4*xe*xsi,4*(xe-xsi),-4*xsi],
      	  [4*xsi*eta,4*eta,4*xsi],[4*xe*eta,-4*eta,4*(xe-eta)]];
};

//--------------------------------------------------------------------//
// 四角形1次要素境界
// element - 要素ラベル
// nodes - 節点番号
var QuadangleBorder1=function(element,nodes){
  ElementBorder.call(this,element,nodes,QUAD1_INT);
};

// 要素境界名称を返す
QuadangleBorder1.prototype.getName=function(){
  return 'QuadangleBorder1';
};

// 節点数を返す
QuadangleBorder1.prototype.nodeCount=function(){
  return 4;
};

// 要素境界を分割する
QuadangleBorder1.prototype.splitBorder=function(){
  var p=this.nodes;
  return [new TriangleBorder1(this.element,[p[0],p[1],p[2]]),
      	  new TriangleBorder1(this.element,[p[2],p[3],p[0]])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
QuadangleBorder1.prototype.shapeFunction=function(xsi,eta){
  return [[0.25*(1-xsi)*(1-eta),-0.25*(1-eta),-0.25*(1-xsi)],
      	  [0.25*(1+xsi)*(1-eta),0.25*(1-eta),-0.25*(1+xsi)],
      	  [0.25*(1+xsi)*(1+eta),0.25*(1+eta),0.25*(1+xsi)],
      	  [0.25*(1-xsi)*(1+eta),-0.25*(1+eta),0.25*(1-xsi)]];
};

//--------------------------------------------------------------------//
// 四角形2次要素境界
// element - 要素ラベル
// nodes - 節点番号
var QuadangleBorder2=function(element,nodes){
  ElementBorder.call(this,element,nodes,QUAD2_INT);
};

// 要素境界名称を返す
QuadangleBorder2.prototype.getName=function(){
  return 'QuadangleBorder2';
};

// 節点数を返す
QuadangleBorder2.prototype.nodeCount=function(){
  return 8;
};

// 周回順に並んだ節点ラベルを返す
QuadangleBorder2.prototype.cycleNodes=function(){
  return [this.nodes[0],this.nodes[4],this.nodes[1],this.nodes[5],
      	  this.nodes[2],this.nodes[6],this.nodes[3],this.nodes[7]];
};

// 要素境界を分割する
QuadangleBorder2.prototype.splitBorder=function(){
  var p=this.nodes;
  return [new TriangleBorder1(this.element,[p[0],p[4],p[7]]),
      	  new TriangleBorder1(this.element,[p[1],p[5],p[4]]),
      	  new TriangleBorder1(this.element,[p[2],p[6],p[5]]),
      	  new TriangleBorder1(this.element,[p[3],p[7],p[6]]),
      	  new TriangleBorder1(this.element,[p[4],p[5],p[6]]),
      	  new TriangleBorder1(this.element,[p[6],p[7],p[4]])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
QuadangleBorder2.prototype.shapeFunction=function(xsi,eta){
  return [[0.25*(1-xsi)*(1-eta)*(-xsi-eta-1),0.25*(1-eta)*(2*xsi+eta),
      	   0.25*(1-xsi)*(xsi+2*eta)],
      	  [0.25*(1+xsi)*(1-eta)*(xsi-eta-1),0.25*(1-eta)*(2*xsi-eta),
      	   0.25*(1+xsi)*(-xsi+2*eta)],
      	  [0.25*(1+xsi)*(1+eta)*(xsi+eta-1),0.25*(1+eta)*(2*xsi+eta),
      	   0.25*(1+xsi)*(xsi+2*eta)],
      	  [0.25*(1-xsi)*(1+eta)*(-xsi+eta-1),0.25*(1+eta)*(2*xsi-eta),
      	   0.25*(1-xsi)*(-xsi+2*eta)],
      	  [0.5*(1+xsi)*(1-xsi)*(1-eta),-xsi*(1-eta),
      	   -0.5*(1+xsi)*(1-xsi)],
      	  [0.5*(1+xsi)*(1+eta)*(1-eta),0.5*(1+eta)*(1-eta),
      	   -(1+xsi)*eta],
      	  [0.5*(1+xsi)*(1-xsi)*(1+eta),-xsi*(1+eta),
      	   0.5*(1+xsi)*(1-xsi)],
      	  [0.5*(1-xsi)*(1+eta)*(1-eta),-0.5*(1+eta)*(1-eta),
      	   -(1-xsi)*eta]];
};

inherits(ElementBorder,Nodes);
inherits(EdgeBorder1,ElementBorder);
inherits(TriangleBorder1,ElementBorder);
inherits(TriangleBorder2,ElementBorder);
inherits(QuadangleBorder1,ElementBorder);
inherits(QuadangleBorder2,ElementBorder);
