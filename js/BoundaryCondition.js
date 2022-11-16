//--------------------------------------------------------------------//
// ���E����
var BoundaryCondition=function(){
  this.restraints=[];		// �S������
  this.loads=[];		// �׏d����
  this.pressures=[];		// �ʈ�����
  this.temperature=[];		// �ߓ_���x����
  this.htcs=[];			// �M�`�B���E����
  this.loadMax=0;		// �ő�׏d
  this.pressMax=0;		// �ő�ʈ�
  this.dof=[];			// �ߓ_�̎��R�x
  this.nodeIndex=[];		// �׏d�x�N�g���̐ߓ_�|�C���^
  this.bcList=[];		// ���E������ݒ肵���ߓ_�̃��X�g
};

// �f�[�^����������
BoundaryCondition.prototype.clear=function(){
  this.restraints.length=0;
  this.loads.length=0;
  this.pressures.length=0;
  this.temperature.length=0;
  this.htcs.length=0;
  this.loadMax=0;
  this.pressMax=0;
};

// ���E����������������
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

// �\����͂̐ߓ_�|�C���^��ݒ肷��
// count - �ߓ_��
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

// �M��͂̐ߓ_�|�C���^��ݒ肷��
// count - �ߓ_��
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

// �����ψʂ�Ԃ�
// bc - �ψʎ��R�x�|�C���^
BoundaryCondition.prototype.getRestDisp=function(bc){
  return this.restraints[parseInt(bc/6)].x[bc%6];
};

// �f�[�^�������Ԃ�
// nodes - �ߓ_
// elems - �v�f
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

// �ߓ_���x�����r����
// bc1,bc2 - ��r���鋫�E����
function compareNodeLabel(bc1,bc2){
  if(bc1.node<bc2.node)      return -1;
  else if(bc1.node>bc2.node) return 1;
  else                       return 0;
}

// �v�f���x�����r����
// bc1,bc2 - ��r���鋫�E����
function compareElementLabel(bc1,bc2){
  if(bc1.element<bc2.element)      return -1;
  else if(bc1.element>bc2.element) return 1;
  else                             return 0;
}

//--------------------------------------------------------------------//
// �R�����x�N�g���i���i�{��]�j
// x,y,z - x,y,z����
// rx,ry,rz - x,y,z�������]�p
var Vector3R=function(x,y,z,rx,ry,rz){
  this.x=[x||0,y||0,z||0,rx||0,ry||0,rz||0];
};

// ���i�����̑傫����Ԃ�
Vector3R.prototype.magnitude=function(){
  return Math.sqrt(this.magnitudeSq());
};

// ���i�����̑傫����2���Ԃ�
Vector3R.prototype.magnitudeSq=function(){
  return this.x[0]*this.x[0]+this.x[1]*this.x[1]+this.x[2]*this.x[2];
};

// ��]�����̑傫����Ԃ�
Vector3R.prototype.magnitudeR=function(){
  return Math.sqrt(this.magnitudeSqR());
};

// ��]�����̑傫����2���Ԃ�
Vector3R.prototype.magnitudeSqR=function(){
  return this.x[3]*this.x[3]+this.x[4]*this.x[4]+this.x[5]*this.x[5];
};

// �x�N�g���̃R�s�[��Ԃ�
Vector3R.prototype.clone=function(){
  return new this.constructor(this.x[0],this.x[1],this.x[2],
      	      	      	      this.x[3],this.x[4],this.x[5]);
};

//--------------------------------------------------------------------//
// �v�f���E����
// element - �v�f���x��
// face - �v�f���E��
var ElementBorderBound=function(element,face){
  this.element=element;
  this.face=face;
};

// �v�f���E��Ԃ�
// elem - �v�f
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
// �S������
// node - �ߓ_���x��
// coords - �Ǐ����W�n
// restx,resty,restz - x,y,z�����̍S���̗L��
// x,y,z - �����ψʂ�x,y,z����
// restrx,restry,restrz - x,y,z�����̉�]�S���̗L��
// rx,ry,rz - �����ψʂ�x,y,z�������]�p
var Restraint=function(node,coords,restx,resty,restz,x,y,z,
      	      	       restrx,restry,restrz,rx,ry,rz){
  Vector3R.call(this,x,y,z,rx,ry,rz);
  this.node=node;
  this.coords=coords;
  this.rest=[restx,resty,restz,restrx,restry,restrz];
  this.globalX=this.x;
};

// �S��������\���������Ԃ�
// nodes - �ߓ_
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
// �׏d����
// node - �ߓ_���x��
// coords - �Ǐ����W�n
// x,y,z - x,y,z����
// rx,ry,rz - x,y,z�������]����
var Load=function(node,coords,x,y,z,rx,ry,rz){
  Vector3R.call(this,x,y,z,rx,ry,rz);
  this.node=node;
  this.coords=coords;
  this.globalX=this.x;
};

// �׏d������\���������Ԃ�
// nodes - �ߓ_
Load.prototype.toString=function(nodes){
  var s='Load\t'+nodes[this.node].label.toString(10)+'\t'+
      	this.x.join('\t');
  if(this.coords){
    s+='\t'+this.coords.label.toString(10);
  }
  return s;
};

//--------------------------------------------------------------------//
// �ʈ�����
// element - �v�f���x��
// face - �v�f���E��
// press - �ʈ�
var Pressure=function(element,face,press){
  ElementBorderBound.call(this,element,face);
  this.press=press;
};

// �ʈ�������\���������Ԃ�
// elems - �v�f
Pressure.prototype.toString=function(elems){
  return 'Pressure\t'+elems[this.element].label.toString(10)+'\t'+
      	  this.face+'\t'+this.press;
};

//--------------------------------------------------------------------//
// ���x�Œ����
// node - �ߓ_���x��
// t - ���x
var Temperature=function(node,t){
  this.node=node;
  this.t=t;
};

// ���x�Œ������\���������Ԃ�
// nodes - �ߓ_
Temperature.prototype.toString=function(nodes){
  return 'Temperature\t'+nodes[this.node].label.toString(10)+'\t'+
      	 this.t;
};

//--------------------------------------------------------------------//
// �M�`�B���E����
// element - �v�f���x��
// face - �v�f���E��
// htc - �M�`�B��
// outTemp - �O�����x
var HeatTransferBound=function(element,face,htc,outTemp){
  ElementBorderBound.call(this,element,face);
  this.htc=htc;
  this.outTemp=outTemp;
};

// �M�`�B���E������\���������Ԃ�
// elems - �v�f
HeatTransferBound.prototype.toString=function(elems){
  return 'HTC\t'+elems[this.element].label.toString(10)+'\t'+
      	  this.face+'\t'+this.htc+'\t'+this.outTemp;
};

inherits(Restraint,Vector3R);
inherits(Load,Vector3R);
inherits(Pressure,ElementBorderBound);
inherits(HeatTransferBound,ElementBorderBound);