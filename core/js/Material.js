var KS_RECT=5/6;			// ��`�f�ʂ̂���f�␳�W��
var KS_CIRCLE=6/7;			// �~�`�f�ʂ̂���f�␳�W��

//--------------------------------------------------------------------//
// �ޗ�
// label - �ޗ��ԍ�
// ee - �����O�� (�c�e���W��) 
// nu - �|�A�\����
// dens - ���x
// hCon - �M�`����
// sHeat - ��M
var Material=function(label,ee,nu,dens,hCon,sHeat){
  this.label=label;
  this.ee=ee;
  this.nu=nu;
  this.dens=dens;
  this.hCon=hCon;
  this.sHeat=sHeat;
  this.gg=0.5*ee/(1+nu);	// ���e���W��
  this.cv=dens*sHeat;		// �̐ϔ�M
  this.matrix=null;		// ���� - �c�}�g���b�N�X
};

// ���ʉ��͖��̉��� - �c�}�g���b�N�X���쐬����
Material.prototype.matrix2Dstress=function(){
  var coef=this.ee/(1-this.nu*this.nu);
  return [[coef,coef*this.nu,0],[coef*this.nu,coef,0],[0,0,this.gg]];
};

// ���ʘc���̉��� - �c�}�g���b�N�X���쐬����
Material.prototype.matrix2Dstrain=function(){
  var coef=this.ee/((1+this.nu)*(1-2*this.nu));
  return [[coef*(1-this.nu),coef*this.nu,0],
      	  [coef*this.nu,coef*(1-this.nu),0],[0,0,this.gg]];
};

// ���Ώ̖��̉��� - �c�}�g���b�N�X���쐬����
Material.prototype.matrixAxiSymetric=function(){
  var coef=this.ee/((1+this.nu)*(1-2*this.nu));
  var s1=coef*(1-this.nu),s2=coef*this.nu;
  return [[s1,s2,s2,0],[s2,s1,s2,0],[s2,s2,s1,0],[0,0,0,this.gg]];
};

// ������̉��� - �c�}�g���b�N�X���쐬����
Material.prototype.matrixTorsion=function(){
  return [[this.gg,0],[0,this.gg]];
};

// 3�������̉��� - �c�}�g���b�N�X���쐬����
Material.prototype.matrix3D=function(){
  var coef=this.ee/((1+this.nu)*(1-2*this.nu));
  var s1=coef*(1-this.nu),s2=coef*this.nu;
  return [[s1,s2,s2,0,0,0],[s2,s1,s2,0,0,0],[s2,s2,s1,0,0,0],
      	  [0,0,0,this.gg,0,0],[0,0,0,0,this.gg,0],[0,0,0,0,0,this.gg]];
};

// �V�F���v�f�̉��� - �c�}�g���b�N�X���쐬����
Material.prototype.matrixShell=function(){
  var coef=this.ee/(1-this.nu*this.nu);
  var s2=coef*this.nu;
  return [[coef,s2,0,0,0],[s2,coef,0,0,0],[0,0,this.gg,0,0],
      	  [0,0,0,KS_RECT*this.gg,0],[0,0,0,0,KS_RECT*this.gg]];
};

// �ޗ���\���������Ԃ�
Material.prototype.toString=function(){
  return 'Material\t'+this.label.toString(10)+'\t'+
      	 this.ee+'\t'+this.nu+'\t'+this.gg+'\t'+this.dens+'\t'+
      	 this.hCon+'\t'+this.sHeat;
};

//--------------------------------------------------------------------//
// �V�F���p�����[�^
// label - �p�����[�^�ԍ�
// thickness - ����
var ShellParameter=function(label,thickness){
  this.label=label;
  this.thickness=thickness;
};

// �V�F���p�����[�^��\���������Ԃ�
ShellParameter.prototype.toString=function(){
  return 'ShellParameter\t'+this.label.toString(10)+'\t'+this.thickness;
};

//--------------------------------------------------------------------//
// ���p�����[�^�i�~�`�f�ʁj
// label - �p�����[�^�ԍ�
// type - �f�ʎ��
// ss - �f�[�^������
var BarParameter=function(label,type,ss){
  this.label=label;
  this.type=type;
  var tp=type.toLowerCase();
  if(tp=='circle'){
    this.section=new CircleSection(ss);
  }
  else if(tp=='rectangle'){
    this.section=new RectSection(ss);
  }
};

// ���p�����[�^��\���������Ԃ�
BarParameter.prototype.toString=function(){
  return 'BarParameter\t'+this.label.toString(10)+'\t'+this.type+'\t'+
      	 this.section.toString();
};
