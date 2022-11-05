//--------------------------------------------------------------------//
// 3�����L���v�f�@(FEM)

var model;		// FEM�f�[�^���f��
var viewModel;		// �\�����f��
var viewObj;		// �\���I�u�W�F�N�g
var bounds;		// ���f�����E
var info;		// ���f�����\����
var colorBar;		// �J���[�o�[
var resultView;		// ���ʕ\���ݒ�
var viewConfig;		// ���f���\���ݒ�
var modalWindow;	// ���[�_���E�B���h�E

var FILE_WINDOW=0;	// �t�@�C������E�B���h�E
var CALC_WINDOW=1;	// �v�Z�ݒ�E�B���h�E
var RESULT_WINDOW=2;	// ���ʕ\���ݒ�E�B���h�E
var CONFIG_WINDOW=3;	// �R���t�B�O�E�B���h�E

// �f�[�^������������
// fileName - �f�[�^�t�@�C����
function initModel(fileName){
  model=new FemDataModel();
  initThree();
  resultView=new ResultView();
  viewConfig=new ViewConfig();
  if((fileName!==null) && (fileName!==undefined)){
    readServerFemFile(fileName);
  }
  modalWindow=[document.getElementById('file'),
      	       document.getElementById('calc'),
      	       document.getElementById('result'),
      	       document.getElementById('config')];
  loop();
}

// three.js �̏�����������
function initThree(){
  document.addEventListener('keydown',keyPressed,false);
  bounds=new Bounds();
  viewModel=new ViewModel('fem-canvas');
  viewObj=new ViewObject();
  info=document.getElementById('info-canvas');
  colorBar=new ColorBar('colorbar');
}

// �L�[�����������̏������s��
function keyPressed(e){
  switch(e.keyCode){
    case 88:		// X
      viewModel.viewX();
      break;
    case 89:		// Y
      viewModel.viewY();
      break;
    case 90:		// Z
      viewModel.viewZ();
      break;
  }
}

// �\���I�u�W�F�N�g������������
function initObject(){
  viewObj.remove();
  viewObj.create();
  viewModel.updateLightAndCamera();
  colorBar.clear();
  resultView.setContourSelect();
  showInfo();
}

// ���[�v�֐�
function loop(){
  viewModel.update();
  requestAnimationFrame(loop);
  viewModel.setAxis();
}

// ���f������\������
function showInfo(){
  if(model.result.calculated){
    if((model.result.dispMax===0) && (model.result.tempMax!==0)){
      info.textContent='���x Max.:'+numString(model.result.tempMax);
    }
    else{
      info.textContent='�ψ� Max.:'+numString(model.result.dispMax);
    }
  }
  else{
    info.innerHTML='�ߓ_:'+model.mesh.nodes.length+
      	      	   '<br />�v�f:'+model.mesh.elements.length;
  }
}

// �ŗL�l��\������
// index - �ŗL�l�̃C���f�b�N�X
// type - ��͎��
// value - �ŗL�l
function showEigenValue(index,type,value){
  if(type===BUCKLING){
    info.textContent='�ŗL�l'+(index+1)+': '+numString(value);
  }
  else{
    info.textContent='�ŗL�U����'+(index+1)+': '+numString(value);
  }
}

// ���l��\���������Ԃ�
// value - ���l
function numString(value){
  var vabs=Math.abs(value);
  if(vabs>=1.0E5){
    return value.toExponential(4);
  }
  else if((vabs>=1) || (vabs===0)){
    return value.toFixed(3);
  }
  else if(vabs>=0.01){
    return value.toFixed(4);
  }
  else{
    return value.toExponential(4);
  }
}

// ���[�_���E�B���h�E��\������
// win - ���[�_���E�B���h�E�̃C���f�b�N�X
function showModalWindow(win){
  modalWindow[win].style.zIndex=4;
  modalWindow[win].style.opacity=1;
}

// ���[�_���E�B���h�E���\���ɂ���
// win - ���[�_���E�B���h�E�̃C���f�b�N�X
function hideModalWindow(win){
  modalWindow[win].style.zIndex=1;
  modalWindow[win].style.opacity=0;
}

// �p���֌W��ݒ肷��
// ctor - �V�N���X
// superCtor - �p�����N���X
function inherits(ctor,superCtor){
  if((ctor===undefined) || (ctor===null))
    throw new TypeError('The constructor to `inherits` must not be '+
                        'null or undefined.');

  if((superCtor===undefined) || (superCtor===null))
    throw new TypeError('The super constructor to `inherits` must '+
                        'not be null or undefined.');

  if(superCtor.prototype===undefined)
    throw new TypeError('The super constructor to `inherits` must '+
                        'have a prototype.');
  ctor.super_=superCtor;
  Object.setPrototypeOf(ctor.prototype,superCtor.prototype);
}
