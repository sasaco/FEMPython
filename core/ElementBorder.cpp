// �l�p�`2���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
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
// �v�f���E
// element - �v�f���x��
// nodes - �ߓ_�ԍ�
// intP - �ϕ��_�̃�,�ō��W,�d�݌W��
var ElementBorder=function(element,nodes,intP){
  Nodes.call(this,nodes);
  this.element=element;
  this.intP=intP;
  this.isEdge=false;		// �ӂł͂Ȃ�
};

// ���񏇂ɕ��񂾐ߓ_���x����Ԃ�
ElementBorder.prototype.cycleNodes=function(){
  return this.nodes;
};

// �v�f���E�̃R�s�[��Ԃ�
ElementBorder.prototype.clone=function(){
  return new this.constructor(this.element,this.nodes.concat());
};

// �v�f���E���r����
// b - ��r�Ώۂ̗v�f���E
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

// �[�����R�r�A����Ԃ�
// p - �ߓ_
// sf - �`��֐��s��
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

// �ϕ��_�̌`��֐��}�g���b�N�X [ NiNj ] ��Ԃ�
// p - �ߓ_
// x - ��,�ō��W
// w - �d�݌W��
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

// �ϕ��_�̌`��֐��x�N�g�� [ Ni ] ��Ԃ�
// p - �ߓ_
// x - ��,�ō��W
// w - �d�݌W��
ElementBorder.prototype.shapeVectorPart=function(p,x,w){
  var sf=this.shapeFunction(x[0],x[1]);
  var coef=w*this.jacobian(p,sf);
  var count=this.nodeCount(),vector=[];
  for(var i=0;i<count;i++){
    vector[i]=coef*sf[i][0];
  }
  return vector;
};

// �`��֐��}�g���b�N�X [ ��NiNjdS ] ��Ԃ�
// p - �ߓ_
// coef - �W��
ElementBorder.prototype.shapeFunctionMatrix=function(p,coef){
  var count=this.nodeCount(),s=numeric.rep([count,count],0);
  for(var i=0;i<this.intP.length;i++){
    addMatrix(s,this.shapeMatrixPart(p,this.intP[i],
      	      	      	      	     coef*this.intP[i][2]));
  }
  return s;
};

// �`��֐��x�N�g�� [ ��NidS ] ��Ԃ�
// p - �ߓ_
// coef - �W��
ElementBorder.prototype.shapeFunctionVector=function(p,coef){
  var count=this.nodeCount(),s=numeric.rep([count],0);
  for(var i=0;i<this.intP.length;i++){
    addVector(s,this.shapeVectorPart(p,this.intP[i],
      	      	      	      	     coef*this.intP[i][2]));
  }
  return s;
};

//--------------------------------------------------------------------//
// ��1���v�f���E
// element - �v�f���x��
// nodes - �ߓ_�ԍ�
var EdgeBorder1=function(element,nodes){
  ElementBorder.call(this,element,nodes,null);
  this.isEdge=true;
};

// �v�f���E���̂�Ԃ�
EdgeBorder1.prototype.getName=function(){
  return 'EdgeBorder1';
};

// �ߓ_����Ԃ�
EdgeBorder1.prototype.nodeCount=function(){
  return 2;
};

