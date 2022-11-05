//--------------------------------------------------------------------//
// Wilson-Taylor��K���Z�ʑ�1���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var HexaElement1WT=function(label,material,nodes){
  HexaElement1.call(this,label,material,nodes);
  this.te=null;		// ��K�����[�h�̕ϊ��}�g���b�N�X
};

// �v�f���E���̂�Ԃ�
HexaElement1WT.prototype.getName=function(){
  return 'HexaElement1WT';
};

// �����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// d1 - ���� - �c�}�g���b�N�X
HexaElement1WT.prototype.stiffnessMatrix=function(p,d1){
  var size=3*this.nodeCount(),kk=numeric.rep([size,size],0);
  var k2=numeric.rep([size,9],0),k3=numeric.rep([9,9],0);
  var sf0=this.shapeFunction(0,0,0);
  var ja0=this.jacobianMatrix(p,sf0);
  var ji0=new THREE.Matrix3().getInverse(ja0,true).elements;
  var jj0=Math.abs(ja0.determinant());
  for(var i=0;i<this.intP.length;i++){
    var sf=this.shapeFunction(this.intP[i][0],this.intP[i][1],
      	      	      	      this.intP[i][2]);
    var ja=this.jacobianMatrix(p,sf);
    var b=this.strainMatrix(this.grad(p,ja,sf));
    var b0=this.strainMatrix2(ji0,this.intP[i]);
    var ks=this.stiffPart
      (d1,b,this.intP[i][3]*Math.abs(ja.determinant()));
    var cf0=this.intP[i][3]*jj0;
    addMatrix(kk,ks);
    addMatrix(k2,this.stiffPart2(d1,b,b0,cf0));
    addMatrix(k3,this.stiffPart(d1,b0,cf0));
  }
  var k4=numeric.dot(numeric.inv(k3),numeric.negtranspose(k2));
  addMatrix(kk,numeric.dot(k2,k4));
  this.te=numeric.transpose(k4);
  return kk;
};

// �v�f���̘c�x�N�g����Ԃ�
// p - �v�f�ߓ_
// v - �ߓ_�ψʃx�N�g��
// x - ��,��,�č��W
HexaElement1WT.prototype.strainPart=function(p,v,x){
  var sf=this.shapeFunction(x[0],x[1],x[2]);
  var ja=this.jacobianMatrix(p,sf);
  var ji=new THREE.Matrix3().getInverse(ja,true).elements;
  var sm=this.strainMatrix(this.grad(p,ja,sf));
  var sm0=numeric.dot(this.te,this.strainMatrix2(ji,x));
  var count=3*this.nodeCount(),eps=[];
  for(var j=0;j<6;j++){
    var s=0;
    for(var k=0;k<count;k++){
      s+=v[k]*(sm[k][j]+sm0[k][j]);
    }
    eps[j]=s;
  }
  return eps;
};

// ��K�����[�h�̘c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// ji - ���R�r�t�s��̗v�f�z��
// x - ��,��,�č��W
HexaElement1WT.prototype.strainMatrix2=function(ji,x){
  var m=numeric.rep([9,6],0);
  var d1=-2*x[0],d2=-2*x[1],d3=-2*x[2];
  var gr=[[ji[0]*d1,ji[1]*d1,ji[2]*d1],[ji[3]*d2,ji[4]*d2,ji[5]*d2],
      	  [ji[6]*d3,ji[7]*d3,ji[8]*d3]];
  for(var i=0;i<3;i++){
    var i3=3*i,m1=m[i3],m2=m[i3+1],m3=m[i3+2];
    var gx=gr[i][0],gy=gr[i][1],gz=gr[i][2];
    m1[0]=gx;
    m2[1]=gy;
    m3[2]=gz;
    m1[3]=gy;
    m2[3]=gx;
    m2[4]=gz;
    m3[4]=gy;
    m1[5]=gz;
    m3[5]=gx;
  }
  return m;
};

// �ϕ��_�̍����}�g���b�N�X [B1t][D][B2]��Ԃ�
// d - ����-�c�}�g���b�N�X
// b1 - �c-�ψʃ}�g���b�N�X�̓]�u�s�� B1t
// b2 - �c-�ψʃ}�g���b�N�X�̓]�u�s�� B2t
// coef - �W��
HexaElement1WT.prototype.stiffPart2=function(d,b1,b2,coef){
  var size1=b1.length,size2=d.length,size3=b2.length;
  var a=[],k=[],j;
  for(var i=0;i<size1;i++){
    a.length=0;
    var bi=b1[i];
    for(j=0;j<size2;j++){
      a[j]=coef*numeric.dotVV(bi,d[j]);
    }
    var ki=[];
    for(j=0;j<size3;j++){
      ki[j]=numeric.dotVV(a,b2[j]);
    }
    k[i]=ki;
  }
  return k;
};

inherits(HexaElement1WT,HexaElement1);