// �v�f���E�𕪊�����
EdgeBorder1.prototype.splitBorder=function(){
  return [this.clone()];
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta - �v�f������,�ō��W
EdgeBorder1.prototype.shapeFunction=function(xsi,eta){
  if((eta===null) || (eta===undefined)){
    return [[0.5*(1-xsi),-0.5],[0.5*(1+xsi),0.5,1]];
  }
  else{
    return [[0.5*(1-xsi),-0.5,0],[0.5*(1+xsi),0.5,0],[0,0,0]];
  }
};

// �`��֐��}�g���b�N�X [ ��NiNjdS ] ��Ԃ�
// p - �ߓ_
// coef - �W��
EdgeBorder1.prototype.shapeFunctionMatrix=function(p,coef){
  var value=C1_3*coef*p[0].distanceTo(p[1]),vh=0.5*value;
  return [[value,vh],[vh,value]];
};

// �`��֐��x�N�g�� [ ��NidS ] ��Ԃ�
// p - �ߓ_
// coef - �W��
EdgeBorder1.prototype.shapeFunctionVector=function(p,coef){
  var value=0.5*coef*p[0].distanceTo(p[1]);
  return [value,value];
};

// �ӂ̖@���x�N�g����Ԃ�
// p - �ߓ_
// ep - �v�f�̐ߓ_
EdgeBorder1.prototype.normalVector=function(p,ep){
  var ne=normalVector(ep);
  var dir=p[1].clone().sub(p[0]);
  return dir.cross(ne).normalize();
};

//--------------------------------------------------------------------//
// �O�p�`1���v�f���E
// element - �v�f���x��
// nodes - �ߓ_�ԍ�
var TriangleBorder1=function(element,nodes){
  ElementBorder.call(this,element,nodes,null);
};

// �v�f���E���̂�Ԃ�
TriangleBorder1.prototype.getName=function(){
  return 'TriangleBorder1';
};

// �ߓ_����Ԃ�
TriangleBorder1.prototype.nodeCount=function(){
  return 3;
};

// �v�f���E�𕪊�����
TriangleBorder1.prototype.splitBorder=function(){
  return [this.clone()];
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta - �v�f������,�ō��W
TriangleBorder1.prototype.shapeFunction=function(xsi,eta){
  return [[1-xsi-eta,-1,-1],[xsi,1,0],[eta,0,1]];
};

// �[�����R�r�A����Ԃ�
// p - �ߓ_
TriangleBorder1.prototype.jacobian=function(p){
  var p0x=p[0].x,p0y=p[0].y,p0z=p[0].z;
  var j1=(p[1].y-p0y)*(p[2].z-p0z)-(p[1].z-p0z)*(p[2].y-p0y);
  var j2=(p[1].z-p0z)*(p[2].x-p0x)-(p[1].x-p0x)*(p[2].z-p0z);
  var j3=(p[1].x-p0x)*(p[2].y-p0y)-(p[1].y-p0y)*(p[2].x-p0x);
  return Math.sqrt(j1*j1+j2*j2+j3*j3);
};

// �`��֐��}�g���b�N�X [ ��NiNjdS ] ��Ԃ�
// p - �ߓ_
// coef - �W��
TriangleBorder1.prototype.shapeFunctionMatrix=function(p,coef){
  var value=coef*this.jacobian(p)/12,vh=0.5*value;
  var count=this.nodeCount(),s=numeric.rep([count,count],vh);
  for(var i=0;i<count;i++){
    s[i][i]=value;
  }
  return s;
};

// �`��֐��x�N�g�� [ ��NidS ] ��Ԃ�
// p - �ߓ_
// coef - �W��
TriangleBorder1.prototype.shapeFunctionVector=function(p,coef){
  return numeric.rep([this.nodeCount()],C1_6*coef*this.jacobian(p));
};

//--------------------------------------------------------------------//
// �O�p�`2���v�f���E
// element - �v�f���x��
// nodes - �ߓ_�ԍ�
var TriangleBorder2=function(element,nodes){
  ElementBorder.call(this,element,nodes,TRI2_INT);
};

// �v�f���E���̂�Ԃ�
TriangleBorder2.prototype.getName=function(){
  return 'TriangleBorder2';
};

// �ߓ_����Ԃ�
TriangleBorder2.prototype.nodeCount=function(){
  return 6;
};

// ���񏇂ɕ��񂾐ߓ_���x����Ԃ�
TriangleBorder2.prototype.cycleNodes=function(){
  return [this.nodes[0],this.nodes[3],this.nodes[1],this.nodes[4],
      	  this.nodes[2],this.nodes[5]];
};

// �v�f���E�𕪊�����
TriangleBorder2.prototype.splitBorder=function(){
  var p=this.nodes;
  return [new TriangleBorder1(this.element,[p[0],p[3],p[5]]),
      	  new TriangleBorder1(this.element,[p[1],p[4],p[3]]),
      	  new TriangleBorder1(this.element,[p[2],p[5],p[4]]),
      	  new TriangleBorder1(this.element,[p[3],p[4],p[5]])];
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta - �v�f������,�ō��W
TriangleBorder2.prototype.shapeFunction=function(xsi,eta){
  var xe=1-xsi-eta;
  return [[xe*(2*xe-1),1-4*xe,1-4*xe],[xsi*(2*xsi-1),4*xsi-1,0],
      	  [eta*(2*eta-1),0,4*eta-1],[4*xe*xsi,4*(xe-xsi),-4*xsi],
      	  [4*xsi*eta,4*eta,4*xsi],[4*xe*eta,-4*eta,4*(xe-eta)]];
};

//--------------------------------------------------------------------//
// �l�p�`1���v�f���E
// element - �v�f���x��
// nodes - �ߓ_�ԍ�
var QuadangleBorder1=function(element,nodes){
  ElementBorder.call(this,element,nodes,QUAD1_INT);
};

// �v�f���E���̂�Ԃ�
QuadangleBorder1.prototype.getName=function(){
  return 'QuadangleBorder1';
};

// �ߓ_����Ԃ�
QuadangleBorder1.prototype.nodeCount=function(){
  return 4;
};

// �v�f���E�𕪊�����
QuadangleBorder1.prototype.splitBorder=function(){
  var p=this.nodes;
  return [new TriangleBorder1(this.element,[p[0],p[1],p[2]]),
      	  new TriangleBorder1(this.element,[p[2],p[3],p[0]])];
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta - �v�f������,�ō��W
QuadangleBorder1.prototype.shapeFunction=function(xsi,eta){
  return [[0.25*(1-xsi)*(1-eta),-0.25*(1-eta),-0.25*(1-xsi)],
      	  [0.25*(1+xsi)*(1-eta),0.25*(1-eta),-0.25*(1+xsi)],
      	  [0.25*(1+xsi)*(1+eta),0.25*(1+eta),0.25*(1+xsi)],
      	  [0.25*(1-xsi)*(1+eta),-0.25*(1+eta),0.25*(1-xsi)]];
};

//--------------------------------------------------------------------//
// �l�p�`2���v�f���E
// element - �v�f���x��
// nodes - �ߓ_�ԍ�
var QuadangleBorder2=function(element,nodes){
  ElementBorder.call(this,element,nodes,QUAD2_INT);
};

// �v�f���E���̂�Ԃ�
QuadangleBorder2.prototype.getName=function(){
  return 'QuadangleBorder2';
};

// �ߓ_����Ԃ�
QuadangleBorder2.prototype.nodeCount=function(){
  return 8;
};

// ���񏇂ɕ��񂾐ߓ_���x����Ԃ�
QuadangleBorder2.prototype.cycleNodes=function(){
  return [this.nodes[0],this.nodes[4],this.nodes[1],this.nodes[5],
      	  this.nodes[2],this.nodes[6],this.nodes[3],this.nodes[7]];
};

// �v�f���E�𕪊�����
QuadangleBorder2.prototype.splitBorder=function(){
  var p=this.nodes;
  return [new TriangleBorder1(this.element,[p[0],p[4],p[7]]),
      	  new TriangleBorder1(this.element,[p[1],p[5],p[4]]),
      	  new TriangleBorder1(this.element,[p[2],p[6],p[5]]),
      	  new TriangleBorder1(this.element,[p[3],p[7],p[6]]),
      	  new TriangleBorder1(this.element,[p[4],p[5],p[6]]),
      	  new TriangleBorder1(this.element,[p[6],p[7],p[4]])];
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta - �v�f������,�ō��W
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
