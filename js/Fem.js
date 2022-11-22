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

//--------------------------------------------------------------------//
// �t�@�C���ǂݍ��݁E��������

// �T�[�o�[���FEM�f�[�^�t�@�C����ǂݍ���
// fileName - �f�[�^�t�@�C����
function readServerFemFile(fileName){
  var xhr=new XMLHttpRequest();
  try{
    xhr.open('GET',fileName,false);
  }
  catch(e){
    alert(e);
  }
  xhr.onreadystatechange=function(){
    if((xhr.readyState===4) &&
       ((xhr.status===0) || (xhr.status===200))){
      try{
      	readFemModel(xhr.responseText.split(/\r?\n/g));
      }
      catch(ex){
      	alert(ex);
      }
    }
  };
  xhr.send(null);
}

// ���[�J���t�@�C����ǂݍ���
function readLocalFile() {
  var file=document.getElementById('localreadfile').files[0];
  if((file===null) || (file===undefined)){
    alert('�t�@�C�����w�肳��Ă��܂���');
    return;
  }
  var reader=new FileReader();
  reader.readAsText(file);
  reader.onload=function(e){
    try{
      readFemModel(reader.result.split(/\r?\n/g));
    }
    catch(ex){
      alert(ex);
    }
  };
  hideModalWindow(FILE_WINDOW);
}

// ���[�J���t�@�C������������
function writeLocalFile() {
  var file=document.getElementById('localwritefile').value.trim();
  if(file===''){
    alert('�t�@�C�������󔒂ł�');
    return;
  }
  var s=model.toStrings().join('\n')+'\n';
  var blob=new Blob([s],{type:'text/plain'});
  if(window.navigator.msSaveBlob){		// IE��p
    window.navigator.msSaveBlob(blob,file);
  }
  else{						// ���̑��̃u���E�U
    var a=document.createElement('a');
    a.href=URL.createObjectURL(blob);
    a.target='_blank';
    a.download=file;
    a.click();
  }
  hideModalWindow(FILE_WINDOW);
}

// FEM�f�[�^��ǂݍ���
// s - �f�[�^������̃��X�g
function readFemModel(s){
  model.clear();
  var mesh=model.mesh,bc=model.bc,res=[];
  for(var i=0;i<s.length;i++){
    var ss=s[i].trim().replace(/\t/g,' ').split(/\s+/);
    if(ss.length>0){
      var keyWord=ss[0].toLowerCase();
// �ޗ��f�[�^
      if((keyWord=='material') && (ss.length>7)){
      	model.materials.push
      	  (new Material(parseInt(ss[1]),parseFloat(ss[2]),
      	      	      	parseFloat(ss[3]),parseFloat(ss[5]),
      	      	      	parseFloat(ss[6]),parseFloat(ss[7])));
      }
// �V�F���p�����[�^
      else if((keyWord=='shellparameter') && (ss.length>2)){
      	model.shellParams.push
      	  (new ShellParameter(parseInt(ss[1]),parseFloat(ss[2])));
      }
// ���p�����[�^
      else if((keyWord=='barparameter') && (ss.length>4)){
      	model.barParams.push(new BarParameter
      	  (parseInt(ss[1]),ss[2],ss.slice(3,ss.length)));
      }
// �Ǐ����W�n
      else if((keyWord=='coordinates') && (ss.length>10)){
      	model.coordinates.push(readCoordinates(ss));
      }
// �ߓ_
      else if((keyWord=='node') && (ss.length>4)){
      	mesh.nodes.push(new FENode(parseInt(ss[1]),parseFloat(ss[2]),
      	      	      	      	   parseFloat(ss[3]),
      	      	      	      	   parseFloat(ss[4])));
      }
// �v�f
      else if((keyWord=='bebarelement') && (ss.length>5)){
      	if(ss.length<8){
      	  mesh.elements.push(new BEBarElement
      	    (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	     readVertex(ss,4,2)));
      	}
      	else{
      	  mesh.elements.push(new BEBarElement
      	    (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	     readVertex(ss,4,2),
      	     new THREE.Vector3().set(parseFloat(ss[6]),
      	      	      	      	     parseFloat(ss[7]),
      	      	      	      	     parseFloat(ss[8]))));
      	}
      }
      else if((keyWord=='tbarelement') && (ss.length>5)){
      	if(ss.length<8){
      	  mesh.elements.push(new TBarElement
      	    (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	     readVertex(ss,4,2)));
      	}
      	else{
      	  mesh.elements.push(new TBarElement
      	    (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	     readVertex(ss,4,2),
      	     new THREE.Vector3().set(parseFloat(ss[6]),
      	      	      	      	     parseFloat(ss[7]),
      	      	      	      	     parseFloat(ss[8]))));
      	}
      }
      else if((keyWord=='trielement1') && (ss.length>6)){
      	mesh.elements.push(new TriElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	   readVertex(ss,4,3)));
      }
      else if((keyWord=='quadelement1') && (ss.length>7)){
      	mesh.elements.push(new QuadElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	   readVertex(ss,4,4)));
      }
      else if((keyWord=='tetraelement1') && (ss.length>6)){
      	mesh.elements.push(new TetraElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,4)));
      }
      else if((keyWord=='wedgeelement1') && (ss.length>8)){
      	mesh.elements.push(new WedgeElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,6)));
      }
      else if((keyWord=='hexaelement1') && (ss.length>10)){
      	mesh.elements.push(new HexaElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,8)));
      }
      else if((keyWord=='hexaelement1wt') && (ss.length>10)){
      	mesh.elements.push(new HexaElement1WT
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,8)));
      }
      else if((keyWord=='tetraelement2') && (ss.length>12)){
      	mesh.elements.push(new TetraElement2
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,10)));
      }
      else if((keyWord=='wedgeelement2') && (ss.length>17)){
      	mesh.elements.push(new WedgeElement2
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,15)));
      }
      else if((keyWord=='hexaelement2') && (ss.length>22)){
      	mesh.elements.push(new HexaElement2
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,20)));
      }
// ���E����
      else if((keyWord=='restraint') && (ss.length>7)){
      	var rest=readRestraint(ss);
      	if(rest!==null) bc.restraints.push(rest);
      }
      else if((keyWord=='load') && (ss.length>4)){
      	bc.loads.push(readLoad(ss));
      }
      else if((keyWord=='pressure') && (ss.length>3)){
      	bc.pressures.push
      	  (new Pressure(parseInt(ss[1]),ss[2].toUpperCase(),
      	      	      	parseFloat(ss[3])));
      }
      else if((keyWord=='temperature') && (ss.length>2)){
      	bc.temperature.push
      	  (new Temperature(parseInt(ss[1]),parseFloat(ss[2])));
      }
      else if((keyWord=='htc') && (ss.length>4)){
      	bc.htcs.push
      	  (new HeatTransferBound(parseInt(ss[1]),ss[2].toUpperCase(),
      	      	      	      	 parseFloat(ss[3]),parseFloat(ss[4])));
      }
// �v�Z����
      else if((keyWord=='resulttype') && (ss.length>1)){
      	if(ss[1].toLowerCase()=='element'){
      	  model.result.type=ELEMENT_DATA;
      	}
      	else{
      	  model.result.type=NODE_DATA;
      	}
      }
      else if(((keyWord=='eigenvalue') && (ss.length>2)) ||
      	      ((keyWord=='displacement') && (ss.length>7)) ||
      	      ((keyWord=='strain1') && (ss.length>7)) ||
      	      ((keyWord=='stress1') && (ss.length>7)) ||
      	      ((keyWord=='strenergy1') && (ss.length>2)) ||
      	      ((keyWord=='strain2') && (ss.length>7)) ||
      	      ((keyWord=='stress2') && (ss.length>7)) ||
      	      ((keyWord=='strenergy2') && (ss.length>2)) ||
      	      ((keyWord=='temp') && (ss.length>2))){
      	res.push(ss);
      }
    }
  }
  model.init();
  initObject();
  if(res.length>0){
    readFemResult(res);
  }
}

// FEM�̌v�Z���ʂ�ǂݍ���
// s - �f�[�^������̃��X�g
function readFemResult(s){
  var map1=[],map2=[],ss=[],res=model.result,p,i;
  var nodes=model.mesh.nodes,elems=model.mesh.elements;
  for(i=0;i<nodes.length;i++){
    map1[nodes[i].label]=i;
  }
  if(res.type===ELEMENT_DATA){
    for(i=0;i<elems.length;i++){
      map2[elems[i].label]=i;
    }
  }
  else{
    map2=map1;
  }
  for(i=0;i<s.length;i++){
    var keyWord=s[i][0].toLowerCase();
    ss.length=0;
    for(var j=2;j<s[i].length;j++){
      ss[j-2]=parseFloat(s[i][j]);
    }
    if(keyWord=='eigenvalue'){
      var ev=new EigenValue(ss[0],s[i][1]);
      model.result.addEigenValue(ev);
      res=ev;
    }
    else if(keyWord=='displacement'){
      p=readDataPointer(s[i],map1);
      var d=new Vector3R(ss[0],ss[1],ss[2],ss[3],ss[4],ss[5]);
      res.displacement[p]=d;
      res.dispMax=Math.max(res.dispMax,d.magnitude());
      res.angleMax=Math.max(res.angleMax,d.magnitudeR());
    }
    else if(keyWord=='strain1'){
      p=readDataPointer(s[i],map2);
      model.result.strain1[p]=new Strain(ss);
    }
    else if(keyWord=='stress1'){
      p=readDataPointer(s[i],map2);
      model.result.stress1[p]=new Stress(ss);
    }
    else if(keyWord=='strenergy1'){
      p=readDataPointer(s[i],map2);
      res.sEnergy1[p]=ss[0];
    }
    else if(keyWord=='strain2'){
      p=readDataPointer(s[i],map2);
      model.result.strain2[p]=new Strain(ss);
    }
    else if(keyWord=='stress2'){
      p=readDataPointer(s[i],map2);
      model.result.stress2[p]=new Stress(ss);
    }
    else if(keyWord=='strenergy2'){
      p=readDataPointer(s[i],map2);
      res.sEnergy2[p]=ss[0];
    }
    else if(keyWord=='temp'){
      p=readDataPointer(s[i],map1);
      model.result.temperature[p]=ss[0];
      model.result.tempMax=Math.max(model.result.tempMax,ss[0]);
    }
  }
  model.result.calculated=true;
  if(model.result.eigenValue.length===0){
    resultView.setInitStatic();
    showInfo();
  }
  else{
    resultView.setInitEigen();
  }
}

// �f�[�^�|�C���^���l������
// ss - �f�[�^������
// map - ���x���}�b�v
function readDataPointer(ss,map){
  var p=parseInt(ss[1]);
  if(p in map){
    return map[p];
  }
  else{
    throw new Error('�v�Z����'+ss[0]+'�̔ԍ�'+p+
      	      	    '�͑��݂��܂���');
  }
}

// �ߓ_�ԍ���ǂݎ��
// ss - ������z��
// is - �J�n�C���f�b�N�X
// count - �ߓ_��
function readVertex(ss,is,count){
  var vertex=[];
  for(var j=0;j<count;j++) vertex[j]=parseInt(ss[is+j]);
  return vertex;
}

// �Ǐ����W�n��ǂݍ���
// ss - �f�[�^������z��
function readCoordinates(ss){
  var c=[[parseFloat(ss[2]),parseFloat(ss[3]),parseFloat(ss[4])],
      	 [parseFloat(ss[5]),parseFloat(ss[6]),parseFloat(ss[7])],
      	 [parseFloat(ss[8]),parseFloat(ss[9]),parseFloat(ss[10])]];
  for(var i=0;i<3;i++){
    var ci=c[i];
    var cf=ci[0]*ci[0]+ci[1]*ci[1]+ci[2]*ci[2];
    if(cf===0){
      throw new Error('���W�n'+ss[2]+'�̎������x�N�g����0�ł�');
    }
    cf=1/Math.sqrt(cf);
    ci[0]*=cf;
    ci[1]*=cf;
    ci[2]*=cf;
  }
  return new Coordinates(parseInt(ss[1]),c[0][0],c[1][0],c[2][0],
      	      	      	 c[0][1],c[1][1],c[2][1],
      	      	      	 c[0][2],c[1][2],c[2][2]);
}

// �S��������ǂݍ���
// ss - �f�[�^������z��
function readRestraint(ss){
  var rx=(parseInt(ss[2])!==0);
  var ry=(parseInt(ss[4])!==0);
  var rz=(parseInt(ss[6])!==0);
  var x=parseFloat(ss[3]),y=parseFloat(ss[5]),z=parseFloat(ss[7]);
  var coords=null;
  if(ss.length<14){
    if(ss.length>8) coords=parseInt(ss[8]);
    if(!rx && !ry && !rz) return null;
    return new Restraint(parseInt(ss[1]),coords,rx,ry,rz,x,y,z);
  }
  else{
    if(ss.length>14) coords=parseInt(ss[14]);
    var rrx=(parseInt(ss[8])!==0);
    var rry=(parseInt(ss[10])!==0);
    var rrz=(parseInt(ss[12])!==0);
    if(!rx && !ry && !rz && !rrx && !rry && !rrz) return null;
    return new Restraint(parseInt(ss[1]),coords,rx,ry,rz,x,y,z,
      	      	      	 rrx,rry,rrz,parseFloat(ss[9]),
      	      	      	 parseFloat(ss[11]),parseFloat(ss[13]));
  }
}

// �׏d������ǂݍ���
// ss - �f�[�^������z��
function readLoad(ss){
  var coords=null;
  if(ss.length<8){
    if(ss.length>5) coords=parseInt(ss[5]);
    return new Load(parseInt(ss[1]),coords,parseFloat(ss[2]),
      	      	    parseFloat(ss[3]),parseFloat(ss[4]));
  }
  else{
    if(ss.length>8) coords=parseInt(ss[8]);
    return new Load(parseInt(ss[1]),coords,
      	      	    parseFloat(ss[2]),parseFloat(ss[3]),
      	      	    parseFloat(ss[4]),parseFloat(ss[5]),
      	      	    parseFloat(ss[6]),parseFloat(ss[7]));
  }
}

// �t�@�C������E�B���h�E��\������
function showFileWindow(){
  showModalWindow(FILE_WINDOW);
}

// �t�@�C�������������
function cancelFile(){
  hideModalWindow(FILE_WINDOW);
// �t�@�C���I������������
  var localfile=document.getElementById('localreadfile');
  localfile.parentNode.innerHTML=localfile.parentNode.innerHTML;
}

var COEF_F_W=0.5/Math.PI;	// f/�֔� 1/2��

//--------------------------------------------------------------------//
// FEM �f�[�^���f��
var FemDataModel=function(){
  this.materials=[];			// �ޗ�
  this.shellParams=[];			// �V�F���p�����[�^
  this.barParams=[];			// ���p�����[�^
  this.coordinates=[];			// �Ǐ����W�n
  this.mesh=new MeshModel();		// ���b�V�����f��
  this.bc=new BoundaryCondition();	// ���E����
  this.solver=new Solver();		// �A�������������I�u�W�F�N�g
  this.result=new Result();		// �v�Z����
  this.hasShellBar=false;		// �V�F���v�f�܂��͗��v�f���܂܂Ȃ�
};

// �f�[�^����������
FemDataModel.prototype.clear=function(){
  this.materials.length=0;
  this.shellParams.length=0;
  this.barParams.length=0;
  this.coordinates.length=0;
  this.mesh.clear();
  this.bc.clear();
  this.result.clear();
  this.result.type=NODE_DATA;
};

// ���f��������������
FemDataModel.prototype.init=function(){
  this.solver.method=ILUCG_METHOD;	// �f�t�H���g�͔�����@
  var mats=this.materials;
  mats.sort(compareLabel);
  this.mesh.init();
  this.bc.init();
  this.reNumbering();
  this.resetMaterialLabel();
  this.resetParameterLabel();
  this.resetCoordinates();
  this.mesh.checkChirality();
  this.mesh.getFreeFaces();
  this.mesh.getFaceEdges();
  for(var i=0;i<mats.length;i++){
    var m2d=mats[i].matrix2Dstress();
    var msh=mats[i].matrixShell();
    var m3d=mats[i].matrix3D();
    mats[i].matrix={m2d:m2d,msh:msh,m3d:m3d};
  }
};

// �ߓ_�E�v�f�|�C���^��ݒ肷��
FemDataModel.prototype.reNumbering=function(){
  var nodes=this.mesh.nodes,elements=this.mesh.elements;
  var map=[],i;
  for(i=0;i<nodes.length;i++){
    map[nodes[i].label]=i;
  }
  for(i=0;i<elements.length;i++){
    resetNodes(map,elements[i]);
  }
  for(i=0;i<this.bc.restraints.length;i++){
    resetNodePointer(map,this.bc.restraints[i]);
  }
  for(i=0;i<this.bc.loads.length;i++){
    resetNodePointer(map,this.bc.loads[i]);
  }
  for(i=0;i<this.bc.temperature.length;i++){
    resetNodePointer(map,this.bc.temperature[i]);
  }
  map.length=0;
  for(i=0;i<elements.length;i++){
    map[elements[i].label]=i;
  }
  for(i=0;i<this.bc.pressures.length;i++){
    resetElementPointer(map,this.bc.pressures[i]);
  }
  for(i=0;i<this.bc.htcs.length;i++){
    resetElementPointer(map,this.bc.htcs[i]);
  }
};

// �ޗ��|�C���^��ݒ肷��
FemDataModel.prototype.resetMaterialLabel=function(){
  if(this.materials.length===0){
    this.materials.push(new Material(1,1,0.3,1,1,1,1));
  }
  var map=[],i,elements=this.mesh.elements;
  for(i=0;i<this.materials.length;i++){
    map[this.materials[i].label]=i;
  }
  for(i=0;i<elements.length;i++){
    if(elements[i].material in map){
      elements[i].material=map[elements[i].material];
    }
    else{
      throw new Error('�ޗ��ԍ�'+elements[i].material+
      	      	      '�͑��݂��܂���');
    }
  }
};

// �V�F���p�����[�^�E���p�����[�^�̃|�C���^��ݒ肷��
FemDataModel.prototype.resetParameterLabel=function(){
  if((this.shellParams.length===0) && (this.barParams.length===0)){
    this.hasShellBar=false;
    return;
  }
  var map1=[],map2=[],i,elements=this.mesh.elements,shellbars=0;
  for(i=0;i<this.shellParams.length;i++){
    map1[this.shellParams[i].label]=i;
  }
  for(i=0;i<this.barParams.length;i++){
    map2[this.barParams[i].label]=i;
  }
  for(i=0;i<elements.length;i++){
    if(elements[i].isShell){
      if(elements[i].param in map1){
      	elements[i].param=map1[elements[i].param];
      	shellbars++;
      }
      else{
      	throw new Error('�p�����[�^�ԍ�'+elements[i].param+
      	      	      	'�͑��݂��܂���');
      }
    }
    else if(elements[i].isBar){
      if(elements[i].param in map2){
      	elements[i].param=map2[elements[i].param];
      	shellbars++;
      }
      else{
      	throw new Error('�p�����[�^�ԍ�'+elements[i].param+
      	      	      	'�͑��݂��܂���');
      }
    }
  }
  this.hasShellBar=(shellbars>0);
  if(this.hasShellBar){		// �V�F���v�f�E���v�f���܂ޏꍇ�͒��ډ�@
    this.solver.method=LU_METHOD;
  }
};

// �Ǐ����W�n��ݒ肷��
FemDataModel.prototype.resetCoordinates=function(){
  if(this.coordinates.length===0){
    return;
  }
  var map=[],i;
  for(i=0;i<this.coordinates.length;i++){
    map[this.coordinates[i].label]=this.coordinates[i];
  }
  for(i=0;i<this.bc.restraints.length;i++){
    resetCoordinatesPointer(map,this.bc.restraints[i]);
  }
  for(i=0;i<this.bc.loads.length;i++){
    resetCoordinatesPointer(map,this.bc.loads[i]);
  }
};

// �ߓ_�̎��R�x��ݒ肷��
FemDataModel.prototype.setNodeDoF=function(){
  var i,dof=this.bc.dof;
  var nodeCount=this.mesh.nodes.length;
  var elemCount=this.mesh.elements.length;
  dof.length=0;
  for(i=0;i<nodeCount;i++){
    dof[i]=3;
  }
  for(i=0;i<elemCount;i++){
    var elem=this.mesh.elements[i];
    if(elem.isShell || elem.isBar){	// �V�F���v�f�E���v�f
      var count=elem.nodeCount();
      for(var j=0;j<count;j++){
      	dof[elem.nodes[j]]=6;
      }
    }
  }
  this.solver.dof=this.bc.setPointerStructure(nodeCount);
};

// �É�͂�����
FemDataModel.prototype.calculate=function(){
  var t0=new Date().getTime();
  var calc=false;
  if((this.bc.temperature.length>0) || (this.bc.htcs.length>0)){
    this.solver.dof=this.mesh.nodes.length;
    this.bc.setPointerHeat(this.solver.dof);
    this.solver.createHeatMatrix();
    var tmp=this.solver.solve();
    this.result.setTemperature(this.bc,tmp,this.mesh.nodes.length);
    calc=true;
  }
  if(this.bc.restraints.length>0){
    this.setNodeDoF();
    this.solver.createStiffnessMatrix();
    var d=this.solver.solve();
    this.result.setDisplacement(this.bc,d,this.mesh.nodes.length);
    if(this.result.type===ELEMENT_DATA){
      this.calculateElementStress();
    }
    else{
      this.calculateNodeStress();
    }
    calc=true;
  }
  if(!calc){
    alert('�S�������s���̂��ߌv�Z�ł��܂���');
  }
  var t1=new Date().getTime();
  console.log('Calculation time:'+(t1-t0)+'ms');
};

// �ŗL�U�����E�ŗL�x�N�g�������߂�
// count - ���߂�ŗL�U���̐�
FemDataModel.prototype.charVib=function(count){
  var t0=new Date().getTime();
  this.result.clear();
  this.setNodeDoF();
  count=Math.min(count,this.solver.dof);
  var n=Math.min(3*count,this.solver.dof),i;
  this.solver.createStiffMassMatrix();
  var eig=this.solver.eigenByLanczos(n);
  var nodeCount=this.mesh.nodes.length;
  for(i=count;i<n;i++) delete eig.ut[i];
  for(i=0;i<count;i++){
    var f=COEF_F_W*Math.sqrt(Math.max(eig.lambda[i],0));
    var uti=eig.ut[i],s=0;
    for(var j=0;j<uti.length;j++) s+=uti[j]*uti[j];
    var u=numeric.mul(1/Math.sqrt(s),uti);
    var ev=new EigenValue(f,VIBRATION);
    ev.setDisplacement(this.bc,u,nodeCount);
    this.result.addEigenValue(ev);
    if(this.result.type===ELEMENT_DATA){
      this.calculateEvElementEnergy(ev);
    }
    else{
      this.calculateEvNodeEnergy(ev);
    }
    delete eig.ut[i];
  }
  var t1=new Date().getTime();
  console.log('Calculation time:'+(t1-t0)+'ms');
};

// ���`������͂�����
// count - ���߂�ŗL�l�̐�
FemDataModel.prototype.calcBuckling=function(count){
  var t0=new Date().getTime();
  if(this.bc.restraints.length===0){
    throw new Error('�S������������܂���');
  }
  this.setNodeDoF();
  var n=Math.min(3*count,this.solver.dof),i;
  this.solver.createStiffnessMatrix();
  var d=this.solver.solve();
  this.result.setDisplacement(this.bc,d,this.mesh.nodes.length);
  this.solver.createGeomStiffMatrix();
  this.result.clear();
  var eig=this.solver.eigenByArnoldi(n,0);
  var nodeCount=this.mesh.nodes.length;
  for(i=count;i<n;i++) delete eig.ut[i];
  for(i=0;i<count;i++){
    var uti=eig.ut[i],s=0;
    for(var j=0;j<uti.length;j++) s+=uti[j]*uti[j];
    var u=numeric.mul(1/Math.sqrt(s),uti);
    var ev=new EigenValue(eig.lambda[i],BUCKLING);
    ev.setDisplacement(this.bc,u,nodeCount);
    this.result.addEigenValue(ev);
    if(this.result.type===ELEMENT_DATA){
      this.calculateEvElementEnergy(ev);
    }
    else{
      this.calculateEvNodeEnergy(ev);
    }
    delete eig.ut[i];
  }
  var t1=new Date().getTime();
  console.log('Calculation time:'+(t1-t0)+'ms');
};

// �ߓ_�c�E���́E�c�G�l���M�[���x���v�Z����
FemDataModel.prototype.calculateNodeStress=function(){
  var nodes=this.mesh.nodes,nodeCount=nodes.length;
  var elems=this.mesh.elements,elemCount=elems.length;
  var angle=numeric.rep([nodeCount],0),p=[],v=[],i,j,s,eaj;
  var eps1,str1,se1,eps2,str2,se2;
  this.result.initStrainAndStress(nodeCount);
  for(i=0;i<elemCount;i++){
    var elem=elems[i],en=elem.nodes;
    p.length=0;
    v.length=0;
    for(j=0;j<en.length;j++){
      p[j]=nodes[en[j]];
      v[j]=this.result.displacement[en[j]];
    }
    var material=this.materials[elem.material],mat=material.matrix;
    var ea=elem.angle(p);
    if(elem.isShell){
      var sp=model.shellParams[elem.param],mmat;
      if(elem.getName()==='TriElement1'){
      	mmat=mat.m2d;
      }
      else{
      	mmat=mat.msh;
      }
      s=elem.strainStress(p,v,mmat,sp);
      eps1=s[0];
      str1=s[1];
      se1=s[2];
      eps2=s[3];
      str2=s[4];
      se2=s[5];
      for(j=0;j<en.length;j++){
      	eaj=ea[j];
      	eps1[j].mul(eaj);
      	eps2[j].mul(eaj);
      	str1[j].mul(eaj);
      	str2[j].mul(eaj);
      	se1[j]*=eaj;
      	se2[j]*=eaj;
      	this.result.addStructureData(en[j],eps1[j],str1[j],se1[j],
      	      	      	      	     eps2[j],str2[j],se2[j]);
      	angle[en[j]]+=eaj;
      }
    }
    else if(elem.isBar){
      var sect=model.barParams[elem.param].section;
      s=elem.strainStress(p,v,material,sect);
      eps1=s[0];
      str1=s[1];
      se1=s[2];
      eps2=s[3];
      str2=s[4];
      se2=s[5];
      for(j=0;j<en.length;j++){
      	this.result.addStructureData(en[j],eps1[j],str1[j],se1[j],
      	      	      	      	     eps2[j],str2[j],se2[j]);
      	angle[en[j]]++;
      }
    }
    else{
      s=elem.strainStress(p,v,mat.m3d);
      eps1=s[0];
      str1=s[1];
      se1=s[2];
      for(j=0;j<en.length;j++){
      	eaj=ea[j];
      	eps1[j].mul(eaj);
      	str1[j].mul(eaj);
      	se1[j]*=eaj;
      	this.result.addStructureData(en[j],eps1[j],str1[j],se1[j],
      	      	      	      	     eps1[j],str1[j],se1[j]);
      	angle[en[j]]+=eaj;
      }
    }
  }
  for(i=0;i<nodeCount;i++){
    if(angle[i]!==0){
      this.result.mulStructureData(i,1/angle[i]);
    }
  }
};

// �v�f�c�E���́E�c�G�l���M�[���x���v�Z����
FemDataModel.prototype.calculateElementStress=function(){
  var nodes=this.mesh.nodes,p=[],v=[],s;
  var elems=this.mesh.elements,elemCount=elems.length;
  this.result.initStrainAndStress(elemCount);
  for(var i=0;i<elemCount;i++){
    var elem=elems[i],en=elem.nodes;
    p.length=0;
    v.length=0;
    for(var j=0;j<en.length;j++){
      p[j]=nodes[en[j]];
      v[j]=this.result.displacement[en[j]];
    }
    var material=this.materials[elem.material],mat=material.matrix;
    if(elem.isShell){
      var sp=model.shellParams[elem.param],mmat;
      if(elem.getName()==='TriElement1'){
      	mmat=mat.m2d;
      }
      else{
      	mmat=mat.msh;
      }
      s=elem.elementStrainStress(p,v,mmat,sp);
      this.result.addStructureData(i,s[0],s[1],s[2],s[3],s[4],s[5]);
    }
    else if(elem.isBar){
      var sect=model.barParams[elem.param].section;
      s=elem.elementStrainStress(p,v,material,sect);
      this.result.addStructureData(i,s[0],s[1],s[2],s[3],s[4],s[5]);
    }
    else{
      s=elem.elementStrainStress(p,v,mat.m3d);
      this.result.addStructureData(i,s[0],s[1],s[2],s[0],s[1],s[2]);
    }
  }
};

// �ŗL�l�f�[�^�̐ߓ_�c�G�l���M�[���x���v�Z����
// ev - �ŗL�l�f�[�^
FemDataModel.prototype.calculateEvNodeEnergy=function(ev){
  var nodes=this.mesh.nodes,nodeCount=nodes.length;
  var elems=this.mesh.elements,elemCount=elems.length;
  var angle=numeric.rep([nodeCount],0),p=[],v=[],i,j,s,enj,eaj,se1,se2;
  ev.initStrainEnergy(nodeCount);
  for(i=0;i<elemCount;i++){
    var elem=elems[i],en=elem.nodes;
    p.length=0;
    v.length=0;
    for(j=0;j<en.length;j++){
      p[j]=nodes[en[j]];
      v[j]=ev.displacement[en[j]];
    }
    var material=this.materials[elem.material],mat=material.matrix;
    var ea=elem.angle(p);
    if(elem.isShell){
      var sp=model.shellParams[elem.param],mmat;
      if(elem.getName()==='TriElement1'){
      	mmat=mat.m2d;
      }
      else{
      	mmat=mat.msh;
      }
      s=elem.strainStress(p,v,mmat,sp);
      se1=s[2];
      se2=s[5];
      for(j=0;j<en.length;j++){
      	enj=en[j];
      	eaj=ea[j];
      	se1[j]*=eaj;
      	se2[j]*=eaj;
      	ev.sEnergy1[enj]+=se1[j];
      	ev.sEnergy2[enj]+=se2[j];
      	angle[enj]+=eaj;
      }
    }
    else if(elem.isBar){
      var sect=model.barParams[elem.param].section;
      s=elem.strainStress(p,v,material,sect);
      se1=s[2];
      se2=s[5];
      for(j=0;j<en.length;j++){
      	enj=en[j];
      	ev.sEnergy1[enj]+=se1[j];
      	ev.sEnergy2[enj]+=se2[j];
      	angle[enj]++;
      }
    }
    else{
      s=elem.strainStress(p,v,mat.m3d);
      se1=s[2];
      for(j=0;j<en.length;j++){
      	enj=en[j];
      	eaj=ea[j];
      	se1[j]*=eaj;
      	ev.sEnergy1[enj]+=se1[j];
      	ev.sEnergy2[enj]+=se1[j];
      	angle[enj]+=eaj;
      }
    }
  }
  for(i=0;i<nodeCount;i++){
    if(angle[i]!==0){
      var coef=1/angle[i];
      ev.sEnergy1[i]*=coef;
      ev.sEnergy2[i]*=coef;
    }
  }
};

// �ŗL�l�f�[�^�̗v�f�c�G�l���M�[���x���v�Z����
// ev - �ŗL�l�f�[�^
FemDataModel.prototype.calculateEvElementEnergy=function(ev){
  var nodes=this.mesh.nodes,p=[],v=[],s;
  var elems=this.mesh.elements,elemCount=elems.length;
  ev.initStrainEnergy(elemCount);
  for(var i=0;i<elemCount;i++){
    var elem=elems[i],en=elem.nodes;
    p.length=0;
    v.length=0;
    for(var j=0;j<en.length;j++){
      p[j]=nodes[en[j]];
      v[j]=ev.displacement[en[j]];
    }
    var material=this.materials[elem.material],mat=material.matrix;
    if(elem.isShell){
      var sp=model.shellParams[elem.param],mmat;
      if(elem.getName()==='TriElement1'){
      	mmat=mat.m2d;
      }
      else{
      	mmat=mat.msh;
      }
      s=elem.elementStrainStress(p,v,mmat,sp);
      ev.sEnergy1[i]=s[2];
      ev.sEnergy2[i]=s[5];
    }
    else if(elem.isBar){
      var sect=model.barParams[elem.param].section;
      s=elem.elementStrainStress(p,v,material,sect);
      ev.sEnergy1[i]=s[2];
      ev.sEnergy2[i]=s[5];
    }
    else{
      s=elem.elementStrainStress(p,v,mat.m3d);
      ev.sEnergy1[i]=s[2];
      ev.sEnergy2[i]=s[2];
    }
  }
};

// �f�[�^�������Ԃ�
FemDataModel.prototype.toStrings=function(){
  var s=[],i,nodes=this.mesh.nodes,elems=this.mesh.elements;
  for(i=0;i<this.materials.length;i++){
    s.push(this.materials[i].toString());
  }
  for(i=0;i<this.shellParams.length;i++){
    s.push(this.shellParams[i].toString());
  }
  for(i=0;i<this.barParams.length;i++){
    s.push(this.barParams[i].toString());
  }
  for(i=0;i<this.coordinates.length;i++){
    s.push(this.coordinates[i].toString());
  }
  for(i=0;i<nodes.length;i++){
    s.push(nodes[i].toString());
  }
  for(i=0;i<elems.length;i++){
    if(elems[i].isShell){
      s.push(elems[i].toString(this.materials,this.shellParams,nodes));
    }
    else if(elems[i].isBar){
      s.push(elems[i].toString(this.materials,this.barParams,nodes));
    }
    else{
      s.push(elems[i].toString(this.materials,nodes));
    }
  }
  Array.prototype.push.apply(s,this.bc.toStrings(nodes,elems));
  Array.prototype.push.apply(s,this.result.toStrings(nodes,elems));
  return s;
};

// �ߓ_�W���̐ߓ_���x�����Đݒ肷��
// map - ���x���}�b�v
// s - �ߓ_�W��
function resetNodes(map,s){
  for(var i=0;i<s.nodes.length;i++){
    if(s.nodes[i] in map){
      s.nodes[i]=map[s.nodes[i]];
    }
    else{
      throw new Error('�ߓ_�ԍ�'+s.nodes[i]+'�͑��݂��܂���');
    }
  }
}

// �ߓ_�|�C���^���Đݒ肷��
// map - ���x���}�b�v
// bc - ���E����
function resetNodePointer(map,bc){
  if(bc.node in map){
    bc.node=map[bc.node];
  }
  else{
    throw new Error('�ߓ_�ԍ�'+bc.node+'�͑��݂��܂���');
  }
}

// �v�f�|�C���^���Đݒ肷��
// map - ���x���}�b�v
// bc - ���E����
function resetElementPointer(map,bc){
  if(bc.element in map){
    bc.element=map[bc.element];
  }
  else{
    throw new Error('�v�f�ԍ�'+bc.element+'�͑��݂��܂���');
  }
}

// �Ǐ����W�n���Đݒ肷��
// map - ���x���}�b�v
// bc - ���E����
function resetCoordinatesPointer(map,bc){
  var coords=bc.coords;
  if((coords===null) || (coords===undefined)){
  }
  else if(coords in map){
    bc.coords=map[coords];
    bc.globalX=bc.coords.toGlobal(bc.x);
  }
  else{
    throw new Error('�Ǐ����W�n�ԍ�'+coords+'�͑��݂��܂���');
  }
}

//--------------------------------------------------------------------//
// ���b�V�����f��
var MeshModel=function(){
  this.nodes=[];		// �ߓ_
  this.elements=[];		// �v�f
  this.freeFaces=[];		// �\��
  this.faceEdges=[];		// �\�ʂ̗v�f��
};

// �ߓ_��Ԃ�
// s - �ߓ_�W��
MeshModel.prototype.getNodes=function(s){
  var p=[];
  for(var i=0;i<s.nodes.length;i++){
    p[i]=this.nodes[s.nodes[i]];
  }
  return p;
};

// �f�[�^����������
MeshModel.prototype.clear=function(){
  this.nodes.length=0;
  this.elements.length=0;
  this.freeFaces.length=0;
  this.faceEdges.length=0;
};

// ���f��������������
MeshModel.prototype.init=function(){
  this.nodes.sort(compareLabel);
  bounds.set();
};

// �v�f�̋��������𑵂���
MeshModel.prototype.checkChirality=function(){
  for(var i=0;i<this.elements.length;i++){
    var elem=this.elements[i];
    if(!elem.isShell && !elem.isBar){
      var pe=this.getNodes(elem);
      var pf=this.getNodes(elem.border(i,0));
      var n1=normalVector(pf);
      var n2=center(pe).sub(center(pf));
      if(n1.dot(n2)>0){
      	elem.mirror();
      }
    }
  }
};

// �\�ʂ����o��
MeshModel.prototype.getFreeFaces=function(){
  var elems=this.elements,i;
  if(elems.length===0) return;
  this.freeFaces.length=0;
  var border=[];
  for(i=0;i<elems.length;i++){
    if(elems[i].isShell){
      this.freeFaces.push(elems[i].border(i,0));
    }
    else if(!elems[i].isBar){
      var count=elems[i].borderCount();
      for(var j=0;j<count;j++){
      	border.push(elems[i].border(i,j));
      }
    }
  }
  if(border.length>0){
    border.sort(function(b1,b2){return b1.compare(b2);});
    var addsw=true,beforeEb=border[0];
    for(i=1;i<border.length;i++){
      var eb=border[i];
      if(beforeEb.compare(eb)===0){
      	addsw=false;
      }
      else{
      	if(addsw) this.freeFaces.push(beforeEb);
      	beforeEb=eb;
      	addsw=true;
      }
    }
    if(addsw) this.freeFaces.push(beforeEb);
  }
};

// �\�ʂ̗v�f�ӂ����o��
MeshModel.prototype.getFaceEdges=function(){
  if(this.freeFaces.length===0) return;
  this.faceEdges.length=0;
  var edges=[],i;
  for(i=0;i<this.freeFaces.length;i++){
    var nds=this.freeFaces[i].cycleNodes();
    for(var j=0;j<nds.length;j++){
      edges.push(new EdgeBorder1(i,[nds[j],nds[(j+1)%nds.length]]));
    }
  }
  if(edges.length>0){
    edges.sort(function(b1,b2){return b1.compare(b2);});
    var beforeEdge=edges[0];
    this.faceEdges.push(beforeEdge);
    for(i=1;i<edges.length;i++){
      var edge=edges[i];
      if(beforeEdge.compare(edge)!==0){
      	this.faceEdges.push(edge);
      	beforeEdge=edge;
      }
    }
  }
};

// �`��f�[�^�����o��
MeshModel.prototype.getGeometry=function(){
  var sb=[],i;
  for(i=0;i<this.freeFaces.length;i++){
    Array.prototype.push.apply(sb,this.freeFaces[i].splitBorder());
  }
  var pos=new Float32Array(9*sb.length);
  var norm=new Float32Array(9*sb.length);
  var colors=new Float32Array(9*sb.length);
  var geometry=new THREE.BufferGeometry();
  geometry.elements=new Int32Array(3*sb.length);
  geometry.nodes=new Int32Array(3*sb.length);
  geometry.angle=new Float32Array(9*sb.length);
  for(i=0;i<sb.length;i++){
    var i9=9*i,v=sb[i].nodes,elem=sb[i].element;
    var p=[this.nodes[v[0]],this.nodes[v[1]],this.nodes[v[2]]];
    var n=normalVector(p);
    for(var j=0;j<3;j++){
      var j3=i9+3*j;
      geometry.elements[3*i+j]=elem;
      geometry.nodes[3*i+j]=v[j];
      pos[j3]=p[j].x;
      pos[j3+1]=p[j].y;
      pos[j3+2]=p[j].z;
      norm[j3]=n.x;
      norm[j3+1]=n.y;
      norm[j3+2]=n.z;
      colors[j3]=meshColors[0];
      colors[j3+1]=meshColors[1];
      colors[j3+2]=meshColors[2];
      geometry.angle[j3]=0;
      geometry.angle[j3+1]=0;
      geometry.angle[j3+2]=0;
    }
  }
  geometry.addAttribute('position',new THREE.BufferAttribute(pos,3));
  geometry.addAttribute('normal',new THREE.BufferAttribute(norm,3));
  geometry.addAttribute('color',new THREE.BufferAttribute(colors,3));
  return geometry;
};

// �v�f�ӂ̌`��f�[�^�����o��
MeshModel.prototype.getEdgeGeometry=function(){
  var edges=this.faceEdges;
  var pos=new Float32Array(6*edges.length);
  var geometry=new THREE.BufferGeometry();
  geometry.nodes=new Int32Array(2*edges.length);
  geometry.angle=new Float32Array(6*edges.length);
  for(var i=0;i<edges.length;i++){
    var i2=2*i,i6=6*i,v=edges[i].nodes;
    var p1=this.nodes[v[0]],p2=this.nodes[v[1]];
    geometry.nodes[i2]=v[0];
    geometry.nodes[i2+1]=v[1];
    pos[i6]=p1.x;
    pos[i6+1]=p1.y;
    pos[i6+2]=p1.z;
    pos[i6+3]=p2.x;
    pos[i6+4]=p2.y;
    pos[i6+5]=p2.z;
    for(var j=0;j<6;j++) geometry.angle[i6+j]=0;
  }
  geometry.addAttribute('position',new THREE.BufferAttribute(pos,3));
  return geometry;
};

// ���v�f�̌`��f�[�^�����o��
MeshModel.prototype.getBarGeometry=function(){
  var geometry=new THREE.BufferGeometry();
  geometry.param=[];
  geometry.dir=[];
  var elems=this.elements,bars=[],axis=[],i;
  for(i=0;i<elems.length;i++){
    if(elems[i].isBar){
      bars.push(elems[i].border(i,0));
      geometry.param.push(model.barParams[elems[i].param].section);
      axis.push(elems[i].axis);
    }
  }
  var pos=new Float32Array(6*bars.length);
  var colors=new Float32Array(6*bars.length);
  geometry.elements=new Int32Array(2*bars.length);
  geometry.nodes=new Int32Array(2*bars.length);
  geometry.angle=new Float32Array(6*bars.length);
  for(i=0;i<bars.length;i++){
    var i2=2*i,i6=6*i,v=bars[i].nodes,elem=bars[i].element;
    var p1=this.nodes[v[0]],p2=this.nodes[v[1]];
    geometry.dir.push(dirVectors([p1,p2],axis[i]));
    geometry.elements[i2]=elem;
    geometry.elements[i2+1]=elem;
    geometry.nodes[i2]=v[0];
    geometry.nodes[i2+1]=v[1];
    pos[i6]=p1.x;
    pos[i6+1]=p1.y;
    pos[i6+2]=p1.z;
    pos[i6+3]=p2.x;
    pos[i6+4]=p2.y;
    pos[i6+5]=p2.z;
    for(var j=0;j<3;j++){
      colors[i6+j]=meshColors[j];
      colors[i6+j+3]=meshColors[j];
      geometry.angle[i6+j]=0;
      geometry.angle[i6+j+3]=0;
    }
  }
  geometry.addAttribute('position',new THREE.BufferAttribute(pos,3));
  geometry.addAttribute('color',new THREE.BufferAttribute(colors,3));
  return geometry;
};

//--------------------------------------------------------------------//
// �ߓ_
// label - �ߓ_���x��
// x,y,z - x,y,z���W
var FENode=function(label,x,y,z){
  THREE.Vector3.call(this,x,y,z);
  this.label=label;
};

// �ߓ_�̃R�s�[��Ԃ�
FENode.prototype.clone=function(){
  return new this.constructor(this.label,this.x,this.y,this.z);
};

// �ߓ_��\���������Ԃ�
FENode.prototype.toString=function(){
  return 'Node\t'+this.label.toString(10)+'\t'+
      	 this.x+'\t'+this.y+'\t'+this.z;
};

//--------------------------------------------------------------------//
// �ߓ_�W��
// nodes - �ߓ_�ԍ�
var Nodes=function(nodes){
  this.nodes=nodes;
};

// �ߓ_����Ԃ�
Nodes.prototype.nodeCount=function(){
  return this.nodes.length;
};

// �d�S�ʒu��Ԃ�
// p - ���_���W
function center(p){
  var x=0,y=0,z=0,cc=1.0/p.length;
  for(var i=0;i<p.length;i++){
    x+=p[i].x;
    y+=p[i].y;
    z+=p[i].z;
  }
  return new THREE.Vector3(cc*x,cc*y,cc*z);
}

// �@���x�N�g����Ԃ�
// p - ���_���W
function normalVector(p){
  if(p.length<3){
    return null;
  }
  else if((p.length==3) || (p.length==6)){
    return new THREE.Vector3().subVectors(p[1],p[0]).cross
      (new THREE.Vector3().subVectors(p[2],p[0])).normalize();
  }
  else if((p.length==4) || (p.length==8)){
    return new THREE.Vector3().subVectors(p[2],p[0]).cross
      (new THREE.Vector3().subVectors(p[3],p[1])).normalize();
  }
  else{
    var vx=0,vy=0,vz=0;
    for(var i=0;i<p.length;i++){
      var p1=p[(i+1)%p.length],p2=p[(i+2)%p.length];
      var norm=new THREE.Vector3().subVectors(p1,p[i]).cross
      	(new THREE.Vector3().subVectors(p2,p[i]));
      vx+=norm.x;
      vy+=norm.y;
      vz+=norm.z;
    }
    return new THREE.Vector3(vx,vy,vz).normalize();
  }
}

// ���x�����r����
// o1,o2 - ��r����Ώ�
function compareLabel(o1,o2){
  if(o1.label<o2.label)      return -1;
  else if(o1.label>o2.label) return 1;
  else                       return 0;
}

// �s��̘a���v�Z����
// a - ��s��
// da - ������s��
function addMatrix(a,da){
  for(var i=0;i<a.length;i++){
    for(var j=0;j<a[i].length;j++){
      a[i][j]+=da[i][j];
    }
  }
}

// �x�N�g���̘a���v�Z����
// v - ��x�N�g��
// dv - ������x�N�g��
function addVector(v,dv){
  for(var i=0;i<v.length;i++){
    v[i]+=dv[i];
  }
}

inherits(FENode,THREE.Vector3);

//--------------------------------------------------------------------//
// �\�����f��
// canvasId - �\���̈�ID
var ViewModel=function(canvasId){
  this.canvasFrame=document.getElementById(canvasId);		// �`��t���[��
  this.renderer=new THREE.WebGLRenderer({antialias:true});	// �����_���\
  if(!this.renderer){
    alert("three.js �̏������Ɏ��s���܂���");
  }

  this.renderer.setSize(this.canvasFrame.clientWidth,
      	      	      	this.canvasFrame.clientHeight);
  this.canvasFrame.appendChild(this.renderer.domElement);

  this.renderer.setClearColor(0x000000,1);
  this.scene=new THREE.Scene();					// �V�[��
  this.initLight();
  this.initCamera();
  this.axis=null;
};

// ����������������
ViewModel.prototype.initLight=function(){
// ���s����
  this.directionalLight=new THREE.DirectionalLight(0xffffff,1);
  bounds.setLightPosition(this.directionalLight.position);
  this.scene.add(this.directionalLight);
// ������
  this.ambientLight=new THREE.AmbientLight(0x999999);
  this.scene.add(this.ambientLight);
};

// �J����������������
ViewModel.prototype.initCamera=function(){
  var aspect=this.canvasFrame.clientWidth/this.canvasFrame.clientHeight;
  var side=0.7*bounds.size,c=bounds.center;
// �J����
  this.camera=new THREE.OrthographicCamera
    (-side*aspect,side*aspect,side,-side,
     0.01*bounds.size,100*bounds.size);
  this.camera.position.set(c.x,c.y,c.z+bounds.viewPoint);
  this.camera.up.set(0,1,0);
//  this.camera.lookAt({x:0,y:0,z:0});	Controls�g�p���͖����������

// �g���b�N�{�[���R���g���[��
  this.trackball=new THREE.OrthographicTrackballControls
      	      	   (this.camera,this.canvasFrame);
  this.trackball.screen.width=this.canvasFrame.clientWidth;
  this.trackball.screen.height=this.canvasFrame.clientHeight;
  var bdc=this.canvasFrame.getBoundingClientRect();
  this.trackball.screen.offetLeft=bdc.left;
  this.trackball.screen.offetTop=bdc.top;

  this.trackball.noRotate=false;
  this.trackball.rotateSpeed=4.0;
  this.trackball.noZoom=false;
  this.trackball.zoomSpeed=4.0;
  this.trackball.noPan=false;
  this.trackball.panSpeed=1.0;
  this.trackball.target.copy(c);
  this.trackball.staticMoving=true;
  this.trackball.dynamicDampingFactor=0.3;
};

// �\���I�u�W�F�N�g��ǉ�����
ViewModel.prototype.addObject=function(obj){
  this.scene.add(obj);
};

// �\���I�u�W�F�N�g���폜����
ViewModel.prototype.removeObject=function(obj){
  this.scene.remove(obj);
};

// ���W����ݒ肷��
ViewModel.prototype.setAxis=function(){
  if(this.axis!==null){
    this.scene.remove(this.axis);
  }
  this.axis=new THREE.AxisHelper(0.2*bounds.size);
  this.axis.position.set(0,0,0);
  this.scene.add(this.axis);
};

// �����E�J�����ʒu���X�V����
ViewModel.prototype.updateLightAndCamera=function(){
  bounds.setLightPosition(this.directionalLight.position);
  var aspect=this.canvasFrame.clientWidth/this.canvasFrame.clientHeight;
  var side=0.7*bounds.size;
  this.camera.left=-side*aspect;
  this.camera.right=side*aspect;
  this.camera.top=side;
  this.camera.bottom=-side;
  this.camera.near=0.01*bounds.size;
  this.camera.far=100*bounds.size;
  this.trackball.target.copy(bounds.center);
  this.viewZ();
};

// �\�����X�V����
ViewModel.prototype.update=function(){
  this.trackball.update();
  this.renderer.render(this.scene,this.camera);
};

// ���_��X�������ɂ���
ViewModel.prototype.viewX=function(){
  var c=bounds.center;
  this.camera.position.set(c.x+bounds.viewPoint,c.y,c.z);
  this.camera.up.set(0,0,1);
  this.camera.zoom=1;
  this.camera.updateProjectionMatrix();
  this.trackball.target.copy(c);
};

// ���_��Y�������ɂ���
ViewModel.prototype.viewY=function(){
  var c=bounds.center;
  this.camera.position.set(c.x,c.y-bounds.viewPoint,c.z);
  this.camera.up.set(0,0,1);
  this.camera.zoom=1;
  this.camera.updateProjectionMatrix();
  this.trackball.target.copy(c);
};

// ���_��Z�������ɂ���
ViewModel.prototype.viewZ=function(){
  var c=bounds.center;
  this.camera.position.set(c.x,c.y,c.z+bounds.viewPoint);
  this.camera.up.set(0,1,0);
  this.camera.zoom=1;
  this.camera.updateProjectionMatrix();
  this.trackball.target.copy(c);
};

//--------------------------------------------------------------------//
// ���f�����E
var Bounds=function(){
  this.box=new THREE.Box3();
  this.center=new THREE.Vector3();
  this.size=1;
  this.viewPoint=1;
};

// ���f�����E��ݒ肷��
Bounds.prototype.set=function(){
  this.box.setFromPoints(model.mesh.nodes);
  this.center.copy(this.box.getCenter());
  this.size=Math.max(this.box.max.x-this.box.min.x,
      	      	     this.box.max.y-this.box.min.y,
      	      	     this.box.max.z-this.box.min.z);
  this.viewPoint=2*this.size;
};

// �����ʒu��ݒ肷��
// p - �����ʒu
Bounds.prototype.setLightPosition=function(p){
  p.set(this.size,-this.size,this.size);
};

// �x�N�g���ŏ�����
var MIN_VECTOR=1e-8;		// �x�N�g�������̍ŏ��l
// �S�������\���}�e���A��
var REST_MAT=new THREE.MeshBasicMaterial({color:0x0066ff});
var LOAD_COLOR=0x00ff00;	// �׏d�����\���F
var PRESS_COLOR=0xff00ff;	// �ʈ������\���F
var HTC_COLOR=0xffcc00;		// �M�`�B���E�����\���F
// �ߓ_���x�����\���}�e���A��
var TEMP_MAT=new THREE.MeshBasicMaterial({color:0xff3300});
// �v�f�\���}�e���A��
var elemMat=new THREE.MeshStandardMaterial
  ({color:0xffffff,roughness:0.2,metalness:0.5,
    transparent:true,opacity:0.8,
    vertexColors:THREE.VertexColors,side:THREE.DoubleSide});
// �v�f�ӂ̕\���}�e���A��
var EDGE_MAT=new THREE.LineBasicMaterial({color:0xffffff});
// ���v�f�̕\���}�e���A��
var BAR_MAT=new THREE.LineBasicMaterial
  ({color:0xffffff,vertexColors:THREE.VertexColors});
var meshColors=[0.9,0.9,0.9];	// ���b�V���̃f�t�H���g�\���F

//--------------------------------------------------------------------//
// �\���I�u�W�F�N�g
var ViewObject=function(){
  this.mesh=null;	// �v�Z���b�V��
  this.edge=null;	// �v�f��
  this.bar=null;	// ���v�f
  this.rest=null;	// �S������
  this.load=null;	// �׏d����
  this.press=null;	// �ʈ�����
  this.htc=null;	// �M�`�B���E����
  this.temp=null;	// �ߓ_���x����
  this.showEdge=true;	// �v�f�ӕ\���X�C�b�`
};

// �\���I�u�W�F�N�g���쐬����
ViewObject.prototype.create=function(){
  var geometry=model.mesh.getGeometry();
  this.mesh=new THREE.Mesh(geometry,elemMat);
  viewModel.addObject(this.mesh);
  geometry=model.mesh.getEdgeGeometry();
  this.edge=new THREE.LineSegments(geometry,EDGE_MAT);
  viewModel.addObject(this.edge);
  geometry=model.mesh.getBarGeometry();
  this.bar=new BarObject(geometry);
  viewModel.addObject(this.bar);
  this.createRestraint();
  this.createLoad();
  this.createPress();
  this.createHTC();
  this.createTemperature();
  this.setEdgeView();
};

// �S�������\���I�u�W�F�N�g���쐬����
ViewObject.prototype.createRestraint=function(){
  var hs=0.02*bounds.size,rests=model.bc.restraints;
  this.rest=new THREE.Group();
  for(var i=0;i<rests.length;i++){
    var r=new RestraintHelper(rests[i],hs);
    r.position.copy(model.mesh.nodes[rests[i].node]);
    this.rest.add(r);
  }
  viewModel.addObject(this.rest);
};

// �׏d�����\���I�u�W�F�N�g���쐬����
ViewObject.prototype.createLoad=function(){
  var coef=0.1*bounds.size/Math.max(model.bc.loadMax,MIN_VECTOR);
  var hl=0.02*bounds.size,hw=0.5*hl,loads=model.bc.loads;
  this.load=new THREE.Group();
  for(var i=0;i<loads.length;i++){
    var ld=loads[i];
    var l=ld.magnitude();
    if(l>=MIN_VECTOR){
      var gx=ld.globalX;
      var dir=new THREE.Vector3(gx[0],gx[1],gx[2]).normalize();
      l=Math.max(coef*l,MIN_VECTOR+hl);
      var arrow=new THREE.ArrowHelper
      	(dir,model.mesh.nodes[ld.node],l,LOAD_COLOR,hl,hw);
      this.load.add(arrow);
    }
  }
  viewModel.addObject(this.load);
};

// �ʈ������\���I�u�W�F�N�g���쐬����
ViewObject.prototype.createPress=function(){
  var coef=0.1*bounds.size/Math.max(model.bc.pressMax,MIN_VECTOR);
  var hl=0.02*bounds.size,hw=0.5*hl,prs=model.bc.pressures;
  this.press=new THREE.Group();
  for(var i=0;i<prs.length;i++){
    var ps=prs[i];
    var eb=ps.getBorder(model.mesh.elements[ps.element]);
    var p=model.mesh.getNodes(eb);
    var c=center(p),norm=normalVector(p);
    var l=ps.press;
    if(l>=MIN_VECTOR){
      var dir=norm.clone().negate();
      l=Math.max(coef*l,MIN_VECTOR+hl);
      var p0=c.clone().addScaledVector(dir,-l);
      var arrow=new THREE.ArrowHelper(dir,p0,l,PRESS_COLOR,hl,hw);
      this.press.add(arrow);
    }
  }
  viewModel.addObject(this.press);
};

// �M�`�B���E�����\���I�u�W�F�N�g���쐬����
ViewObject.prototype.createHTC=function(){
  var l=0.1*bounds.size;
  var hl=0.04*bounds.size,hw=0.5*hl,htcs=model.bc.htcs;
  this.htc=new THREE.Group();
  for(var i=0;i<htcs.length;i++){
    var ht=htcs[i],elem=model.mesh.elements[ht.element];
    var eb=ht.getBorder(elem);
    var p=model.mesh.getNodes(eb);
    var c=center(p),norm=normalVector(p);
    if(eb.isEdge){
      norm=eb.normalVector(p,model.mesh.getNodes(elem));
    }
    var dir=norm.clone().negate();
    var p0=c.clone().addScaledVector(dir,-l);
    var arrow=new THREE.ArrowHelper(dir,p0,l,HTC_COLOR,hl,hw);
    this.htc.add(arrow);
  }
  viewModel.addObject(this.htc);
};

// �ߓ_���x�\���I�u�W�F�N�g���쐬����
ViewObject.prototype.createTemperature=function(){
  var rr=0.01*bounds.size,temps=model.bc.temperature;
  this.temp=new THREE.Group();
  for(var i=0;i<temps.length;i++){
    var mesh=new THREE.Mesh(new THREE.SphereGeometry(rr),TEMP_MAT);
    mesh.position.copy(model.mesh.nodes[temps[i].node]);
    this.temp.add(mesh);
  }
  viewModel.addObject(this.temp);
};

// �ψʂ�ݒ肷��
// disp - �ψ�
// coef - �\���W��
ViewObject.prototype.setDisplacement=function(disp,coef){
  if(disp.length===0) return;
  setGeomDisplacement(this.mesh.geometry,disp,coef);
  setGeomDisplacement(this.edge.geometry,disp,coef);
  this.bar.setDisplacement(disp,coef);
};

// �R���^�[�}��ݒ肷��
// value - �R���^�[�}�f�[�^
// minValue,maxValue - �R���^�[�}�f�[�^�ŏ��l,�ő�l
// type - �f�[�^�ێ��`��
ViewObject.prototype.setContour=function(value,minValue,maxValue,type){
  var coef=1;
  if(maxValue!==minValue) coef=1/(maxValue-minValue);
  setGeomContour(this.mesh.geometry,value,minValue,coef,type);
  this.bar.setContour(value,minValue,coef,type);
};

// ���ʕ\������������
ViewObject.prototype.removeResult=function(){
  removeGeomResult(this.mesh.geometry);
  this.bar.removeResult();

  var geom=this.edge.geometry,label=geom.nodes,angle=geom.angle;
  var pos=geom.attributes.position.array;
  for(var i=0;i<label.length;i++){
    var i3=3*i,p=model.mesh.nodes[label[i]];
    pos[i3]=p.x;
    pos[i3+1]=p.y;
    pos[i3+2]=p.z;
    angle[i3]=0;
    angle[i3+1]=0;
    angle[i3+2]=0;
  }
  geom.attributes.position.needsUpdate=true;
};

// �R���^�[�}����������
ViewObject.prototype.clearContour=function(){
  clearGeomContour(this.mesh.geometry);
  this.bar.clearContour();
};

// �v�f�ӕ\����؂�ւ���
// showEdge - �v�f�ӂ�\������ꍇ��true
ViewObject.prototype.setShowEdge=function(showEdge){
  this.showEdge=showEdge;
  this.setEdgeView();
};

// �v�f�ӕ\����؂�ւ���
ViewObject.prototype.setEdgeView=function(){
  if(this.edge!==null){
    this.edge.visible=this.showEdge;
  }
  if(this.showEdge){
    elemMat.opacity=0.8;
  }
  else{
    elemMat.opacity=1;
  }
};

// �\���I�u�W�F�N�g���폜����
ViewObject.prototype.remove=function(){
  var i,child;
  if(this.mesh!==null){
    viewModel.removeObject(this.mesh);
    this.mesh.geometry.dispose();
    this.mesh.material.dispose();
    this.mesh=null;
  }
  if(this.edge!==null){
    viewModel.removeObject(this.edge);
    this.edge.geometry.dispose();
    this.edge.material.dispose();
    this.edge=null;
  }
  if(this.bar!==null){
    this.bar.removeObject();
    this.bar=null;
  }
  if(this.rest!==null){
    for(i=this.rest.children.length-1;i>=0;i--){
      this.rest.children[i].removeChildren();
      this.rest.remove(this.rest.children[i]);
    }
    this.rest=null;
  }
  if(this.load!==null){
    this.removeAllows(this.load);
    this.load=null;
  }
  if(this.press!==null){
    this.removeAllows(this.press);
    this.press=null;
  }
  if(this.htc!==null){
    this.removeAllows(this.htc);
    this.htc=null;
  }
  if(this.temp!==null){
    viewModel.removeObject(this.temp);
    for(i=this.temp.children.length-1;i>=0;i--){
      child=this.temp.children[i];
      child.geometry.dispose();
      child.material.dispose();
      this.temp.remove(child);
    }
    this.temp=null;
  }
};

// ���W���̕\���I�u�W�F�N�g���폜����
// grp - ���W��
ViewObject.prototype.removeAllows=function(grp){
  viewModel.removeObject(grp);
  for(var i=grp.children.length-1;i>=0;i--){
    var child=grp.children[i];
    child.line.geometry.dispose();
    child.line.material.dispose();
    child.cone.geometry.dispose();
    child.cone.material.dispose();
    grp.remove(child);
  }
};

// �ψʂ�ݒ肷��
// geometry - ���W��ݒ肷��`��f�[�^
// disp - �ψ�
// coef - �\���W��
function setGeomDisplacement(geometry,disp,coef){
  var label=geometry.nodes,nodes=model.mesh.nodes,angle=geometry.angle;
  var pos=geometry.attributes.position.array;
  for(var i=0;i<label.length;i++){
    var i3=3*i,p=nodes[label[i]],dx=disp[label[i]].x;
    pos[i3]=p.x+coef*dx[0];
    pos[i3+1]=p.y+coef*dx[1];
    pos[i3+2]=p.z+coef*dx[2];
    angle[i3]=coef*dx[3];
    angle[i3+1]=coef*dx[4];
    angle[i3+2]=coef*dx[5];
  }
  geometry.attributes.position.needsUpdate=true;
}

// �`��f�[�^�̃R���^�[�}��ݒ肷��
// geometry - �ΏۂƂȂ�`��f�[�^
// value - �R���^�[�}�f�[�^
// minValue - �R���^�[�}�f�[�^�ŏ��l
// coef - �f�[�^�ϊ��W��
// type - �f�[�^�ێ��`��
function setGeomContour(geometry,value,minValue,coef,type){
  var label,colors=geometry.attributes.color.array;
  if(type===ELEMENT_DATA){
    label=geometry.elements;
  }
  else{
    label=geometry.nodes;
  }
  for(var i=0;i<label.length;i++){
    var i3=3*i,d=coef*(value[label[i]]-minValue);
    var cls=contourColor(d);
    colors[i3]=cls[0];
    colors[i3+1]=cls[1];
    colors[i3+2]=cls[2];
  }
  geometry.attributes.color.needsUpdate=true;
}

// �`��f�[�^�̌��ʕ\������������
// geometry - �ΏۂƂȂ�`��f�[�^
function removeGeomResult(geometry){
  var label=geometry.nodes,nodes=model.mesh.nodes,angle=geometry.angle;
  var pos=geometry.attributes.position.array;
  var colors=geometry.attributes.color.array;
  for(var i=0;i<label.length;i++){
    var i3=3*i,p=nodes[label[i]];
    pos[i3]=p.x;
    pos[i3+1]=p.y;
    pos[i3+2]=p.z;
    colors[i3]=meshColors[0];
    colors[i3+1]=meshColors[1];
    colors[i3+2]=meshColors[2];
    angle[i3]=0;
    angle[i3+1]=0;
    angle[i3+2]=0;
  }
  geometry.attributes.position.needsUpdate=true;
  geometry.attributes.color.needsUpdate=true;
}

// �`��f�[�^�̃R���^�[�}����������
// geometry - �ΏۂƂȂ�`��f�[�^
function clearGeomContour(geometry){
  var colors=geometry.attributes.color.array;
  for(var i=0;i<colors.length;i+=3){
    colors[i]=meshColors[0];
    colors[i+1]=meshColors[1];
    colors[i+2]=meshColors[2];
  }
  geometry.attributes.color.needsUpdate=true;
}

//--------------------------------------------------------------------//
// �S�������\���I�u�W�F�N�g
// rest - �S������
// size - �\���T�C�Y
var RestraintHelper=function(rest,size){
  THREE.Group.call(this);
  var geom;
  if(rest.rest[0]){
    geom=new THREE.CylinderBufferGeometry(0,0.5*size,size,5,1);
    geom.translate(0,-0.5*size,0);
    geom.rotateZ(0.5*Math.PI);
    this.add(new THREE.Mesh(geom,REST_MAT));
  }
  if(rest.rest[1]){
    geom=new THREE.CylinderBufferGeometry(0,0.5*size,size,5,1);
    geom.translate(0,-0.5*size,0);
    geom.rotateX(Math.PI);
    this.add(new THREE.Mesh(geom,REST_MAT));
  }
  if(rest.rest[2]){
    geom=new THREE.CylinderBufferGeometry(0,0.5*size,size,5,1);
    geom.translate(0,-0.5*size,0);
    geom.rotateX(-0.5*Math.PI);
    this.add(new THREE.Mesh(geom,REST_MAT));
  }
  if(rest.rest[3]){
    geom=new THREE.CylinderBufferGeometry(0,0.3*size,2*size,5,1);
    geom.translate(0,size,0);
    geom.rotateZ(-0.5*Math.PI);
    this.add(new THREE.Mesh(geom,REST_MAT));
  }
  if(rest.rest[4]){
    geom=new THREE.CylinderBufferGeometry(0,0.3*size,2*size,5,1);
    geom.translate(0,size,0);
    this.add(new THREE.Mesh(geom,REST_MAT));
  }
  if(rest.rest[5]){
    geom=new THREE.CylinderBufferGeometry(0,0.3*size,2*size,5,1);
    geom.translate(0,size,0);
    geom.rotateX(0.5*Math.PI);
    this.add(new THREE.Mesh(geom,REST_MAT));
  }
  if(rest.coords){
    var e=rest.coords.c.elements;
    var e2=[e[0],e[1],e[2],0,e[3],e[4],e[5],0,e[6],e[7],e[8],0,0,0,0,1];
    this.applyMatrix(new THREE.Matrix4().fromArray(e2));
  }
};

// �\���I�u�W�F�N�g���폜����
RestraintHelper.prototype.removeChildren=function(){
  for(var i=this.children.length-1;i>=0;i--){
    var child=this.children[i];
    child.geometry.dispose();
    child.material.dispose();
    this.remove(child);
  }
};

//--------------------------------------------------------------------//
// ���v�f�\���I�u�W�F�N�g
// geometry - �v�f���S���̌`��f�[�^
var BarObject=function(geometry){
  THREE.Group.call(this);
  this.center=new THREE.LineSegments(geometry,BAR_MAT);
  this.add(this.center);
  var param=geometry.param;
  var count=2*param.length;	// �P�v�f�ɒf�ʂQ�i�����ƊO���j
  for(var i=0;i<count;i++){
    var geom=new THREE.BufferGeometry();
    var points=3*param[parseInt(i/2)].vertexCount()+3;
    var pos=new Float32Array(points);
    var colors=new Float32Array(points);
    for(var j=0;j<points;j++){
      pos[j]=0;
      colors[j]=meshColors[j%3];
    }
    geom.addAttribute('position',new THREE.BufferAttribute(pos,3));
    geom.addAttribute('color',new THREE.BufferAttribute(colors,3));
    this.add(new THREE.Line(geom,BAR_MAT));
  }
  this.setSection();
};

// �f�ʂ̈ʒu��ݒ肷��
BarObject.prototype.setSection=function(){
  var param=this.center.geometry.param;
  var dir=this.center.geometry.dir,angle=this.center.geometry.angle;
  var v=this.center.geometry.attributes.position.array;
  var v1=new THREE.Vector3(),v2=new THREE.Vector3();
  var axis=new THREE.Vector3();
  for(var i=0;i<param.length;i++){
    var i6=6*i;
    var geom1=this.children[2*i+1].geometry;
    var geom2=this.children[2*i+2].geometry;
    var pos1=geom1.attributes.position.array;
    var pos2=geom2.attributes.position.array;
    var cx=0.5*(v[i6]+v[i6+3]);
    var cy=0.5*(v[i6+1]+v[i6+4]);
    var cz=0.5*(v[i6+2]+v[i6+5]);
    var wx=0.5*(angle[i6]+angle[i6+3]);
    var wy=0.5*(angle[i6+1]+angle[i6+4]);
    var wz=0.5*(angle[i6+2]+angle[i6+5]);
    v1.set(v[i6+3]-v[i6],v[i6+4]-v[i6+1],v[i6+5]-v[i6+2]).normalize();
    v2.copy(dir[i][1]);
    var ww=Math.sqrt(wx*wx+wy*wy+wz*wz);
    if(ww>0){
      v2.applyAxisAngle(axis.set(wx/ww,wy/ww,wz/ww),ww);
    }
    param[i].setCoords(pos1,pos2,cx,cy,cz,v1,v2);
    geom1.attributes.position.needsUpdate=true;
    geom2.attributes.position.needsUpdate=true;
  }
};

// �ψʂ�ݒ肷��
// disp - �ψ�
// coef - �\���W��
BarObject.prototype.setDisplacement=function(disp,coef){
  setGeomDisplacement(this.center.geometry,disp,coef);
  this.setSection();
};

// �R���^�[�}��ݒ肷��
// value - �R���^�[�}�f�[�^
// minValue - �R���^�[�}�f�[�^�ŏ��l
// coef - �f�[�^�ϊ��W��
// type - �f�[�^�ێ��`��
BarObject.prototype.setContour=function(value,minValue,coef,type){
  setGeomContour(this.center.geometry,value,minValue,coef,type);
};

// ���ʕ\������������
BarObject.prototype.removeResult=function(){
  removeGeomResult(this.center.geometry);
  this.setSection();
};

// �R���^�[�}����������
BarObject.prototype.clearContour=function(){
  clearGeomContour(this.center.geometry);
};

// �\���I�u�W�F�N�g���폜����
BarObject.prototype.removeObject=function(){
  viewModel.removeObject(this);
  for(var i=this.children.length-1;i>=0;i--){
    var child=this.children[i];
    child.geometry.dispose();
    child.material.dispose();
    this.remove(child);
  }
};

//--------------------------------------------------------------------//
// ���f���\���ݒ�
var ViewConfig=function(){
  var canvas=document.getElementById('el-color');	// �v�f�\���F�ݒ�p
  canvas.width=canvas.clientWidth;
  canvas.height=canvas.clientHeight;
  this.cpicker=new ColorPicker(canvas);			// �J���[�s�b�J�[
  this.showEdge=document.getElementById('showedge');	// �v�f�ӕ\��
  this.lightx=document.getElementById('lightx');	// �����ʒu�ݒ�p
  this.lighty=document.getElementById('lighty');
  this.lightz=document.getElementById('lightz');
};

// �R���t�B�O�E�B���h�E��\������
ViewConfig.prototype.show=function(){
  this.cpicker.setColor(meshColors);
  var lp=viewModel.directionalLight.position;
  this.lightx.value=numString(lp.x);
  this.lighty.value=numString(lp.y);
  this.lightz.value=numString(lp.z);
};

// �R���t�B�O��ݒ肷��
ViewConfig.prototype.set=function(){
  meshColors=this.cpicker.getColor();
  if(parseInt(resultView.contour.value)<0){
    viewObj.clearContour();
  }
  viewObj.setShowEdge(this.showEdge.checked);
  var lp=viewModel.directionalLight.position;
  lp.set(parseFloat(this.lightx.value),parseFloat(this.lighty.value),
      	 parseFloat(this.lightz.value));
};

// �R���t�B�O�E�B���h�E��\������
function showConfig(){
  showModalWindow(CONFIG_WINDOW);
  viewConfig.show();
}

// �R���t�B�O��ݒ肷��
function setConfig(){
  hideModalWindow(CONFIG_WINDOW);
  viewConfig.set();
}

// �R���t�B�O��������
function cancelConfig(){
  hideModalWindow(CONFIG_WINDOW);
}

inherits(RestraintHelper,THREE.Group);
inherits(BarObject,THREE.Group);

var cls=[];					// �R���^�[�}�F
var CBAR_FONT_SIZE=14;				// �t�H���g�T�C�Y
var CBAR_FONT=CBAR_FONT_SIZE+"px 'Arial'";	// �t�H���g
var FG_COLOR='#ffffff';				// �O�i�F

//--------------------------------------------------------------------//
// �J���[�o�[
// canvasId - �\���̈�ID
var ColorBar=function(canvasId){
  this.canvas=document.getElementById(canvasId);	// �`��L�����o�X
  this.canvas.width=this.canvas.clientWidth;
  this.canvas.height=this.canvas.clientHeight;
  this.context=this.canvas.getContext('2d');		// �R���e�L�X�g
  this.context.font=CBAR_FONT;
  this.context.textBaseline='top';
  this.rect={x:parseInt(this.canvas.width/5),		// �J���[�o�[�̕`��̈�
      	     y:parseInt(this.canvas.height/10),
      	     width:parseInt(this.canvas.width*3/10),
      	     height:parseInt(this.canvas.height*4/5)};
  this.gradient=this.context.createLinearGradient
    (this.rect.x,this.rect.y+this.rect.height,this.rect.x,this.rect.y);
  this.gradient.addColorStop(0,'#0000ff');
  this.gradient.addColorStop(0.25,'#00ffff');
  this.gradient.addColorStop(0.5,'#00cc00');
  this.gradient.addColorStop(0.75,'#ffff00');
  this.gradient.addColorStop(1,'#ff0000');
  var xt=this.rect.x+this.rect.width+5;
// ���l�\���ʒu
  this.maxPos={x:xt,y:parseInt(this.rect.y-CBAR_FONT_SIZE/2)};
  this.minPos={x:xt,y:this.maxPos.y+this.rect.height};
};

// �R���^�[�}��`�悷��
// minValue - �R���^�[�}�f�[�^�ŏ��l
// maxValue - �R���^�[�}�f�[�^�ő�l
ColorBar.prototype.draw=function(minValue,maxValue){
  this.context.clearRect(0,0,this.canvas.width,this.canvas.height);
  this.context.fillStyle=this.gradient;
  this.context.fillRect(this.rect.x,this.rect.y,
      	      	      	this.rect.width,this.rect.height);
  this.context.strokeStyle=FG_COLOR;
  this.context.strokeRect(this.rect.x,this.rect.y,
      	      	      	  this.rect.width,this.rect.height);
  this.context.fillStyle=FG_COLOR;
  this.context.fillText(numString(maxValue),this.maxPos.x,this.maxPos.y);
  this.context.fillText(numString(minValue),this.minPos.x,this.minPos.y);
};

// �R���^�[�}����������
ColorBar.prototype.clear=function(){
  this.context.clearRect(0,0,this.canvas.width,this.canvas.height);
};

// �R���^�[�}�̐F��Ԃ�
// z - 0�`1�̒l
function contourColor(z){
  cls[0]=0;
  cls[1]=0;
  cls[2]=0;
  if(z<=0){
    cls[2]=1;
  }
  else if(z<0.25){
    cls[1]=4*z;
    cls[2]=1;
  }
  else if(z<0.5){
    cls[1]=1.2-0.8*z;
    cls[2]=2-4*z;
  }
  else if(z<0.75){
    cls[0]=4*z-2;
    cls[1]=0.4+0.8*z;
  }
  else if(z<1){
    cls[0]=1;
    cls[1]=4-4*z;
  }
  else{
    cls[0]=1;
  }
  return cls;
}

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

var CIRCLE_DIV=16;			// �~�`�f�ʕ\���I�u�W�F�N�g�̕�����
var CIRCLE_DTH=2*Math.PI/CIRCLE_DIV;	// �~�`�f�ʂ̂P�����̊p�x
var COEF_K1=64/Math.pow(Math.PI,5);	// ��`�f�ʂ̝���W��
var COEF_K=8/(Math.PI*Math.PI);

//--------------------------------------------------------------------//
// �~�`�f��
// ss - �f�[�^������
var CircleSection=function(ss){
  this.d1=parseFloat(ss[0]);	// �O�a
  this.d2=parseFloat(ss[1]);	// ���a
// �f�ʐ�
  this.area=0.25*Math.PI*(this.d1*this.d1-this.d2*this.d2);
// �f�ʂQ�����[�����g
  this.iy=0.0625*this.area*(this.d1*this.d1+this.d2*this.d2);
  this.iz=this.iy;
  this.ip=this.iy+this.iz;	// �f�ʂQ���Ƀ��[�����g
};

// �f�ʂ̒��_����Ԃ�
CircleSection.prototype.vertexCount=function(){
  return CIRCLE_DIV;
};

// ����f�␳�W����Ԃ�
CircleSection.prototype.shearCoef=function(){
  return KS_CIRCLE;
};

// �c�E���̓x�N�g����Ԃ�
// material - �ޗ�
// ex - �������k�c
// thd - �䝀��p
// kpy,kpz - �Ȃ��ɂ��ȗ�
// sy,sz - �f�ʂ���f�c
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

// �f�ʕ\�����f���̍��W�n��ݒ肷��
// pos1,pos2 - �O�a�C���a�̍��W
// cx,cy,cz - ���S�_���W
// v1,v2 - �������C�f�ʊ�����x�N�g��
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

// ���ʁE�d�S����̊������[�����g��Ԃ�
// dens - ���x
// l - �v�f����
CircleSection.prototype.massInertia=function(dens,l){
  var dl=dens*l,dly=dl*this.iy;
  return [dl*this.area,2*dly,dly,dly];
};

// �f�ʂ�\���������Ԃ�
CircleSection.prototype.toString=function(){
  return this.d1+'\t'+this.d2;
};

//--------------------------------------------------------------------//
// ��`�f��
// ss - �f�[�^������
var RectSection=function(ss){
  var b1=parseFloat(ss[0]),h1=parseFloat(ss[1]);
  var b2=parseFloat(ss[2]),h2=parseFloat(ss[3]);
  this.b1=b1;	// �O����
  this.h1=h1;	// �O������
  this.b2=b2;	// ������
  this.h2=h2;	// ��������
// �f�ʐ�
  this.area=b1*h1-b2*h2;
// �f�ʂQ�����[�����g
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
  this.ip=ip1-ip2;		// �f�ʂQ���Ƀ��[�����g
};

// �f�ʂ̒��_����Ԃ�
RectSection.prototype.vertexCount=function(){
  return 4;
};

// ����f�␳�W����Ԃ�
RectSection.prototype.shearCoef=function(){
  return KS_RECT;
};

// �c�E���̓x�N�g����Ԃ�
// material - �ޗ�
// ex - �������k�c
// thd - �䝀��p
// kpy,kpz - �Ȃ��ɂ��ȗ�
// sy,sz - �f�ʂ���f�c
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

// �f�ʕ\�����f���̍��W�n��ݒ肷��
// pos1,pos2 - �O�a�C���a�̍��W
// cx,cy,cz - ���S�_���W
// v1,v2 - �������C�f�ʊ�����x�N�g��
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

// ���ʁE�d�S����̊������[�����g��Ԃ�
// dens - ���x
// l - �v�f����
RectSection.prototype.massInertia=function(dens,l){
  var dl=dens*l,dly=dl*this.iz,dlz=dl*this.iy;
  return [dl*this.area,dly+dlz,dly,dlz];
};

// �f�ʂ�\���������Ԃ�
RectSection.prototype.toString=function(){
  return this.b1+'\t'+this.h1+'\t'+this.b2+'\t'+this.h2;
};

// ��`�f�ʂ̝���W�������߂�
// ba - �ӂ̒�����b/a
function rectCoef(ba){
  var dk1s=0,dks=0,dbs=0,pba=0.5*Math.PI*ba;
  var i=1,dk1,dk,db,ex,sg=1;
  do{
    ex=Math.exp(-2*pba*i);
    dk1=(1-ex)/((i+ex)*Math.pow(i,5));	// IE�͑o�Ȑ��֐�������
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

//--------------------------------------------------------------------//
// �Ǐ����W�n
// label - ���W�n���x��
// n11,n12,n13,n21,n22,n23,n31,n32,n33 - ���W�ϊ��}�g���b�N�X�̐���
var Coordinates=function(label,n11,n12,n13,n21,n22,n23,n31,n32,n33){
  this.label=label;
  this.c=new THREE.Matrix3().set(n11,n12,n13,n21,n22,n23,n31,n32,n33);
};

// �O���[�o�����W�n�ɕϊ�����
// x - �x�N�g������
Coordinates.prototype.toGlobal=function(x){
  var y=[],e=this.c.elements;
  for(var i=0;i<3;i++){
    y[i]=e[i]*x[0]+e[i+3]*x[1]+e[i+6]*x[2];
    y[i+3]=e[i]*x[3]+e[i+3]*x[4]+e[i+6]*x[5];
  }
  return y;
};

// �����}�g���b�N�X��ϊ�����
// matrix - �����}�g���b�N�X
// dof - �}�g���b�N�X�̎��R�x
// idx0 - �ߓ_�|�C���^
// ndof - �ߓ_���R�x
Coordinates.prototype.transMatrix=function(matrix,dof,idx0,ndof){
  var e=this.c.elements,i,j,k,me,mi1,mi2,mi3;
  for(i=0;i<dof;i+=3){
    mi1=matrix[i];
    mi2=matrix[i+1];
    mi3=matrix[i+2];
    for(j=idx0;j<idx0+ndof;j+=3){
      if(j in matrix[i]){
      	me=[mi1[j],mi2[j],mi3[j],mi1[j+1],mi2[j+1],mi3[j+1],
      	    mi1[j+2],mi2[j+2],mi3[j+2]];
      	for(k=0;k<3;k++){
      	  var ke3=3*k,e1=e[ke3],e2=e[ke3+1],e3=e[ke3+2];
      	  mi1[j+k]=me[0]*e1+me[3]*e2+me[6]*e3;
      	  mi2[j+k]=me[1]*e1+me[4]*e2+me[7]*e3;
      	  mi3[j+k]=me[2]*e1+me[5]*e2+me[8]*e3;
      	}
      }
    }
  }
  for(i=idx0;i<idx0+ndof;i+=3){
    mi1=matrix[i];
    mi2=matrix[i+1];
    mi3=matrix[i+2];
    for(j=0;j<dof;j+=3){
      if(j in matrix[i]){
      	me=[mi1[j],mi2[j],mi3[j],mi1[j+1],mi2[j+1],mi3[j+1],
      	    mi1[j+2],mi2[j+2],mi3[j+2]];
      	for(k=0;k<3;k++){
      	  var km3=3*k,me1=me[km3],me2=me[km3+1],me3=me[km3+2];
      	  mi1[j+k]=e[0]*me1+e[1]*me2+e[2]*me3;
      	  mi2[j+k]=e[3]*me1+e[4]*me2+e[5]*me3;
      	  mi3[j+k]=e[6]*me1+e[7]*me2+e[8]*me3;
      	}
      }
    }
  }
};

// �׏d�x�N�g����ϊ�����
// vector - �׏d�x�N�g��
// dof - �}�g���b�N�X�̎��R�x
// idx0 - �ߓ_�|�C���^
// ndof - �ߓ_���R�x
Coordinates.prototype.transVector=function(vector,dof,idx0,ndof){
  var e=this.c.elements;
  for(var j=idx0;j<idx0+ndof;j+=3){
    var x1=vector[j],x2=vector[j+1],x3=vector[j+2];
    vector[j]=e[0]*x1+e[1]*x2+e[2]*x3;
    vector[j+1]=e[3]*x1+e[4]*x2+e[5]*x3;
    vector[j+2]=e[6]*x1+e[7]*x2+e[8]*x3;
  }
};

// �Ǐ����W�n��\���������Ԃ�
Coordinates.prototype.toString=function(){
  return 'Coordinates\t'+this.label.toString(10)+'\t'+
      	  this.c.elements.join('\t');
};

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

// �O�p�`2���v�f�̃K�E�X�ϕ��̐ϕ��_���W
var GTRI2=[1/6,2/3];
// �l�ʑ�2���v�f�̃K�E�X�ϕ��̐ϕ��_���W
var GTETRA2=[0.25-0.05*Math.sqrt(5),0.25+0.15*Math.sqrt(5)];
// �l�p�`1���v�f�̃K�E�X�ϕ��̐ϕ��_���W
var GX2=[-1/Math.sqrt(3),1/Math.sqrt(3)];
// �l�p�`2���v�f�̃K�E�X�ϕ��̐ϕ��_���W
var GX3=[-Math.sqrt(0.6),0,Math.sqrt(0.6)];
// �K�E�X�ϕ��̏d�݌W��
var GW3=[5/9,8/9,5/9];
var C1_3=1/3,C1_6=1/6,C1_12=1/12,C1_24=1/24;	// 1/3,1/6,1/12,1/24

//--------------------------------------------------------------------//
// �v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var FElement=function(label,material,nodes){
  Nodes.call(this,nodes);
  this.label=label;
  this.material=material;
  this.isShell=false;		// �V�F���v�f�ł͂Ȃ�
  this.isBar=false;		// ���v�f�ł͂Ȃ�
};

// �v�f���E��Ԃ�
// element - �v�f���x��
FElement.prototype.borders=function(element){
  var count=this.borderCount(),borders=[];
  for(var i=0;i<count;i++) borders[i]=this.border(element,i);
  return borders;
};

// �ϕ��_�̍����}�g���b�N�X��Ԃ�
// d - ����-�c�}�g���b�N�X
// b - �c-�ψʃ}�g���b�N�X�̓]�u�s��
// coef - �W��
FElement.prototype.stiffPart=function(d,b,coef){
  var size1=b.length,size2=d.length,a=[],k=[],j;
  for(var i=0;i<size1;i++){
    a.length=0;
    var bi=b[i];
    for(j=0;j<size2;j++){
      a[j]=coef*numeric.dotVV(bi,d[j]);
    }
    var ki=[];
    for(j=0;j<size1;j++){
      ki[j]=numeric.dotVV(a,b[j]);
    }
    k[i]=ki;
  }
  return k;
};

// �ߓ_�ψʂ�1�����z��ɕϊ�����
// u - �ߓ_�ψ�
// dof - �ߓ_���R�x
FElement.prototype.toArray=function(u,dof){
  var count=this.nodeCount(),v=[];
  for(var i=0;i<count;i++){
    var ux=u[i].x;
    for(var j=0;j<dof;j++){
      v.push(ux[j]);
    }
  }
  return v;
};

// �ߓ_�ψʂ��Ǐ����W�n�E1�����z��ɕϊ�����
// u - �ߓ_�ψ�
// d - �����]���}�g���b�N�X
FElement.prototype.toLocalArray=function(u,d){
  var v=[],j;
  for(var i=0;i<2;i++){
    var ux=u[i].x;
    for(j=0;j<3;j++){
      v.push(d[0][j]*ux[0]+d[1][j]*ux[1]+d[2][j]*ux[2]);
    }
    for(j=0;j<3;j++){
      v.push(d[0][j]*ux[3]+d[1][j]*ux[4]+d[2][j]*ux[5]);
    }
  }
  return v;
};

// �ߓ_�����ւ���
// i1,i2 - �ߓ_�C���f�b�N�X
function swap(nodes,i1,i2){
  var t=nodes[i1];
  nodes[i1]=nodes[i2];
  nodes[i2]=t;
}

// ���ʏ�̊p�x�����߂�
// p0 - ��_
// p1,p2 - ���_
function planeAngle(p0,p1,p2){
  var v1=p1.clone().sub(p0).normalize();
  var v2=p2.clone().sub(p0).normalize();
  return Math.acos(Math.min(Math.max(v1.dot(v2),0),1));
}

// �O�p�`�̗��̊p�����ʉߏ肩�狁�߂�
// p0 - ��_
// p1,p2,p3 - ���_
function solidAngle(p0,p1,p2,p3){
  var v1=p1.clone().sub(p0);
  var v2=p2.clone().sub(p0);
  var v3=p3.clone().sub(p0);
  var v12=v1.clone().cross(v2).normalize();
  var v23=v2.clone().cross(v3).normalize();
  var v31=v3.clone().cross(v1).normalize();
  var max=Math.max,min=Math.min,acos=Math.acos;
  var a1=max(min(-v12.dot(v31),1),-1);
  var a2=max(min(-v23.dot(v12),1),-1);
  var a3=max(min(-v31.dot(v23),1),-1);
  return acos(a1)+acos(a2)+acos(a3)-Math.PI;
}

// �����]���}�g���b�N�X��Ԃ�
// p - ���_���W
// axis - �f�ʊ�����x�N�g��
function dirMatrix(p,axis){
  var v=dirVectors(p,axis);
  return [[v[0].x,v[1].x,v[2].x],[v[0].y,v[1].y,v[2].y],
      	  [v[0].z,v[1].z,v[2].z]];
}

// �����]���}�g���b�N�X��Ԃ�
// p - ���_���W
// axis - �f�ʊ�����x�N�g��
function dirVectors(p,axis){
  var v1,v2,v3;
  if(p.length==2){		// ���v�f
    v1=p[1].clone().sub(p[0]).normalize();
    v2=new THREE.Vector3();
    v3=new THREE.Vector3();
    if((axis!==null) && (axis!==undefined)){
      var dt=v1.dot(axis);
      v2.set(axis.x-dt*v1.x,axis.y-dt*v1.y,axis.z-dt*v1.z);
      if(v2.lengthSq()>0) v2.normalize();
    }
    if(v2.lengthSq()===0){
      if(Math.abs(v1.x)<Math.abs(v1.y)){
      	v2.set(1-v1.x*v1.x,-v1.x*v1.y,-v1.x*v1.z).normalize();
      }
      else{
      	v2.set(-v1.y*v1.x,1-v1.y*v1.y,-v1.y*v1.z).normalize();
      }
    }
    v3.crossVectors(v1,v2);
    return [v1,v2,v3];
  }
  else if(p.length>2){		// �V�F���v�f
    v3=normalVector(p);
    v2=p[1].clone().sub(p[0]);
    v2=v3.clone().cross(v2).normalize();
    v1=v2.clone().cross(v3);
    return [v1,v2,v3];
  }
  return null;
}

// �����}�g���b�N�X�̕������C������
// d - �����]���}�g���b�N�X
// k - �����}�g���b�N�X
function toDir(d,k){
  var a=numeric.dot(d,k);
  for(var i=0;i<k.length;i++){
    var ki=k[i],ai=a[i];
    for(var j=0;j<ki.length;j++){
      ki[j]=numeric.dotVV(ai,d[j]);
    }
  }
}

// �����}�g���b�N�X�̕������C������
// d - �����]���}�g���b�N�X
// k - �����}�g���b�N�X
function toDir3(d,k){
  var i1,j1,a=[[0,0,0],[0,0,0],[0,0,0]],ai;
  for(var i=0;i<k.length;i+=3){
    for(var j=0;j<k[i].length;j+=3){
      for(i1=0;i1<3;i1++){
      	ai=a[i1];
      	var di=d[i1];
      	for(j1=0;j1<3;j1++){
      	  var s=0;
      	  for(var ii=0;ii<3;ii++){
      	    s+=di[ii]*k[i+ii][j+j1];
      	  }
      	  ai[j1]=s;
      	}
      }
      for(i1=0;i1<3;i1++){
      	ai=a[i1];
      	var ki=k[i+i1];
      	for(j1=0;j1<3;j1++){
      	  ki[j+j1]=numeric.dotVV(ai,d[j1]);
      	}
      }
    }
  }
}

inherits(FElement,Nodes);

// �l�ʑ�2���v�f�̐ߓ_�̃�,��,�č��W
var TETRA2_NODE=[[0,0,0],[1,0,0],[0,1,0],[0,0,1],[0.5,0,0],[0.5,0.5,0],
      	      	 [0,0.5,0],[0,0,0.5],[0.5,0,0.5],[0,0.5,0.5]];
// �l�ʑ�2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
var TETRA2_INT=[[GTETRA2[0],GTETRA2[0],GTETRA2[0],C1_24],
      	      	[GTETRA2[1],GTETRA2[0],GTETRA2[0],C1_24],
      	      	[GTETRA2[0],GTETRA2[1],GTETRA2[0],C1_24],
      	      	[GTETRA2[0],GTETRA2[0],GTETRA2[1],C1_24]];
// ���`1���v�f�̐ߓ_�̃�,��,�č��W
var WEDGE1_NODE=[[0,0,-1],[1,0,-1],[0,1,-1],[0,0,1],[1,0,1],[0,1,1]];
// ���`1���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
var WEDGE1_INT=[[C1_3,C1_3,GX2[0],0.5],[C1_3,C1_3,GX2[1],0.5]];
// ���`2���v�f�̐ߓ_�̃�,��,�č��W
var WEDGE2_NODE=[[0,0,-1],[1,0,-1],[0,1,-1],[0,0,1],[1,0,1],[0,1,1],
      	      	 [0.5,0,-1],[0.5,0.5,-1],[0,0.5,-1],
      	      	 [0.5,0,1],[0.5,0.5,1],[0,0.5,1],
      	      	 [0,0,0],[1,0,0],[0,1,0]];
// ���`2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
var WEDGE2_INT=[[GTRI2[0],GTRI2[0],GX3[0],C1_6*GW3[0]],
      	      	[GTRI2[1],GTRI2[0],GX3[0],C1_6*GW3[0]],
      	      	[GTRI2[0],GTRI2[1],GX3[0],C1_6*GW3[0]],
      	      	[GTRI2[0],GTRI2[0],GX3[1],C1_6*GW3[1]],
      	      	[GTRI2[1],GTRI2[0],GX3[1],C1_6*GW3[1]],
      	      	[GTRI2[0],GTRI2[1],GX3[1],C1_6*GW3[1]],
      	      	[GTRI2[0],GTRI2[0],GX3[2],C1_6*GW3[2]],
      	      	[GTRI2[1],GTRI2[0],GX3[2],C1_6*GW3[2]],
      	      	[GTRI2[0],GTRI2[1],GX3[2],C1_6*GW3[2]]];
// �Z�ʑ�1���v�f�̐ߓ_�̃�,��,�č��W
var HEXA1_NODE=[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],
      	      	[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]];
// �Z�ʑ�1���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
var HEXA1_INT=[[GX2[0],GX2[0],GX2[0],1],[GX2[1],GX2[0],GX2[0],1],
      	       [GX2[0],GX2[1],GX2[0],1],[GX2[1],GX2[1],GX2[0],1],
      	       [GX2[0],GX2[0],GX2[1],1],[GX2[1],GX2[0],GX2[1],1],
      	       [GX2[0],GX2[1],GX2[1],1],[GX2[1],GX2[1],GX2[1],1]];
// �Z�ʑ�2���v�f�̐ߓ_�̃�,��,�č��W
var HEXA2_NODE=[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],
      	      	[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1],
      	      	[0,-1,-1],[1,0,-1],[0,1,-1],[-1,0,-1],
      	      	[0,-1,1],[1,0,1],[0,1,1],[-1,0,1],
      	      	[-1,-1,0],[1,-1,0],[1,1,0],[-1,1,0]];
// �Z�ʑ�2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
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
// �Z�ʑ�1���v�f�̎��ʃ}�g���b�N�X�W��
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
// �\���b�h�v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// nodeP - �ߓ_�̃�,��,�č��W
// intP - �ϕ��_�̃�,��,�č��W,�d�݌W��
var SolidElement=function(label,material,nodes,nodeP,intP){
  FElement.call(this,label,material,nodes);
  this.nodeP=nodeP;
  this.intP=intP;
};

// ���R�r�s���Ԃ�
// p - �v�f�ߓ_
// sf - �`��֐��s��
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

// �`��֐��̌��z [ dNi/dx dNi/dy dNi/dz ] ��Ԃ�
// p - �v�f�ߓ_
// ja - ���R�r�s��
// sf - �`��֐��s��
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

// �c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// grad - �`��֐��̌��z
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

// �ϕ��_�̌`��֐��}�g���b�N�X [ NiNj ] ��Ԃ�
// p - �v�f�ߓ_
// x - ��,��,�č��W
// w - �d�݌W��
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

// �ϕ��_�̊g�U�}�g���b�N�X [ ��Ni�E��Nj ] ��Ԃ�
// p - �v�f�ߓ_
// x - ��,��,�č��W
// w - �d�݌W��
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

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
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

// �����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// d1 - ���� - �c�}�g���b�N�X
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

// �`��֐��}�g���b�N�X [ ��NiNjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
SolidElement.prototype.shapeFunctionMatrix=function(p,coef){
  var count=this.nodeCount(),s=numeric.rep([count,count],0);
  for(var i=0;i<this.intP.length;i++){
    addMatrix(s,this.shapePart(p,this.intP[i],coef*this.intP[i][3]));
  }
  return s;
};

// �g�U�}�g���b�N�X [ ���Ni�E��NjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
SolidElement.prototype.gradMatrix=function(p,coef){
  var count=this.nodeCount(),g=numeric.rep([count,count],0);
  for(var i=0;i<this.intP.length;i++){
    addMatrix(g,this.gradPart(p,this.intP[i],coef*this.intP[i][3]));
  }
  return g;
};

// �􉽍����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
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

// �ߓ_�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
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

// �v�f���̘c�x�N�g����Ԃ�
// p - �v�f�ߓ_
// v - �ߓ_�ψʃx�N�g��
// x - ��,��,�č��W
SolidElement.prototype.strainPart=function(p,v,x){
  var sf=this.shapeFunction(x[0],x[1],x[2]);
  var ja=this.jacobianMatrix(p,sf);
  var sm=this.strainMatrix(this.grad(p,ja,sf));
  return numeric.dotVM(v,sm);
};

// �v�f�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
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

// �v�f��\���������Ԃ�
// materials - �ޗ�
// p - �ߓ_
SolidElement.prototype.toString=function(materials,p){
  var s=this.getName()+'\t'+this.label.toString(10)+'\t'+
      	materials[this.material].label.toString(10);
  for(var i=0;i<this.nodes.length;i++){
    s+='\t'+p[this.nodes[i]].label.toString(10);
  }
  return s;
};

//--------------------------------------------------------------------//
// �l�ʑ�1���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var TetraElement1=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,null,null);
};

// �v�f���̂�Ԃ�
TetraElement1.prototype.getName=function(){
  return 'TetraElement1';
};

// �ߓ_����Ԃ�
TetraElement1.prototype.nodeCount=function(){
  return 4;
};

// �v�f���E����Ԃ�
TetraElement1.prototype.borderCount=function(){
  return 4;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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

// �v�f���������]����
TetraElement1.prototype.mirror=function(){
  swap(this.nodes,1,2);
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
TetraElement1.prototype.angle=function(p){
  var th=[];
  for(var i=0;i<4;i++){
    th[i]=solidAngle(p[i],p[(i+1)%4],p[(i+2)%4],p[(i+3)%4]);
  }
  return th;
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
TetraElement1.prototype.shapeFunction=function(xsi,eta,zeta){
  return [[1-xsi-eta-zeta,-1,-1,-1],[xsi,1,0,0],[eta,0,1,0],
      	  [zeta,0,0,1]];
};

// ���R�r�A����Ԃ�
// p - �v�f�ߓ_
TetraElement1.prototype.jacobian=function(p){
  var p0x=p[0].x,p0y=p[0].y,p0z=p[0].z;
  var j11=(p[2].y-p0y)*(p[3].z-p0z)-(p[3].y-p0y)*(p[2].z-p0z);
  var j21=(p[3].y-p0y)*(p[1].z-p0z)-(p[1].y-p0y)*(p[3].z-p0z);
  var j31=(p[1].y-p0y)*(p[2].z-p0z)-(p[2].y-p0y)*(p[1].z-p0z);
  return (p[1].x-p0x)*j11+(p[2].x-p0x)*j21+(p[3].x-p0x)*j31;
};

// �`��֐��̌��z [ dNi/dx dNi/dy dNi/dz ] ��Ԃ�
// p - �v�f�ߓ_
// ja - ���R�r�A��
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

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
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

// �����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// d1 - ���� - �c�}�g���b�N�X
TetraElement1.prototype.stiffnessMatrix=function(p,d1){
  var ja=this.jacobian(p);
  return this.stiffPart(d1,this.strainMatrix(this.grad(p,ja)),
      	      	      	C1_6*Math.abs(ja));
};

// �`��֐��}�g���b�N�X [ ��NiNjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
TetraElement1.prototype.shapeFunctionMatrix=function(p,coef){
  var value=0.1*C1_6*coef*Math.abs(this.jacobian(p)),vh=0.5*value;
  return [[value,vh,vh,vh],[vh,value,vh,vh],[vh,vh,value,vh],
      	  [vh,vh,vh,value]];
};

// �g�U�}�g���b�N�X [ ���Ni�E��NjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
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

// �􉽍����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
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

// �ߓ_�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
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

// �v�f�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
TetraElement1.prototype.elementStrainStress=function(p,u,d1){
  var sm=this.strainMatrix(this.grad(p,this.jacobian(p)));
  var eps=numeric.dotVM(this.toArray(u,3),sm);
  var str=numeric.dotMV(d1,eps);
  var energy=0.5*numeric.dotVV(eps,str);
  return [new Strain(eps),new Stress(str),energy];
};

//--------------------------------------------------------------------//
// �l�ʑ�2���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var TetraElement2=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,TETRA2_NODE,TETRA2_INT);
};

// �v�f���̂�Ԃ�
TetraElement2.prototype.getName=function(){
  return 'TetraElement2';
};

// �ߓ_����Ԃ�
TetraElement2.prototype.nodeCount=function(){
  return 10;
};

// �v�f���E����Ԃ�
TetraElement2.prototype.borderCount=function(){
  return 4;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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

// �v�f���������]����
TetraElement2.prototype.mirror=function(){
  swap(this.nodes,1,2);
  swap(this.nodes,4,6);
  swap(this.nodes,8,9);
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
TetraElement2.prototype.angle=function(p){
  return [solidAngle(p[0],p[4],p[6],p[7]),
      	  solidAngle(p[1],p[5],p[4],p[8]),
      	  solidAngle(p[2],p[6],p[5],p[9]),
      	  solidAngle(p[3],p[7],p[8],p[9]),
      	  planeAngle(p[4],p[2],p[3]),planeAngle(p[5],p[0],p[3]),
      	  planeAngle(p[6],p[1],p[3]),planeAngle(p[7],p[1],p[2]),
      	  planeAngle(p[8],p[2],p[0]),planeAngle(p[9],p[0],p[1])];
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
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
// ���`1���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var WedgeElement1=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,WEDGE1_NODE,WEDGE1_INT);
};

// �v�f���̂�Ԃ�
WedgeElement1.prototype.getName=function(){
  return 'WedgeElement1';
};

// �ߓ_����Ԃ�
WedgeElement1.prototype.nodeCount=function(){
  return 6;
};

// �v�f���E����Ԃ�
WedgeElement1.prototype.borderCount=function(){
  return 5;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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

// �v�f���������]����
WedgeElement1.prototype.mirror=function(){
  swap(this.nodes,1,2);
  swap(this.nodes,4,5);
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
WedgeElement1.prototype.angle=function(p){
  var th=[];
  for(var i=0;i<3;i++){
    th[i]=solidAngle(p[i],p[(i+1)%3],p[(i+2)%3],p[i+3]);
    th[i+3]=solidAngle(p[i+3],p[(i+1)%3+3],p[(i+2)%3+3],p[i]);
  }
  return th;
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
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

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
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
// ���`2���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var WedgeElement2=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,WEDGE2_NODE,WEDGE2_INT);
};

// �v�f���̂�Ԃ�
WedgeElement2.prototype.getName=function(){
  return 'WedgeElement2';
};

// �ߓ_����Ԃ�
WedgeElement2.prototype.nodeCount=function(){
  return 15;
};

// �v�f���E����Ԃ�
WedgeElement2.prototype.borderCount=function(){
  return 5;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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

// �v�f���������]����
WedgeElement2.prototype.mirror=function(){
  swap(this.nodes,1,2);
  swap(this.nodes,4,5);
  swap(this.nodes,6,8);
  swap(this.nodes,9,11);
  swap(this.nodes,13,14);
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
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

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
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
// �Z�ʑ�1���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var HexaElement1=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,HEXA1_NODE,HEXA1_INT);
};

// �v�f���̂�Ԃ�
HexaElement1.prototype.getName=function(){
  return 'HexaElement1';
};

// �ߓ_����Ԃ�
HexaElement1.prototype.nodeCount=function(){
  return 8;
};

// �v�f���E����Ԃ�
HexaElement1.prototype.borderCount=function(){
  return 6;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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

// �v�f���������]����
HexaElement1.prototype.mirror=function(){
  swap(this.nodes,1,3);
  swap(this.nodes,5,7);
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
HexaElement1.prototype.angle=function(p){
  var th=[];
  for(var i=0;i<4;i++){
    th[i]=solidAngle(p[i],p[(i+1)%4],p[(i+3)%4],p[i+4]);
    th[i+4]=solidAngle(p[i+4],p[(i+1)%4+4],p[(i+3)%4+4],p[i]);
  }
  return th;
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
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

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
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
// �Z�ʑ̃Z�����f�B�s�e�B��2���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var HexaElement2=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,HEXA2_NODE,HEXA2_INT);
};

// �v�f���̂�Ԃ�
HexaElement2.prototype.getName=function(){
  return 'HexaElement2';
};

// �ߓ_����Ԃ�
HexaElement2.prototype.nodeCount=function(){
  return 20;
};

// �v�f���E����Ԃ�
HexaElement2.prototype.borderCount=function(){
  return 6;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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

// �v�f���������]����
HexaElement2.prototype.mirror=function(){
  swap(this.nodes,1,3);
  swap(this.nodes,5,7);
  swap(this.nodes,8,11);
  swap(this.nodes,9,10);
  swap(this.nodes,12,15);
  swap(this.nodes,13,14);
  swap(this.nodes,17,19);
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
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

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
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

// �O�p�`1���v�f�̐ߓ_�̃�,�ō��W
var TRI1_NODE=[[0,0],[1,0],[0,1]];
// �O�p�`1���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
var TRI1_INT=[[C1_3,C1_3,0.5]];
// �O�p�`2���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
var TRI2_INT=[[GTRI2[0],GTRI2[0],C1_6],[GTRI2[1],GTRI2[0],C1_6],
      	      [GTRI2[0],GTRI2[1],C1_6]];
// �l�p�`1���v�f�̐ߓ_�̃�,�ō��W
var QUAD1_NODE=[[-1,-1],[1,-1],[1,1],[-1,1]];
// �l�p�`1���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
var QUAD1_INT=[[GX2[0],GX2[0],1],[GX2[1],GX2[0],1],[GX2[0],GX2[1],1],
      	       [GX2[1],GX2[1],1]];
// �O�p�`1���v�f�̎��ʃ}�g���b�N�X�W��
var TRI1_MASS1=[[1,0.5,0.5],[0.5,1,0.5],[0.5,0.5,1]];

//--------------------------------------------------------------------//
// �V�F���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - �V�F���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// nodeP - �ߓ_�̃�,�ō��W
// intP - �ϕ��_�̃�,�ō��W,�d�݌W��
var ShellElement=function(label,material,param,nodes,nodeP,intP){
  FElement.call(this,label,material,nodes);
  this.param=param;
  this.isShell=true;
  this.nodeP=nodeP;
  this.intP=intP;
};

// �v�f���E����Ԃ�
ShellElement.prototype.borderCount=function(){
  return 2;
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
ShellElement.prototype.angle=function(p){
  var th=[],count=this.nodeCount();
  for(var i=0;i<count;i++){
    th[i]=planeAngle(p[i],p[(i+1)%count],p[(i+count-1)%count]);
  }
  return th;
};

// ���R�r�s���Ԃ�
// p - �v�f�ߓ_
// sf - �`��֐��s��
// n - �@���x�N�g��
// t - �v�f����
ShellElement.prototype.jacobianMatrix=function(p,sf,n,t){
  var count=this.nodeCount(),jac=[0,0,0,0,0,0,0,0,0];
  for(var i=0;i<count;i++){
    var sfi=sf[i],ppi=p[i],pix=ppi.x,piy=ppi.y,piz=ppi.z;
    for(var j=0;j<2;j++){
      var sfij=sfi[j+1];
      jac[j]+=sfij*pix;
      jac[j+3]+=sfij*piy;
      jac[j+6]+=sfij*piz;
    }
  }
  jac[2]=0.5*t*n.x;
  jac[5]=0.5*t*n.y;
  jac[8]=0.5*t*n.z;
  return new THREE.Matrix3().fromArray(jac);
};

// �t���R�r�s���Ԃ�
// ja - ���R�r�s��
// d - �����]���}�g���b�N�X
ShellElement.prototype.jacobInv=function(ja,d){
  var e1=ja.elements;
  var jd=new THREE.Matrix3().set
    (e1[0]*d[0][0]+e1[3]*d[1][0]+e1[6]*d[2][0],
     e1[0]*d[0][1]+e1[3]*d[1][1]+e1[6]*d[2][1],
     e1[0]*d[0][2]+e1[3]*d[1][2]+e1[6]*d[2][2],
     e1[1]*d[0][0]+e1[4]*d[1][0]+e1[7]*d[2][0],
     e1[1]*d[0][1]+e1[4]*d[1][1]+e1[7]*d[2][1],
     e1[1]*d[0][2]+e1[4]*d[1][2]+e1[7]*d[2][2],
     0,0,e1[2]*d[0][2]+e1[5]*d[1][2]+e1[8]*d[2][2]);
  return new THREE.Matrix3().getInverse(jd,true);
};

// �`��֐��̌��z [ dNi/dx dNi/dy ] ��Ԃ�
// p - �v�f�ߓ_
// ja - ���R�r�s��
// sf - �`��֐��s��
// d - �����]���}�g���b�N�X
// t - �v�f����
ShellElement.prototype.grad=function(p,ja,sf,d,t){
  var count=this.nodeCount(),gr=[];
  var ji=this.jacobInv(ja,d).elements;
  for(var i=0;i<count;i++){
    var sfi=sf[i],dndxsi=sfi[1],dndeta=sfi[2];
    gr[i]=[ji[0]*dndxsi+ji[3]*dndeta,ji[1]*dndxsi+ji[4]*dndeta,
      	   ji[2]*dndxsi+ji[5]*dndeta];
  }
  return gr;
};

// �c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// ja - ���R�r�s��
// sf - �`��֐��s��
// d - �����]���}�g���b�N�X
ShellElement.prototype.strainMatrix1=function(ja,sf,d){
  var count=this.nodeCount(),m=numeric.rep([count,4],0);
  var ji=this.jacobInv(ja,d).elements;
  for(var i=0;i<count;i++){
    var mi=m[i],sfi=sf[i];
    for(var j=0;j<3;j++){
      mi[j]=ji[j]*sfi[1]+ji[j+3]*sfi[2];
    }
    mi[3]=ji[8]*sfi[0];
  }
  return m;
};

// �c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// �������c�͗v�f�ʍ��W�A�ψʂ͑S�̍��W
// ja - ���R�r�s��
// sf - �`��֐��s��
// d - �����]���}�g���b�N�X
// zeta - �ߓ_�̃č��W
// t - �v�f����
ShellElement.prototype.strainMatrix=function(ja,sf,d,zeta,t){
  var b=this.strainMatrix1(ja,sf,d),z=0.5*t*zeta;
  var count=this.nodeCount(),m1=numeric.rep([5,6],0);
  var matrix=numeric.rep([6*count,5],0);
  for(var i=0;i<count;i++){
    var bi=b[i];
    m1[0][0]=bi[0];
    m1[0][4]=z*bi[0];
    m1[1][1]=bi[1];
    m1[1][3]=-z*bi[1];
    m1[2][0]=bi[1];
    m1[2][1]=bi[0];
    m1[2][3]=-z*bi[0];
    m1[2][4]=z*bi[1];
    m1[3][1]=bi[2];
    m1[3][2]=bi[1];
    m1[3][3]=-0.5*t*bi[3]-z*bi[2];
    m1[4][0]=bi[2];
    m1[4][2]=bi[0];
    m1[4][4]=0.5*t*bi[3]+z*bi[2];
    var ib=6*i;
    for(var i1=0;i1<5;i1++){
      var m1i=m1[i1];
      for(var j1=0;j1<3;j1++){
      	var dj=d[j1],s1=0,s2=0;
      	for(var k1=0;k1<3;k1++){
      	  s1+=m1i[k1]*dj[k1];
      	  s2+=m1i[k1+3]*dj[k1];
      	}
      	matrix[ib+j1][i1]+=s1;
      	matrix[ib+3+j1][i1]+=s2;
      }
    }
  }
  return matrix;
};

// �ϕ��_�̌`��֐��}�g���b�N�X [ NiNj ] ��Ԃ�
// p - �v�f�ߓ_
// x - ��,��,�č��W
// w - �d�݌W��
// t - �v�f����
ShellElement.prototype.shapePart=function(p,x,w,t){
  var sf=this.shapeFunction(x[0],x[1]);
  var ja=this.jacobianMatrix(p,sf,normalVector(p),t);
  var count=this.nodeCount(),matrix=[];
  var coef=2*w*Math.abs(ja.determinant());
  for(var i=0;i<count;i++){
    var matr=[],cf2=coef*sf[i][0];
    for(var j=0;j<count;j++){
      matr[j]=cf2*sf[j][0];
    }
    matrix[i]=matr;
  }
  return matrix;
};

// �ϕ��_�̊g�U�}�g���b�N�X [ ��Ni�E��Nj ] ��Ԃ�
// p - �v�f�ߓ_
// x - ��,��,�č��W
// w - �d�݌W��
// t - �v�f����
ShellElement.prototype.gradPart=function(p,x,w,t){
  var sf=this.shapeFunction(x[0],x[1]);
  var ja=this.jacobianMatrix(p,sf,normalVector(p),t);
  var gr=this.grad(p,ja,sf,dirMatrix(p),t);
  var count=this.nodeCount(),matrix=[];
  var coef=2*w*Math.abs(ja.determinant());
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

// �`��֐��}�g���b�N�X [ ��NiNjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
// sp - �V�F���p�����[�^
ShellElement.prototype.shapeFunctionMatrix=function(p,coef,sp){
  var count=this.nodeCount(),s=numeric.rep([count,count],0);
  var t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    addMatrix(s,this.shapePart(p,this.intP[i],coef*this.intP[i][2],t));
  }
  return s;
};

// �g�U�}�g���b�N�X [ ���Ni�E��NjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
// sp - �V�F���p�����[�^
ShellElement.prototype.gradMatrix=function(p,coef,sp){
  var count=this.nodeCount(),g=numeric.rep([count,count],0);
  var t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    addMatrix(g,this.gradPart(p,this.intP[i],coef*this.intP[i][2],t));
  }
  return g;
};

// �􉽍����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
// sp - �V�F���p�����[�^
ShellElement.prototype.geomStiffnessMatrix=function(p,u,d1,sp){
  var count=this.nodeCount(),kk=numeric.rep([6*count,6*count],0);
  var d=dirMatrix(p),n=normalVector(p);
  var v=this.toArray(u,6),t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    var ip=this.intP[i];
    var sf=this.shapeFunction(ip[0],ip[1]);
    var ja=this.jacobianMatrix(p,sf,n,t);
    var gr=this.grad(p,ja,sf,d,t);
    var sm=this.strainMatrix(ja,sf,d,0,t);
    var str=this.toStress(numeric.dotMV(d1,numeric.dotVM(v,sm)));
    var w=2*ip[2]*Math.abs(ja.determinant());
    for(var i1=0;i1<count;i1++){
      var i6=6*i1,gri=gr[i1];
      for(var j1=0;j1<count;j1++){
      	var j6=6*j1,grj=gr[j1];
      	var s=w*(gri[0]*(str.xx*grj[0]+str.xy*grj[1]+str.zx*grj[2])+
      	      	 gri[1]*(str.xy*grj[0]+str.yy*grj[1]+str.yz*grj[2])+
      	      	 gri[2]*(str.zx*grj[0]+str.yz*grj[1]+str.zz*grj[2]));
      	kk[i6][j6]+=s;
      	kk[i6+1][j6+1]+=s;
      	kk[i6+2][j6+2]+=s;
      }
    }
  }
  toDir3(d,kk);
  return kk;
};

// �ߓ_�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
// sp - �V�F���p�����[�^
ShellElement.prototype.strainStress=function(p,u,d1,sp){
  var count=this.nodeCount(),d=dirMatrix(p),n=normalVector(p);
  var v=this.toArray(u,6),t=sp.thickness;
  var strain1=[],stress1=[],energy1=[],strain2=[],stress2=[],energy2=[];
  for(var i=0;i<count;i++){
    var np=this.nodeP[i];
    var eps1=this.strainPart(p,v,n,d,np[0],np[1],1,t);
    var eps2=this.strainPart(p,v,n,d,np[0],np[1],-1,t);
    strain1[i]=this.toStrain(eps1);
    stress1[i]=this.toStress(numeric.dotMV(d1,eps1));
    strain2[i]=this.toStrain(eps2);
    stress2[i]=this.toStress(numeric.dotMV(d1,eps2));
    strain1[i].rotate(d);
    stress1[i].rotate(d);
    strain2[i].rotate(d);
    stress2[i].rotate(d);
    energy1[i]=0.5*strain1[i].innerProduct(stress1[i]);
    energy2[i]=0.5*strain2[i].innerProduct(stress2[i]);
  }
  return [strain1,stress1,energy1,strain2,stress2,energy2];
};

// �v�f���̘c�x�N�g����Ԃ�
// p - �v�f�ߓ_
// v - �ߓ_�ψʃx�N�g��
// n - �@���x�N�g��
// d - �����]���}�g���b�N�X
// xsi,eta,zeta - ��,��,�č��W
// t - �v�f����
ShellElement.prototype.strainPart=function(p,v,n,d,xsi,eta,zeta,t){
  var sf=this.shapeFunction(xsi,eta);
  var ja=this.jacobianMatrix(p,sf,n,t);
  var sm=this.strainMatrix(ja,sf,d,zeta,t);
  return numeric.dotVM(v,sm);
};

// �v�f�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
// sp - �V�F���p�����[�^
ShellElement.prototype.elementStrainStress=function(p,u,d1,sp){
  var d=dirMatrix(p),n=normalVector(p),v=this.toArray(u,6);
  var t=sp.thickness,cf=1/this.intP.length;
  var strain1=[0,0,0,0,0,0],stress1=[0,0,0,0,0,0],energy1=0;
  var strain2=[0,0,0,0,0,0],stress2=[0,0,0,0,0,0],energy2=0;
  for(var i=0;i<this.intP.length;i++){
    var ip=this.intP[i];
    var eps1=this.strainPart(p,v,n,d,ip[0],ip[1],1,t);
    var eps2=this.strainPart(p,v,n,d,ip[0],ip[1],-1,t);
    strain1=numeric.add(strain1,eps1);
    strain2=numeric.add(strain2,eps2);
    var str1=numeric.dotMV(d1,eps1);
    var str2=numeric.dotMV(d1,eps2);
    stress1=numeric.add(stress1,str1);
    stress2=numeric.add(stress2,str2);
    energy1+=numeric.dotVV(eps1,str1);
    energy2+=numeric.dotVV(eps2,str2);
  }
  strain1=numeric.mul(strain1,cf);
  stress1=numeric.mul(stress1,cf);
  energy1*=0.5*cf;
  strain2=numeric.mul(strain1,cf);
  stress2=numeric.mul(stress1,cf);
  energy2*=0.5*cf;
  return [this.toStrain(strain1),this.toStress(stress1),energy1,
      	  this.toStrain(strain2),this.toStress(stress2),energy2];
};

// �x�N�g����c�ɕϊ�����
// s - �c�x�N�g��
ShellElement.prototype.toStrain=function(s){
  return new Strain([s[0],s[1],0,s[2],s[3],s[4]]);
};

// �x�N�g����c�ɕϊ�����
// s - �c�x�N�g��
ShellElement.prototype.toStress=function(s){
  return new Stress([s[0],s[1],0,s[2],s[3],s[4]]);
};

// �v�f��\���������Ԃ�
// materials - �ޗ�
// params - �V�F���p�����[�^
// p - �ߓ_
ShellElement.prototype.toString=function(materials,params,p){
  var s=this.getName()+'\t'+this.label.toString(10)+'\t'+
      	materials[this.material].label.toString(10)+'\t'+
      	params[this.param].label.toString(10);
  for(var i=0;i<this.nodes.length;i++){
    s+='\t'+p[this.nodes[i]].label.toString(10);
  }
  return s;
};

//--------------------------------------------------------------------//
// �O�p�`1���v�f (�����V�F��)
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - �V�F���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var TriElement1=function(label,material,param,nodes){
  ShellElement.call(this,label,material,param,nodes,TRI1_NODE,TRI1_INT);
};

// �v�f���̂�Ԃ�
TriElement1.prototype.getName=function(){
  return 'TriElement1';
};

// �ߓ_����Ԃ�
TriElement1.prototype.nodeCount=function(){
  return 3;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
TriElement1.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new TriangleBorder1(element,[p[0],p[1],p[2]]);
    case 1:
      return new TriangleBorder1(element,[p[0],p[2],p[1]]);
  }
};

// �v�f���E�ӂ�Ԃ�
// element - �v�f���x��
// index - �v�f���E�ӂ̃C���f�b�N�X
TriElement1.prototype.borderEdge=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new EdgeBorder1(element,[p[0],p[1]]);
    case 1:
      return new EdgeBorder1(element,[p[1],p[2]]);
    case 2:
      return new EdgeBorder1(element,[p[2],p[0]]);
  }
};

// �v�f���������]����
TriElement1.prototype.mirror=function(){
  swap(this.nodes,1,2);
};

// �P���̌`��֐��s�� [ Ni dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta - �v�f������,�ō��W
TriElement1.prototype.shapeFunction=function(xsi,eta){
  return [[1-xsi-eta,-1,-1],[xsi,1,0],[eta,0,1]];
};

// �Q���̌`��֐��s�� [ Ni dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta - �v�f������,�ō��W
TriElement1.prototype.shapeFunction2=function(xsi,eta){
  var xe=1-xsi-eta;
  return [[xe*(2*xe-1),1-4*xe,1-4*xe],[xsi*(2*xsi-1),4*xsi-1,0],
      	  [eta*(2*eta-1),0,4*eta-1],[4*xe*xsi,4*(xe-xsi),-4*xsi],
      	  [4*xsi*eta,4*eta,4*xsi],[4*xe*eta,-4*eta,4*(xe-eta)]];
};

// �p�x�̌`��֐��s�� [ Hxi Hyi dHxi/d�� dHyi/d�� dHxi/d�� dHyi/d�� ] ��Ԃ�
// p - �v�f�ߓ_
// d - �����]���}�g���b�N�X
// xsi,eta - �v�f������,�ō��W
TriElement1.prototype.shapeFunction3=function(p,d,xsi,eta){
  var count=this.nodeCount(),m=numeric.rep([3*count,6],0);
  var sf2=this.shapeFunction2(xsi,eta);
  var d12=p[1].clone().sub(p[0]);
  var d23=p[2].clone().sub(p[1]);
  var d31=p[0].clone().sub(p[2]);
  var l=[1/d12.lengthSq(),1/d23.lengthSq(),1/d31.lengthSq()];
  var x=[d[0][0]*d12.x+d[1][0]*d12.y+d[2][0]*d12.z,
      	 d[0][0]*d23.x+d[1][0]*d23.y+d[2][0]*d23.z,
      	 d[0][0]*d31.x+d[1][0]*d31.y+d[2][0]*d31.z];
  var y=[d[0][1]*d12.x+d[1][1]*d12.y+d[2][1]*d12.z,
      	 d[0][1]*d23.x+d[1][1]*d23.y+d[2][1]*d23.z,
      	 d[0][1]*d31.x+d[1][1]*d31.y+d[2][1]*d31.z];
  var a=[1.5*l[0]*y[0],1.5*l[1]*y[1],1.5*l[2]*y[2]];
  var b=[-1.5*l[0]*x[0],-1.5*l[1]*x[1],-1.5*l[2]*x[2]];
  var c=[0.75*l[0]*y[0]*y[0]-0.5,0.75*l[1]*y[1]*y[1]-0.5,
      	 0.75*l[2]*y[2]*y[2]-0.5];
  var d1=[0.75*l[0]*x[0]*y[0],0.75*l[1]*x[1]*y[1],0.75*l[2]*x[2]*y[2]];
  var e=[0.25-0.75*l[0]*y[0]*y[0],0.25-0.75*l[1]*y[1]*y[1],
      	 0.25-0.75*l[2]*y[2]*y[2]];
  for(var i=0;i<3;i++){
    var i1=(i+2)%3;
    var i3=3*i;
    for(var j=0;j<3;j++){
      var j2=2*j;
      m[i3][j2]=a[i1]*sf2[3+i1][j]-a[i]*sf2[3+i][j];
      m[i3][j2+1]=b[i1]*sf2[3+i1][j]-b[i]*sf2[3+i][j];
      m[i3+1][j2]=sf2[i][j]-c[i1]*sf2[3+i1][j]-c[i]*sf2[3+i][j];
      var dn=d1[i1]*sf2[3+i1][j]+d1[i]*sf2[3+i][j];
      m[i3+1][j2+1]=dn;
      m[i3+2][j2]=dn;
      m[i3+2][j2+1]=sf2[i][j]-e[i1]*sf2[3+i1][j]-e[i]*sf2[3+i][j];
    }
  }
  return m;
};

// ���R�r�A����Ԃ�
// p - �v�f�ߓ_
TriElement1.prototype.jacobian=function(p){
  var p0x=p[0].x,p0y=p[0].y,p0z=p[0].z;
  var j1=(p[1].y-p0y)*(p[2].z-p0z)-(p[1].z-p0z)*(p[2].y-p0y);
  var j2=(p[1].z-p0z)*(p[2].x-p0x)-(p[1].x-p0x)*(p[2].z-p0z);
  var j3=(p[1].x-p0x)*(p[2].y-p0y)-(p[1].y-p0y)*(p[2].x-p0x);
  return Math.sqrt(j1*j1+j2*j2+j3*j3);
};

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
// t - �v�f����
TriElement1.prototype.massMatrix=function(p,dens,t){
  var count=this.nodeCount(),m=numeric.rep([6*count,6*count],0);
  var mb=numeric.rep([3*count,3*count],0),d=dirMatrix(p);
  var djt=dens*t*this.jacobian(p),tt=C1_12*t*t,dm=C1_12*djt,i,j;

  for(var k=0;k<3;k++){
    var ipi=TRI2_INT[k],sf3=this.shapeFunction3(p,d,ipi[0],ipi[1]);
    var sf=this.shapeFunction(ipi[0],ipi[1]);
    var hz=[sf[0][0],0,0,sf[1][0],0,0,sf[2][0],0,0],cfm=djt*ipi[2];
    for(i=0;i<3*count;i++){
      for(j=0;j<3*count;j++){
      	var hxhy=sf3[i][0]*sf3[j][0]+sf3[i][1]*sf3[j][1];
      	mb[i][j]+=cfm*(tt*hxhy+hz[i]*hz[j]);
      }
    }
  }

  for(i=0;i<count;i++){
    var i3=3*i,i6=6*i;
    for(j=0;j<count;j++){
      var j3=3*j,j6=6*j,cf1=TRI1_MASS1[i][j];
      var dme=cf1*dm;
      m[i6][j6]=dme;
      m[i6+1][j6+1]=dme;
      for(var i1=0;i1<3;i1++){
      	for(var j1=0;j1<3;j1++){
      	  m[i6+2+i1][j6+2+j1]=mb[i3+i1][j3+j1];
      	}
      }
      m[i6+5][j6+5]=1e-5*dm*tt;
    }
  }
  toDir3(d,m);
  return m;
};

// �����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// d1 - ���� - �c�}�g���b�N�X
// sp - �V�F���p�����[�^
TriElement1.prototype.stiffnessMatrix=function(p,d1,sp){
  var d=dirMatrix(p),n=normalVector(p),t=sp.thickness,i,j,ii,jj;

  var sf1=this.shapeFunction(C1_3,C1_3);
  var ja1=this.jacobianMatrix(p,sf1,n,t);
  var jac1=ja1.determinant();
  var jinv=this.jacobInv(ja1,d);
  var b1=this.strainMatrix1(sf1,jinv);
  var k1=this.stiffPart(d1,b1,Math.abs(jac1));

  var count=this.nodeCount(),k2=numeric.rep([3*count,3*count],0);
  var coef=t*t*Math.abs(jac1)/36;
  for(i=0;i<3;i++){
    var ipi=TRI2_INT[i],sf3=this.shapeFunction3(p,d,ipi[0],ipi[1]);
    var b2=this.strainMatrix2(sf3,jinv);
    addMatrix(k2,this.stiffPart(d1,b2,coef));
  }

  var ce1=1e-3*t*t*Math.abs(jac1)*d1[2][2],ce2=-ce1/2;
  var kk=numeric.rep([6*count,6*count],0),ks=numeric.rep([6,6],0);
  var dir=numeric.rep([6,6],0);
  for(i=0;i<3;i++){
    for(j=0;j<3;j++){
      dir[i][j]=d[i][j];
      dir[i+3][j+3]=d[i][j];
    }
  }
  for(i=0;i<3;i++){
    var i2=2*i;
    var i3=3*i;
    var i6=6*i;
    for(j=0;j<count;j++){
      var j2=2*j;
      var j3=3*j;
      var j6=6*j;
      for(ii=0;ii<6;ii++){
      	for(jj=0;jj<6;jj++){
      	  ks[ii][jj]=0;
      	}
      }
      ks[0][0]=k1[i2][j2];
      ks[0][1]=k1[i2][j2+1];
      ks[1][0]=k1[i2+1][j2];
      ks[1][1]=k1[i2+1][j2+1];
      for(ii=0;ii<3;ii++){
      	for(jj=0;jj<3;jj++){
      	  ks[2+ii][2+jj]=k2[i3+ii][j3+jj];
      	}
      }
      if(i==j) ks[5][5]=ce1;
      else     ks[5][5]=ce2;
      toDir(dir,ks);
      for(ii=0;ii<6;ii++){
      	for(jj=0;jj<6;jj++){
      	  kk[i6+ii][j6+jj]=ks[ii][jj];
      	}
      }
    }
  }
  return kk;
};

// �c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// sf - �`��֐��s��
// jinv - �t���R�r�s��
TriElement1.prototype.strainMatrix1=function(sf,jinv){
  var count=this.nodeCount(),b=numeric.rep([2*count,3],0);
  var ji=jinv.elements;
  for(var i=0;i<count;i++){
    var sfi=sf[i];
    var dndx=ji[0]*sfi[1]+ji[3]*sfi[2];
    var dndy=ji[1]*sfi[1]+ji[4]*sfi[2];
    var i2=2*i;
    b[i2][0]=dndx;
    b[i2+1][1]=dndy;
    b[i2][2]=dndy;
    b[i2+1][2]=dndx;
  }
  return b;
};

// �ʊO�c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// sf - �`��֐��s��
// jinv - �t���R�r�s��
TriElement1.prototype.strainMatrix2=function(sf,jinv){
  var count=3*this.nodeCount(),b=[];
  var ji=jinv.elements;
  for(var i=0;i<count;i++){
    var sfi=sf[i];
    var hxx=ji[0]*sfi[2]+ji[3]*sfi[4];
    var hxy=ji[1]*sfi[2]+ji[4]*sfi[4];
    var hyx=ji[0]*sfi[3]+ji[3]*sfi[5];
    var hyy=ji[1]*sfi[3]+ji[4]*sfi[5];
    b[i]=[hyx,-hxy,hyy-hxx];
  }
  return b;
};

// �`��֐��}�g���b�N�X [ ��NiNjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
// t - �v�f����
TriElement1.prototype.shapeFunctionMatrix=function(p,coef,t){
  var ds=coef*this.jacobian(p)/12;
  var count=3*this.nodeCount(),s=numeric.rep([count],0.5*ds);
  for(var i=0;i<count;i++) s[i][i]=ds;
  return s;
};

// �􉽍����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
// sp - �V�F���p�����[�^
TriElement1.prototype.geomStiffnessMatrix=function(p,u,d1,sp){
  var count=this.nodeCount(),kk=numeric.rep([6*count,6*count],0);
  var d=dirMatrix(p),n=normalVector(p);
  var v=this.toArray(u,6),t=sp.thickness;
  var ip=this.intP[0];
  var sf1=this.shapeFunction(ip[0],ip[1]);
  var ja=this.jacobianMatrix(p,sf1,n,t);
  var gr=this.grad(p,ja,sf1,d,t);
  var jinv=this.jacobInv(ja,d);
  var sf3=this.shapeFunction3(p,d,ip[0],ip[1]);
  var sm=this.strainMatrix(sf1,sf3,jinv,d,0,t);
  var str=this.toStress(numeric.dotMV(d1,numeric.dotVM(v,sm)));
  var w=Math.abs(ja.determinant());
  for(var i1=0;i1<count;i1++){
    var i6=6*i1,gri=gr[i1];
    for(var j1=0;j1<count;j1++){
      var j6=6*j1,grj=gr[j1];
      var s=w*(gri[0]*(str.xx*grj[0]+str.xy*grj[1]+str.zx*grj[2])+
      	       gri[1]*(str.xy*grj[0]+str.yy*grj[1]+str.yz*grj[2])+
      	       gri[2]*(str.zx*grj[0]+str.yz*grj[1]+str.zz*grj[2]));
      kk[i6][j6]=s;
      kk[i6+1][j6+1]=s;
      kk[i6+2][j6+2]=s;
    }
  }
  toDir3(d,kk);
  return kk;
};

// �v�f���̘c�x�N�g����Ԃ�
// p - �v�f�ߓ_
// v - �ߓ_�ψʃx�N�g��
// n - �@���x�N�g��
// d - �����]���}�g���b�N�X
// xsi,eta,zeta - ��,��,�č��W
// t - �v�f����
TriElement1.prototype.strainPart=function(p,v,n,d,xsi,eta,zeta,t){
  var sf1=this.shapeFunction(xsi,eta);
  var ja=this.jacobianMatrix(p,sf1,n,t);
  var jinv=this.jacobInv(ja,d);
  var sf3=this.shapeFunction3(p,d,xsi,eta);
  var sm=this.strainMatrix(sf1,sf3,jinv,d,zeta,t);
  return numeric.dotVM(v,sm);
};

// �c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// �������c�͗v�f�ʍ��W�A�ψʂ͑S�̍��W
// sf1 - �ʓ��ό`�̌`��֐��s��
// sf3 - �ʊO�ό`�̌`��֐��s��
// jinv - �t���R�r�s��
// d - �����]���}�g���b�N�X
// zeta - �ߓ_�̃č��W
// t - �v�f����
TriElement1.prototype.strainMatrix=function(sf1,sf3,jinv,d,zeta,t){
  var b1=this.strainMatrix1(sf1,jinv);
  var b2=this.strainMatrix2(sf3,jinv);
  var count=this.nodeCount(),m1=numeric.rep([3,6],0);
  var matrix=numeric.rep([6*count,3],0),z=0.5*t*zeta,i1;
  for(var i=0;i<count;i++){
    var i2=2*i;
    var i3=3*i;
    var i6=6*i;
    for(i1=0;i1<3;i1++){
      m1[i1][0]=b1[i2][i1];
      m1[i1][1]=b1[i2+1][i1];
      m1[i1][2]=z*b2[i3][i1];
      m1[i1][3]=z*b2[i3+1][i1];
      m1[i1][4]=z*b2[i3+2][i1];
      m1[i1][5]=0;
    }
    for(i1=0;i1<3;i1++){
      var m1i=m1[i1];
      for(var j1=0;j1<3;j1++){
      	var dj=d[j1],s1=0,s2=0;
      	for(var k1=0;k1<3;k1++){
      	  s1+=m1i[k1]*dj[k1];
      	  s2+=m1i[k1+3]*dj[k1];
      	}
      	matrix[i6+j1][i1]+=s1;
      	matrix[i6+3+j1][i1]+=s2;
      }
    }
  }
  return matrix;
};

// �x�N�g����c�ɕϊ�����
// s - �c�x�N�g��
TriElement1.prototype.toStrain=function(s){
  return new Strain([s[0],s[1],0,s[2],0,0]);
};

// �x�N�g����c�ɕϊ�����
// s - �c�x�N�g��
TriElement1.prototype.toStress=function(s){
  return new Stress([s[0],s[1],0,s[2],0,0]);
};

//--------------------------------------------------------------------//
// �l�p�`1���v�f (MITC4)
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - �V�F���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var QuadElement1=function(label,material,param,nodes){
  ShellElement.call(this,label,material,param,nodes,
      	      	    QUAD1_NODE,QUAD1_INT);
};

// �v�f���̂�Ԃ�
QuadElement1.prototype.getName=function(){
  return 'QuadElement1';
};

// �ߓ_����Ԃ�
QuadElement1.prototype.nodeCount=function(){
  return 4;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
QuadElement1.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new QuadangleBorder1(element,[p[0],p[1],p[2],p[3]]);
    case 1:
      return new QuadangleBorder1(element,[p[0],p[3],p[2],p[1]]);
  }
};

// �v�f���E�ӂ�Ԃ�
// element - �v�f���x��
// index - �v�f���E�ӂ̃C���f�b�N�X
QuadElement1.prototype.borderEdge=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new EdgeBorder1(element,[p[0],p[1]]);
    case 1:
      return new EdgeBorder1(element,[p[1],p[2]]);
    case 2:
      return new EdgeBorder1(element,[p[2],p[3]]);
    case 3:
      return new EdgeBorder1(element,[p[3],p[0]]);
  }
};

// �v�f���������]����
QuadElement1.prototype.mirror=function(){
  swap(this.nodes,1,3);
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta - �v�f������,�ō��W
QuadElement1.prototype.shapeFunction=function(xsi,eta){
  return [[0.25*(1-xsi)*(1-eta),-0.25*(1-eta),-0.25*(1-xsi)],
      	  [0.25*(1+xsi)*(1-eta),0.25*(1-eta),-0.25*(1+xsi)],
      	  [0.25*(1+xsi)*(1+eta),0.25*(1+eta),0.25*(1+xsi)],
      	  [0.25*(1-xsi)*(1+eta),-0.25*(1+eta),0.25*(1-xsi)]];
};

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
// t - �v�f����
QuadElement1.prototype.massMatrix=function(p,dens,t){
  var count=this.nodeCount(),m=numeric.rep([6*count,6*count],0);
  var d=dirMatrix(p),n=normalVector(p),tt=C1_12*t*t;
  for(var i=0;i<this.intP.length;i++){
    var ipi=this.intP[i],sf=this.shapeFunction(ipi[0],ipi[1]);
    var nn=[sf[0][0],sf[1][0],sf[2][0],sf[3][0]];
    var jac=Math.abs(this.jacobianMatrix(p,sf,n,t).determinant());
    jac*=2*ipi[2];
    for(var i1=0;i1<count;i1++){
      var i6=6*i1,nja=nn[i1]*jac;
      for(var j1=0;j1<count;j1++){
      	var j6=6*j1,nnja=nja*nn[j1],dm=dens*nnja,dii=tt*dm;
      	m[i6][j6]+=dm;
      	m[i6+1][j6+1]+=dm;
      	m[i6+2][j6+2]+=dm;
      	m[i6+3][j6+3]+=dii;
      	m[i6+4][j6+4]+=dii;
      	m[i6+5][j6+5]+=1e-4*dii;
      }
    }
  }
  toDir3(d,m);
  return m;
};

// �����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// d1 - ���� - �c�}�g���b�N�X
// sp - �V�F���p�����[�^
QuadElement1.prototype.stiffnessMatrix=function(p,d1,sp){
  var size=6*this.nodeCount(),kk=numeric.rep([size,size],0);
  var n=normalVector(p),t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    var ks=this.stiffPart(p,d1,n,this.intP[i][0],this.intP[i][1],t);
    addMatrix(kk,ks);
  }
  return kk;
};

// �ϕ��_�̍����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// d1 - ���� - �c�}�g���b�N�X
// n - �@���x�N�g��
// xsi,eta - �v�f������,�ō��W
// t - �v�f����
QuadElement1.prototype.stiffPart=function(p,d1,n,xsi,eta,t){
  var d=dirMatrix(p);
  var sf=this.shapeFunction(xsi,eta);
  var ja=this.jacobianMatrix(p,sf,n,t);
  var bc0=this.strainMatrix1(ja,sf,d);
  var sf1=this.shapeFunction(xsi,0);
  var ja1=this.jacobianMatrix(p,sf1,n,t);
  var sf2=this.shapeFunction(0,eta);
  var ja2=this.jacobianMatrix(p,sf2,n,t);
  var bc=[this.strainMatrix1(ja1,sf1,d),this.strainMatrix1(ja2,sf2,d)];
  var count=this.nodeCount();
  var kk=numeric.rep([6*count,6*count],0);
  var jacob=Math.abs(ja.determinant());

  var tt6=t*t/6.0,ce1=1e-3*t*t*d1[3][3],ce2=-ce1/(count-1),j1,j2;
  var k1=[[0,0,0],[0,0,0],[0,0,0]];
  var k2=[[0,0,0],[0,0,0],[0,0,0]];
  var k3=[[0,0,0],[0,0,0],[0,0,0]];
  var k4=[[0,0,0],[0,0,0],[0,0,0]];
  for(var i=0;i<count;i++){
    for(var j=0;j<count;j++){
      for(j1=0;j1<3;j1++){
      	for(j2=0;j2<3;j2++){
      	  k1[j1][j2]=0;
      	  k2[j1][j2]=0;
      	  k3[j1][j2]=0;
      	  k4[j1][j2]=0;
      	}
      }
      for(j1=0;j1<2;j1++){
      	for(j2=0;j2<2;j2++){
      	  k1[j1][j2]=bc0[i][j1]*d1[j1][j2]*bc0[j][j2]+
      	      	     bc0[i][1-j1]*d1[2][2]*bc0[j][1-j2];
      	}
      	var dd=d1[4-j1][4-j1];
      	k1[j1][j1]+=bc[1-j1][i][2]*dd*bc[1-j1][j][2];
      	k1[j1][2]=bc[1-j1][i][2]*dd*bc[j1][j][j1];
      	k1[2][j1]=bc[j1][i][j1]*dd*bc[1-j1][j][2];
      	k2[j1][j1]=bc[1-j1][i][2]*dd*bc[1-j1][j][3];
      	k2[2][j1]=bc[1-j1][i][j1]*dd*bc[1-j1][j][3];
      	k3[j1][j1]=bc[1-j1][i][3]*dd*bc[1-j1][j][2];
      	k3[j1][2]=bc[1-j1][i][3]*dd*bc[1-j1][j][j1];
      }
      k1[2][2]=bc[0][i][1]*d1[3][3]*bc[0][j][1]+
      	       bc[1][i][0]*d1[4][4]*bc[1][j][0];
      k4[0][0]=k1[1][1]+3*bc[0][i][3]*d1[3][3]*bc[0][j][3];
      k4[0][1]=-k1[1][0];
      k4[1][0]=-k1[0][1];
      k4[1][1]=k1[0][0]+3*bc[1][i][3]*d1[4][4]*bc[1][j][3];
      for(j1=0;j1<3;j1++){
      	var kt=k2[j1][0];
      	k2[j1][0]=-k2[j1][1];
      	k2[j1][1]=kt;
      	kt=k3[0][j1];
      	k3[0][j1]=-k3[1][j1];
      	k3[1][j1]=kt;
      }

      if(i==j) k4[2][2]=ce1;
      else     k4[2][2]=ce2;
      toDir(d,k1);
      toDir(d,k2);
      toDir(d,k3);
      toDir(d,k4);
      var i0=6*i;
      var j0=6*j;
      for(j1=0;j1<3;j1++){
      	for(j2=0;j2<3;j2++){
      	  kk[i0+j1][j0+j2]=2*jacob*k1[j1][j2];
      	  kk[i0+j1][j0+3+j2]=t*jacob*k2[j1][j2];
      	  kk[i0+3+j1][j0+j2]=t*jacob*k3[j1][j2];
      	  kk[i0+3+j1][j0+3+j2]=tt6*jacob*k4[j1][j2];
      	}
      }
    }
  }
  return kk;
};

// �ߓ_���W���Ǐ����W�n�ɕϊ�����
// d - �����]���}�g���b�N�X
// p - �v�f�ߓ_
function toLocal(d,p){
  var x=[];
  for(var i=0;i<p.length;i++){
    x[i]=new THREE.Vector3().set
      (d[0][0]*p[i].x+d[1][0]*p[i].y+d[2][0]*p[i].z,
       d[0][1]*p[i].x+d[1][1]*p[i].y+d[2][1]*p[i].z,
       d[0][2]*p[i].x+d[1][2]*p[i].y+d[2][2]*p[i].z);
  }
  return x;
}

inherits(ShellElement,FElement);
inherits(TriElement1,ShellElement);
inherits(QuadElement1,ShellElement);

var I_YMZ=[1,5,7,11];	// y������,z�����萬���̃C���f�b�N�X
var I_ZMY=[2,4,8,10];	// z������,y�����萬���̃C���f�b�N�X

//--------------------------------------------------------------------//
// ���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - ���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// axis - �f�ʊ�����x�N�g��
var BarElement=function(label,material,param,nodes,axis){
  FElement.call(this,label,material,nodes);
  this.param=param;
  this.isBar=true;
  if((axis!==null) && (axis!==undefined)) axis.normalize();
  this.axis=axis;
};

// �ߓ_����Ԃ�
BarElement.prototype.nodeCount=function(){
  return 2;
};

// �v�f���E����Ԃ�
BarElement.prototype.borderCount=function(){
  return 1;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
BarElement.prototype.border=function(element,index){
  var p=this.nodes;
  switch(index){
    default:
      return null;
    case 0:
      return new EdgeBorder1(element,[p[0],p[1]]);
  }
};

// �v�f���E�ӂ�Ԃ� ���v�f�ł͗v�f���E�Ɠ���
// element - �v�f���x��
// index - �v�f���E�ӂ̃C���f�b�N�X
BarElement.prototype.borderEdge=function(element,index){
  return this.border(element,index);
};

// �v�f�ߓ_�̊p�x��Ԃ� ���v�f�ł͂P
// p - �v�f�ߓ_
BarElement.prototype.angle=function(p){
  return [1,1];
};

// �����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// material - �ޗ�
// sect - ���f�ʃp�����[�^
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

// �g�U�}�g���b�N�X [ ���Ni�E��NjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
// sect - ���f�ʃp�����[�^
BarElement.prototype.gradMatrix=function(p,coef,sect){
  var c=coef*sect.area/p[0].distanceTo(p[1]);
  return [[c,-c],[-c,c]];
};

// �􉽍����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// material - �ޗ�
// sect - ���f�ʃp�����[�^
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

// �ߓ_�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// material - �ޗ�
// sect - ���f�ʃp�����[�^
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

// �v�f�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// material - �ޗ�
// sect - ���f�ʃp�����[�^
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

// �v�f��\���������Ԃ�
// materials - �ޗ�
// params - ���p�����[�^
// p - �ߓ_
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
// Bernoulli-Euler���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - ���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// axis - �f�ʊ�����x�N�g��
var BEBarElement=function(label,material,param,nodes,axis){
  BarElement.call(this,label,material,param,nodes,axis);
};

// �v�f���E���̂�Ԃ�
BEBarElement.prototype.getName=function(){
  return 'BEBarElement';
};

// �����}�g���b�N�X�̗��Ȃ�������Ԃ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
BEBarElement.prototype.stiffBend=function(l,material,sect){
  var kc=material.ee/(l*l*l),kcy=kc*sect.iy,kcz=kc*sect.iz;
  var kcy12=12*kcy,kcy6l=6*kcy*l,kcyll=kcy*l*l;
  var kcz12=12*kcz,kcz6l=6*kcz*l,kczll=kcz*l*l;
  return [[kcy12,kcy6l,-kcy12,kcy6l],[kcy6l,4*kcyll,-kcy6l,2*kcyll],
      	  [-kcy12,-kcy6l,kcy12,-kcy6l],[kcy6l,2*kcyll,-kcy6l,4*kcyll],
      	  [kcz12,-kcz6l,-kcz12,-kcz6l],[-kcz6l,4*kczll,kcz6l,2*kczll],
      	  [-kcz12,kcz6l,kcz12,kcz6l],[-kcz6l,2*kczll,kcz6l,4*kczll]];
};


// �Ȃ��ȗ��E����f�c��Ԃ�
// v - �Ǐ����W�n�̕ψ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
BEBarElement.prototype.bendCurveShare=function(v,l,material,sect){
  var ckap1=6/(l*l),ckap2=4/l,ckap3=0.5*ckap2;
  var kpy=[ckap1*(v[1]-v[7])+ckap2*v[5]+ckap3*v[11],
      	   ckap1*(v[1]-v[7])+ckap3*v[5]+ckap2*v[11]];
  var kpz=[-ckap1*(v[2]-v[8])+ckap2*v[4]+ckap3*v[10],
      	   -ckap1*(v[2]-v[8])+ckap3*v[4]+ckap2*v[10]];
  return [kpy,kpz,0,0];
};

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
// sect - ���f�ʃp�����[�^
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
// Timoshenko���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - ���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// axis - �f�ʊ�����x�N�g��
var TBarElement=function(label,material,param,nodes,axis){
  BarElement.call(this,label,material,param,nodes,axis);
};

// �v�f���E���̂�Ԃ�
TBarElement.prototype.getName=function(){
  return 'TBarElement';
};

// �����}�g���b�N�X�̗��Ȃ�������Ԃ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
TBarElement.prototype.stiffBend=function(l,material,sect){
  var kb=material.ee/l,kby=kb*sect.iy,kbz=kb*sect.iz;
  var ksc1=sect.shearCoef()*material.gg*sect.area/l;
  var ksc2y=12*kby/l,kscy=ksc1*ksc2y/(ksc1+ksc2y);	// MacNeal�̕␳
  var ksc2z=12*kbz/l,kscz=ksc1*ksc2z/(ksc1+ksc2z);
  var lh=0.5*l,ksyl=kscy*lh,ksyl2=ksyl*lh,kszl=kscz*lh,kszl2=kszl*lh;
  return [[kscy,ksyl,-kscy,ksyl],[ksyl,kby+ksyl2,-ksyl,-kby+ksyl2],
      	  [-kscy,-ksyl,kscy,-ksyl],[ksyl,-kby+ksyl2,-ksyl,kby+ksyl2],
      	  [kscz,-kszl,-kscz,-kszl],[-kszl,kbz+kszl2,kszl,-kbz+kszl2],
      	  [-kscz,kszl,kscz,kszl],[-kszl,-kbz+kszl2,kszl,kbz+kszl2]];
};

// �Ȃ��ȗ��E����f�c��Ԃ�
// v - �Ǐ����W�n�̕ψ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
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

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
// sect - ���f�ʃp�����[�^
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

var PRECISION=1e-10;	// �}�g���b�N�X���x
var LU_METHOD=0;	// LU����@
var ILUCG_METHOD=1;	// �s���SLU�����������z�@

//--------------------------------------------------------------------//
// �A�������������I�u�W�F�N�g
var Solver=function(){
  this.matrix=[];		// �s��
  this.matrix2=[];		// ��Q�s��
  this.vector=[];		// �x�N�g��
  this.dof=0;			// ���f�����R�x
  this.method=LU_METHOD;	// ��������@
};

// �f�[�^����������
Solver.prototype.clear=function(){
  this.matrix.length=0;
  this.matrix2.length=0;
  this.vector.length=0;
  this.dof=0;
};

// �����}�g���b�N�X�E�׏d�x�N�g�����쐬����
Solver.prototype.createStiffnessMatrix=function(){
  var i,bc=model.bc,bcList=bc.bcList,reducedList=[];
  for(i=0;i<bcList.length;i++){
    if(bcList[i]<0){
      reducedList.push(i);
    }
  }

// �����}�g���b�N�X�E�׏d�x�N�g���̍쐬
  var matrix1=stiffnessMatrix(this.dof),vector1=loadVector(this.dof);

// �S�����R�x����������
  for(i=0;i<bcList.length;i++){
    if(bcList[i]>=0){
      var rx=bc.getRestDisp(bcList[i]);
      for(var j=0;j<vector1.length;j++){
      	if(i in matrix1[j]){
      	  vector1[j]-=rx*matrix1[j][i];
      	}
      }
    }
  }
  this.extruct(matrix1,vector1,reducedList);
};

// �����}�g���b�N�X�E���ʃ}�g���b�N�X���쐬����
Solver.prototype.createStiffMassMatrix=function(){
  var i,bc=model.bc,bcList=bc.bcList,reducedList=[];
  for(i=0;i<bcList.length;i++){
    if(bcList[i]<0){
      reducedList.push(i);
    }
  }
  var matrix1=stiffnessMatrix(this.dof),matrix2=massMatrix(this.dof);

  this.matrix.length=0;
  this.matrix2.length=0;
  for(i=0;i<reducedList.length;i++){
    this.matrix[i]=extructRow(matrix1[reducedList[i]],reducedList);
    this.matrix2[i]=extructRow(matrix2[reducedList[i]],reducedList);
  }
};

// �􉽍����}�g���b�N�X���쐬����
Solver.prototype.createGeomStiffMatrix=function(){
  var i,bc=model.bc,bcList=bc.bcList,reducedList=[];
  for(i=0;i<bcList.length;i++){
    if(bcList[i]<0){
      reducedList.push(i);
    }
  }
  var matrix2=geomStiffnessMatrix(this.dof);

  this.matrix2.length=0;
  for(i=0;i<reducedList.length;i++){
    this.matrix2[i]=extructRow(matrix2[reducedList[i]],reducedList);
  }
};

// �M�v�Z�̃}�g���b�N�X�E�x�N�g�����v�Z����
Solver.prototype.createHeatMatrix=function(){
  var i,bcList=model.bc.bcList,reducedList=[];
  for(i=0;i<bcList.length;i++){
    if(bcList[i]<0){
      reducedList.push(i);
    }
  }

// �`�M�}�g���b�N�X�E�M���E�����x�N�g���̍쐬
  var matrix1=heatMatrix(),vector1=tempVector(matrix1);

// �S�����R�x����������
  for(i=0;i<bcList.length;i++){
    if(bcList[i]>=0){
      var t=model.bc.temperature[bcList[i]];
      for(var j=0;j<vector1.length;j++){
      	if(i in matrix1[j]){
      	  vector1[j]-=t.t*matrix1[j][i];
      	}
      }
    }
  }
  this.extruct(matrix1,vector1,reducedList);
};

// �s��̈ꕔ�𒊏o����
// matrix1,vector1 - ���̃}�g���b�N�X,�x�N�g��
// list - ���o�����̃��X�g
Solver.prototype.extruct=function(matrix1,vector1,list){
  this.matrix.length=0;
  this.vector.length=0;
  for(var i=0;i<list.length;i++){
    this.vector[i]=vector1[list[i]];
    this.matrix[i]=extructRow(matrix1[list[i]],list);
  }
};

// �A��������������
Solver.prototype.solve=function(){
  switch(this.method){
    case LU_METHOD:
      var a=numeric.ccsSparse(this.matrix);
      return numeric.ccsLUPSolve(numeric.ccsLUP(a),this.vector);
    case ILUCG_METHOD:
      return solveILU(toSparse(this.matrix),getILU(this.matrix),
      	      	      this.vector);
  }
};

// �����`���X�@�ŌŗL�l�E�ŗL�x�N�g�������߂�
// n - �R�d�Ίp���s��̑傫��
// delta - �V�t�g�ʃ�
Solver.prototype.eigenByLanczos=function(n,delta){
  switch(this.method){
    case LU_METHOD:
      return eigByLanczosLUP(this.matrix,this.matrix2,n,delta);
    case ILUCG_METHOD:
      return eigByLanczosILUCG(this.matrix,this.matrix2,n,delta);
  }
};

// �A�[�m���f�B�@�ŌŗL�l�E�ŗL�x�N�g�������߂�
// n - �R�d�Ίp���s��̑傫��
// delta - �V�t�g�ʃ�
Solver.prototype.eigenByArnoldi=function(n,delta){
  switch(this.method){
    case LU_METHOD:
      return eigByArnoldiLUP(this.matrix,this.matrix2,n,delta);
    case ILUCG_METHOD:
      return eigByArnoldiILUCG(this.matrix,this.matrix2,n,delta);
  }
};

// ���ʃ}�g���b�N�X���쐬����
// dof - ���f�����R�x
function massMatrix(dof){
  var mesh=model.mesh,elements=mesh.elements,matrix=[],i,j,mm,mmax=0;
  for(i=0;i<dof;i++) matrix[i]=[];
  for(i=0;i<elements.length;i++){
    var elem=elements[i];
    var material=model.materials[elem.material],dens=material.dens;
    if(elem.isShell){
      var sp=model.shellParams[elem.param];
      mm=elem.massMatrix(mesh.getNodes(elem),dens,sp.thickness);
      mmax=setElementMatrix(elem,6,matrix,mm,mmax);
    }
    else if(elem.isBar){
      var sect=model.barParams[elem.param].section;
      mm=elem.massMatrix(mesh.getNodes(elem),dens,sect);
      mmax=setElementMatrix(elem,6,matrix,mm,mmax);
    }
    else{
      mm=elem.massMatrix(mesh.getNodes(elem),dens);
      mmax=setElementMatrix(elem,3,matrix,mm,mmax);
    }
  }
// ���W�ϊ�
  var rests=model.bc.restraints;
  var index=model.bc.nodeIndex,bcdof=model.bc.dof;
  for(i=0;i<rests.length;i++){
    var ri=rests[i];
    if(ri.coords){
      ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i]);
    }
  }
// ��Βl����������������������
  var eps=PRECISION*mmax;
  for(i=0;i<dof;i++){
    var mrow=matrix[i];
    for(j in mrow){
      if(mrow.hasOwnProperty(j)){
      	j=parseInt(j);
      	if(Math.abs(mrow[j])<eps){
      	  delete mrow[j];
      	}
      }
    }
  }
  return matrix;
}

// �����}�g���b�N�X���쐬����
// dof - ���f�����R�x
function stiffnessMatrix(dof){
  var mesh=model.mesh,elements=mesh.elements,matrix=[],i,j,km,kmax=0;
  for(i=0;i<dof;i++) matrix[i]=[];
  for(i=0;i<elements.length;i++){
    var elem=elements[i];
    var material=model.materials[elem.material],mat=material.matrix;
    if(elem.isShell){
      var sp=model.shellParams[elem.param];
      if(elem.getName()==='TriElement1'){
      	km=elem.stiffnessMatrix(mesh.getNodes(elem),mat.m2d,sp);
      }
      else{
      	km=elem.stiffnessMatrix(mesh.getNodes(elem),mat.msh,sp);
      }
      kmax=setElementMatrix(elem,6,matrix,km,kmax);
    }
    else if(elem.isBar){
      var sect=model.barParams[elem.param].section;
      km=elem.stiffnessMatrix(mesh.getNodes(elem),material,sect);
      kmax=setElementMatrix(elem,6,matrix,km,kmax);
    }
    else{
      km=elem.stiffnessMatrix(mesh.getNodes(elem),mat.m3d);
      kmax=setElementMatrix(elem,3,matrix,km,kmax);
    }
  }
// ���W�ϊ�
  var rests=model.bc.restraints;
  var index=model.bc.nodeIndex,bcdof=model.bc.dof;
  for(i=0;i<rests.length;i++){
    var ri=rests[i];
    if(ri.coords){
      ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i]);
    }
  }
// ��Βl����������������������
  var eps=PRECISION*kmax;
  for(i=0;i<dof;i++){
    var mrow=matrix[i];
    for(j in mrow){
      if(mrow.hasOwnProperty(j)){
      	j=parseInt(j);
      	if(Math.abs(mrow[j])<eps){
      	  delete mrow[j];
      	}
      }
    }
  }
  return matrix;
}

// �􉽍����}�g���b�N�X���쐬����
// dof - ���f�����R�x
function geomStiffnessMatrix(dof){
  var mesh=model.mesh,elements=mesh.elements,nodes=mesh.nodes;
  var disp=model.result.displacement;
  var matrix=[],i,j,km,kmax=0,p=[],v=[];
  for(i=0;i<dof;i++) matrix[i]=[];
  for(i=0;i<elements.length;i++){
    var elem=elements[i],en=elem.nodes;
    p.length=0;
    v.length=0;
    for(j=0;j<en.length;j++){
      p[j]=nodes[en[j]];
      v[j]=disp[en[j]];
    }
    var material=model.materials[elem.material],mat=material.matrix;
    if(elem.isShell){
      var sp=model.shellParams[elem.param];
      if(elem.getName()==='TriElement1'){
      	km=elem.geomStiffnessMatrix(p,v,mat.m2d,sp);
      }
      else{
      	km=elem.geomStiffnessMatrix(p,v,mat.msh,sp);
      }
      kmax=setElementMatrix(elem,6,matrix,km,kmax);
    }
    else if(elem.isBar){
      var sect=model.barParams[elem.param].section;
      km=elem.geomStiffnessMatrix(p,v,material,sect);
      kmax=setElementMatrix(elem,6,matrix,km,kmax);
    }
    else{
      km=elem.geomStiffnessMatrix(p,v,mat.m3d);
      kmax=setElementMatrix(elem,3,matrix,km,kmax);
    }
  }
// ���W�ϊ�
  var rests=model.bc.restraints;
  var index=model.bc.nodeIndex,bcdof=model.bc.dof;
  for(i=0;i<rests.length;i++){
    var ri=rests[i];
    if(ri.coords){
      ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i]);
    }
  }
// ��Βl�������������������E�������]
  var eps=PRECISION*kmax;
  for(i=0;i<dof;i++){
    var mrow=matrix[i];
    for(j in mrow){
      if(mrow.hasOwnProperty(j)){
      	j=parseInt(j);
      	if(Math.abs(mrow[j])<eps){
      	  delete mrow[j];
      	}
      	else{
      	  mrow[j]=-mrow[j];
      	}
      }
    }
  }
  return matrix;
}

// �v�f�̃}�g���b�N�X��ݒ肷��
// element - �v�f
// dof - ���R�x
// matrix - �S�̍����}�g���b�N�X
// km - �v�f�̍����}�g���b�N�X
// kmax - �����̐�Βl�̍ő�l
function setElementMatrix(element,dof,matrix,km,kmax){
  var nodeCount=element.nodeCount();
  var index=model.bc.nodeIndex,nodes=element.nodes;
  for(var i=0;i<nodeCount;i++){
    var row0=index[nodes[i]],i0=dof*i;
    for(var j=0;j<nodeCount;j++){
      var column0=index[nodes[j]],j0=dof*j;
      for(var i1=0;i1<dof;i1++){
      	var mrow=matrix[row0+i1],krow=km[i0+i1];
      	for(var j1=0;j1<dof;j1++){
      	  var cj1=column0+j1;
      	  if(cj1 in mrow){
      	    mrow[cj1]+=krow[j0+j1];
      	    kmax=Math.max(kmax,Math.abs(mrow[cj1]));
      	  }
      	  else{
      	    mrow[cj1]=krow[j0+j1];
      	    kmax=Math.max(kmax,Math.abs(mrow[cj1]));
      	  }
      	}
      }
    }
  }
  return kmax;
}

// �׏d�x�N�g�����쐬����
// dof - ���f�����R�x
function loadVector(dof){
  var loads=model.bc.loads,press=model.bc.pressures;
  var vector=numeric.rep([dof],0);
  var i,j,index0,index=model.bc.nodeIndex,bcdof=model.bc.dof;
  for(i=0;i<loads.length;i++){
    var ld=loads[i],nd=ld.node,ldx=ld.globalX,ldof=bcdof[nd];
    index0=index[nd];
    for(j=0;j<ldof;j++){
      vector[index0+j]=ldx[j];
    }
  }
  for(i=0;i<press.length;i++){
    var border=press[i].getBorder
      (model.mesh.elements[press[i].element]);
    var p=model.mesh.getNodes(border);
    var ps=border.shapeFunctionVector(p,press[i].press);
    var norm=normalVector(p);
    var count=border.nodeCount();
    for(j=0;j<count;j++){
      index0=index[border.nodes[j]];
      vector[index0]-=ps[j]*norm.x;
      vector[index0+1]-=ps[j]*norm.y;
      vector[index0+2]-=ps[j]*norm.z;
    }
  }
  var rests=model.bc.restraints;
  for(i=0;i<rests.length;i++){
    var ri=rests[i];
    if(ri.coords){
      ri.coords.transVector(vector,dof,index[ri.node],bcdof[i]);
    }
  }
  return vector;
}

// �`�M�}�g���b�N�X���쐬����
function heatMatrix(){
  var elements=model.mesh.elements,mesh=model.mesh;
  var dof=model.mesh.nodes.length,matrix=[],i;
  for(i=0;i<dof;i++) matrix[i]=[];
  for(i=0;i<elements.length;i++){
    var elem=elements[i],count=elem.nodeCount();
    var h=model.materials[elem.material].hCon,ls;
    if(elem.isShell){
      var sp=model.shellParams[elem.param];
      ls=elem.gradMatrix(mesh.getNodes(elem),h,sp);
    }
    else if(elem.isBar){
      var sect=model.barParams[elem.param].section;
      ls=elem.gradMatrix(mesh.getNodes(elem),h,sect);
    }
    else{
      ls=elem.gradMatrix(mesh.getNodes(elem),h);
    }
    for(var i1=0;i1<count;i1++){
      var mrow=matrix[elem.nodes[i1]],lrow=ls[i1];
      for(var j1=0;j1<count;j1++){
      	if(elem.nodes[j1] in mrow){
      	  mrow[elem.nodes[j1]]+=lrow[j1];
      	}
      	else{
      	  mrow[elem.nodes[j1]]=lrow[j1];
      	}
      }
    }
  }
  return matrix;
}

// �M���E�����x�N�g�����쐬����
// matrix - �`�M�}�g���b�N�X
function tempVector(matrix){
  var htcs=model.bc.htcs,i;
  var vector=numeric.rep([model.mesh.nodes.length],0);
  for(i=0;i<htcs.length;i++){
    var elem=model.mesh.elements[htcs[i].element];
    var border=htcs[i].getBorder(elem);
    var p=model.mesh.getNodes(border);
    var h=htcs[i].htc;
    if(border.isEdge){
      var sp=model.shellParams[elem.param];
      h*=sp.thickness;
    }
    var hm=border.shapeFunctionMatrix(p,h);
    var hv=border.shapeFunctionVector(p,h*htcs[i].outTemp);
    var count=border.nodeCount();
    for(var i1=0;i1<count;i1++){
      var mrow=matrix[border.nodes[i1]],hrow=hm[i1];
      for(var j1=0;j1<count;j1++){
      	if(border.nodes[j1] in mrow){
      	  mrow[border.nodes[j1]]+=hrow[j1];
      	}
      	else{
      	  mrow[border.nodes[j1]]=hrow[j1];
      	}
      }
      vector[border.nodes[i1]]+=hv[i1];
    }
  }
  return vector;
}

// �s��̍s����ꕔ�𒊏o����
// mrow - ���̃}�g���b�N�X�̍s�f�[�^
// list - ���o�����̃��X�g
function extructRow(mrow,list){
  var exrow=[],col=[],i1=0,j1=0;
  for(var j in mrow){
    if(mrow.hasOwnProperty(j)){
      col.push(parseInt(j));
    }
  }
  col.sort(function(j1,j2){return j1-j2;});
  while((i1<col.length) && (j1<list.length)){
    if(col[i1]==list[j1]){
      exrow[j1]=mrow[col[i1]];
      i1++;
      j1++;
    }
    else if(col[i1]<list[j1]){
      i1++;
    }
    else{
      j1++;
    }
  }
  return exrow;
}

// �v�Z���J�n����
function calcStart(){
  info.textContent='�v�Z���E�E�E';
  var elems=document.getElementsByName('method');
  if(elems[0].checked){
    model.solver.method=LU_METHOD;
  }
  else if(elems[1].checked){
    model.solver.method=ILUCG_METHOD;
  }
  elems=document.getElementsByName('restype');
  if(elems[0].checked){
    model.result.type=NODE_DATA;
  }
  else if(elems[1].checked){
    model.result.type=ELEMENT_DATA;
  }
  hideModalWindow(CALC_WINDOW);
  elems=document.getElementsByName('calctype');
  if(elems[0].checked){
    setTimeout(statCalcStart,10);
  }
  else if(elems[1].checked){
    setTimeout(vibCalcStart,10);
  }
  else{
    setTimeout(buckCalcStart,10);
  }
}

// �É�͂̌v�Z���J�n����
function statCalcStart(){
  try{
    model.calculate();
    resultView.setInitStatic();
    showInfo();
  }
  catch(ex){
    alert(ex);
  }
}

// �ŗL�U����͂̌v�Z���J�n����
function vibCalcStart(){
  try{
    var count=parseInt(document.getElementById('eigennumber').value);
    model.charVib(count);
    resultView.setInitEigen();
  }
  catch(ex){
    alert(ex);
  }
}

// ���`������͂̌v�Z���J�n����
function buckCalcStart(){
//  try{
    var count=parseInt(document.getElementById('eigennumber').value);
    model.calcBuckling(count);
    resultView.setInitEigen();
/*  }
  catch(ex){
    alert(ex);
  }*/
}

// �v�Z�ݒ�E�B���h�E��\������
function showCalc(){
  showModalWindow(CALC_WINDOW);
  var elems=document.getElementsByName('method');
  elems[model.solver.method].checked=true;
}

// �v�Z�ݒ��������
function calcCancel(){
  hideModalWindow(CALC_WINDOW);
}

//--------------------------------------------------------------------//
// ������@�i�s���SLU�����������z�@�j

var ILUCG_THRES=1e-10;	// �s���SLU�����������z�@�̎���臒l�̃f�t�H���g�l

// �s���SLU����������
// a - �s��
function getILU(a){
  var m=a.length,i,j,diag=[],col=[],val=[],d=[],colData=[];
  for(i=0;i<m;i++){
    col[i]=[];
    val[i]=[];
    diag[i]=-1;
    colData[i]=[];
  }
// �񂩂�̌����p�|�C���^��ݒ肷��
  for(i=0;i<m;i++){
    var arow=a[i];
    var cols=[];
    for(j in arow){
      if(arow.hasOwnProperty(j)){
      	cols.push(parseInt(j));
      }
    }
    cols.sort(function(c1,c2){return c1-c2;});
    for(j=0;j<cols.length;j++){
      var cj=cols[j];
      if(cj===i){
      	diag[i]=j;
      	d[cj]=colData[cj].length;
      }
      col[i].push(cj);
      val[i].push(arow[cj]);
      colData[cj].push(i);
    }
  }
// �s���SLU����������
  for(var k=0;k<m-1;k++){
    var vk=val[k],ck=col[k],dk=diag[k],cdk=colData[k];
    if((dk<0) || (vk[dk]===0)) throw new Error('�Ίp������0�ł�');
    var dkk=1/vk[dk];
    for(j=dk+1;j<ck.length;j++){
      vk[j]*=dkk;
    }
    var i0=d[k]+1,i1=cdk.length;
    for(i=i0;i<i1;i++){
      var row=cdk[i],vrow=val[row],crow=col[row];
      var c=crow.indexOf(k);
      if(c>=0){
      	var vik=-vrow[c];
      	for(j=dk+1;j<ck.length;j++){
      	  c=crow.indexOf(ck[j]);
      	  if(c>=0){
      	    vrow[c]+=vik*vk[j];
      	  }
      	}
      }
    }
  }
  var rowData=[],valData=[],count=0;
  colData.length=0;
  rowData.push(count);
  for(i=0;i<m;i++){
    diag[i]+=count;
    count+=col[i].length;
    rowData.push(count);
    Array.prototype.push.apply(colData,col[i]);
    Array.prototype.push.apply(valData,val[i]);
    valData[diag[i]]=1/valData[diag[i]];
  }
  return [rowData,colData,valData,diag];
}

// LU����@�ŘA���������̉������߂�
// lu - LU���������a�s��
// p - �x�N�g��
function solveLU(lu,p){
  var row=lu[0],col=lu[1],val=lu[2],diag=lu[3],m=row.length-1;
  var q=[],i,j,j1;
  q[0]=p[0]*val[diag[0]];
  for(i=1;i<m;i++){
    var p2=p[i],j0=row[i];
    j1=diag[i];
    for(j=j0;j<j1;j++){
      p2-=val[j]*q[col[j]];
    }
    q[i]=p2*val[j1];
  }
  for(i=m-2;i>=0;i--){
    j1=diag[i]+1;
    var q2=q[i],j2=row[i+1];
    for(j=j1;j<j2;j++){
      q2-=val[j]*q[col[j]];
    }
    q[i]=q2;
  }
  return q;
}

// �s���SLU�����������z�@�ŘA���������̉������߂�
// matrix - ���̍s��
// ilu - �s���SLU���������a�s��
// p - �x�N�g��
// iterMax - �����񐔂̏��
// thres - ����臒l
function solveILU(matrix,ilu,p,iterMax,thres){
  iterMax=iterMax||p.length;
  thres=thres||ILUCG_THRES;
  var x=numeric.rep([p.length],0),xg=p.concat();
  var xq=solveLU(ilu,xg);
  var xd=xq.concat(),j;
  for(var iter=0;iter<iterMax;iter++){
    var z1=numeric.dotVV(xd,xg);
    xq=sparseDotMV(matrix,xd);
    var r=numeric.dotVV(xd,xq);
    if(Math.abs(r)===0){
      throw new Error('�����������G���[ at iter='+iter);
    }
    var alpha=z1/r;
    for(j=0;j<xg.length;j++){
      x[j]+=alpha*xd[j];
      xg[j]-=alpha*xq[j];
    }
    if(numeric.dotVV(xg,xg)<thres) return x;
    var xq2=solveLU(ilu,xg);
    var z2=numeric.dotVV(xq,xq2);
    var beta=-z2/r;
    for(j=0;j<xg.length;j++){
      xd[j]=beta*xd[j]+xq2[j];
    }
  }
  return x;
}

// �s��ƃx�N�g���̐ς��v�Z����
// matrix - �a�s��
// x - �x�N�g��
function sparseDotMV(matrix,x){
  var row=matrix[0],col=matrix[1],val=matrix[2],m=row.length-1,y=[];
  for(var i=0;i<m;i++){
    var y0=0,j0=row[i],j1=row[i+1];
    for(var j=j0;j<j1;j++){
      y0+=val[j]*x[col[j]];
    }
    y[i]=y0;
  }
  return y;
}

// �s�x�[�X�̑a�s��ɕϊ�����
// a - ���̍s��
function toSparse(a){
  var m=a.length,count=0,row=[],col=[],val=[],j;
  row.push(count);
  for(var i=0;i<m;i++){
    var arow=a[i];
    var cols=[];
    for(j in arow){
      if(arow.hasOwnProperty(j)){
      	cols.push(parseInt(j));
      }
    }
    cols.sort(function(c1,c2){return c1-c2;});
    count+=cols.length;
    row.push(count);
    Array.prototype.push.apply(col,cols);
    for(j=0;j<cols.length;j++){
      val.push(arow[cols[j]]);
    }
  }
  return [row,col,val];
}

//--------------------------------------------------------------------//
// �����`���X�@�ɂ��ŗL�l�v�Z

var EIG_SHIFT=-0.1;	// �V�t�g�ʃ̃f�t�H���g�l

// �����`���X�@�ň�ʌŗL�l���Ax=��Bx�̂R�d�Ίp��������
// a,b - �s��A,B
// n - �R�d�Ίp���s��̑傫��
function eigByLanczos(a,b,n){
  var size=a.length,u=[],alp=[],bet=[],bau,al,bt,i;
  n=Math.min(n||size,size);
  var blup=numeric.ccsLUP(numeric.ccsSparse(b));
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  a=toSparse(a);
  b=toSparse(b);
  var ubu=sparseDotVMV(b,u[0]);
  u[0]=numeric.mul(1/Math.sqrt(Math.abs(ubu)),u[0]);
  for(i=0;i<n-1;i++){
    al=sparseDotVMV(a,u[i]);
    alp.push(al);
    bau=numeric.ccsLUPSolve(blup,sparseDotMV(a,u[i]));
    var v=numeric.sub(bau,numeric.mul(al,u[i]));
    if(i>0) v=numeric.sub(v,numeric.mul(bt,u[i-1]));
    for(var j=0;j<=i;j++){
      var uv=numeric.dot(u[j],sparseDotMV(b,v));
      v=numeric.sub(v,numeric.mul(uv,u[j]));
    }
    ubu=sparseDotVMV(b,v);
    bt=Math.sqrt(Math.abs(ubu));
    bet.push(bt);
    u[i+1]=numeric.mul(1/bt,v);
  }
  al=sparseDotVMV(a,u[n-1]);
  alp.push(al);
  var eig=tdmEig(alp,bet);
  return {lambda:eig[0],ut:numeric.dot(eig[1],u)};
}

// �����`���X�@�ň�ʌŗL�l���Ax=��Bx�̂R�d�Ίp��������
// �������ŗL�l��D�悳���邽�� B(A+��B)^-1Bx=��Bx �ɕϊ�����
// �t�s���LU����@
// a,b - �s��A,B
// n - �R�d�Ίp���s��̑傫��
// delta - �V�t�g�ʃ�
function eigByLanczosLUP(a,b,n,delta){
  var size=a.length,u=[],alp=[],bet=[],bu,abu,al,bt,i,j;
  n=Math.min(n||size,size);
  if((delta===null) || (delta===undefined)) delta=EIG_SHIFT;
  var ablup=numeric.ccsLUP(numeric.ccsSparse(shiftMatrix(a,b,delta)));
  b=toSparse(b);
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  var ubu=sparseDotVMV(b,u[0]);
  u[0]=numeric.mul(1/Math.sqrt(Math.abs(ubu)),u[0]);
  for(i=0;i<n;i++){
    bu=sparseDotMV(b,u[i]);
    abu=numeric.ccsLUPSolve(ablup,bu);
    al=numeric.dot(bu,abu);
    alp.push(al);
    if(i<n-1){
      var v=numeric.sub(abu,numeric.mul(al,u[i]));
      if(i>0) v=numeric.sub(v,numeric.mul(bt,u[i-1]));
      for(j=0;j<=i;j++){
      	var uv=numeric.dot(u[j],sparseDotMV(b,v));
      	v=numeric.sub(v,numeric.mul(uv,u[j]));
      }
      ubu=sparseDotVMV(b,v);
      bt=Math.sqrt(Math.abs(ubu));
      bet.push(bt);
      u[i+1]=numeric.mul(1/bt,v);
    }
  }
  var eig=tdmEig(alp,bet);
  var e1=eig[0],e2=[];
  for(i=0;i<e1.length;i++) e2[i]=1/e1[i]+delta;
  return {lambda:e2,ut:numeric.dot(eig[1],u)};
}

// �����`���X�@�ň�ʌŗL�l���Ax=��Bx�̂R�d�Ίp��������
// �������ŗL�l��D�悳���邽�� B(A+��B)^-1Bx=��Bx �ɕϊ�����
// �t�s���ILUCG�@
// a,b - �s��A,B
// n - �R�d�Ίp���s��̑傫��
// delta - �V�t�g�ʃ�
function eigByLanczosILUCG(a,b,n,delta){
  var size=a.length,u=[],alp=[],bet=[],bu,abu,al,bt,i,j;
  n=Math.min(n||size,size);
  if((delta===null) || (delta===undefined)) delta=EIG_SHIFT;
  var adb=shiftMatrix(a,b,delta);
  var ablu=getILU(adb);
  adb=toSparse(adb);
  b=toSparse(b);
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  var ubu=sparseDotVMV(b,u[0]);
  u[0]=numeric.mul(1/Math.sqrt(Math.abs(ubu)),u[0]);
  for(i=0;i<n;i++){
    bu=sparseDotMV(b,u[i]);
    abu=solveILU(adb,ablu,bu);
    al=numeric.dot(bu,abu);
    alp.push(al);
    if(i<n-1){
      var v=numeric.sub(abu,numeric.mul(al,u[i]));
      if(i>0) v=numeric.sub(v,numeric.mul(bt,u[i-1]));
      for(j=0;j<=i;j++){
      	var uv=numeric.dot(u[j],sparseDotMV(b,v));
      	v=numeric.sub(v,numeric.mul(uv,u[j]));
      }
      ubu=sparseDotVMV(b,v);
      bt=Math.sqrt(Math.abs(ubu));
      bet.push(bt);
      u[i+1]=numeric.mul(1/bt,v);
    }
  }
  var eig=tdmEig(alp,bet),e1=eig[0],e2=[];
  for(i=0;i<e1.length;i++) e2[i]=1/e1[i]+delta;
  return {lambda:e2,ut:numeric.dot(eig[1],u)};
}

// �A�[�m���f�B�@�ň�ʌŗL�l���Ax=��Bx�̂R�d�Ίp��������
// �������ŗL�l��D�悳���邽�� (A+��B)^-1Bx=��x �ɕϊ�����
// �t�s���LU����@
// a,b - �s��A,B
// n - �R�d�Ίp���s��̑傫��
// delta - �V�t�g�ʃ�
function eigByArnoldiLUP(a,b,n,delta){
  var size=a.length,u=[],bu,abu,h1,i,j;
  n=Math.min(n||size,size);
  var h=numeric.rep([n,n],0);
  if((delta===null) || (delta===undefined)) delta=EIG_SHIFT;
  var ablup=numeric.ccsLUP(numeric.ccsSparse(shiftMatrix(a,b,delta)));
  b=toSparse(b);
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  var uu=numeric.dotVV(u[0],u[0]);
  u[0]=numeric.mul(1/Math.sqrt(uu),u[0]);
  for(i=0;i<n;i++){
    bu=sparseDotMV(b,u[i]);
    abu=numeric.ccsLUPSolve(ablup,bu);
    var v=abu;
    for(j=0;j<=i;j++){
      h1=numeric.dot(u[j],abu);
      h[j][i]=h1;
      v=numeric.sub(v,numeric.mul(h1,u[j]));
    }
    if(i<n-1){
      for(j=0;j<=i;j++){
      	var uv=numeric.dot(u[j],v);
      	v=numeric.sub(v,numeric.mul(uv,u[j]));
      }
      uu=numeric.dotVV(v,v);
      h1=Math.sqrt(uu);
      h[i+1][i]=h1;
      u[i+1]=numeric.mul(1/h1,v);
    }
  }
  var eig=eigen(h),e1=eig[0],e2=[];
  for(i=0;i<e1.length;i++) e2[i]=1/e1[i]+delta;
  return {lambda:e2,ut:numeric.dot(eig[1],u)};
}

// �A�[�m���f�B�@�ň�ʌŗL�l���Ax=��Bx�̂R�d�Ίp��������
// �������ŗL�l��D�悳���邽�� (A+��B)^-1Bx=��x �ɕϊ�����
// �t�s���ILUCG�@
// a,b - �s��A,B
// n - �R�d�Ίp���s��̑傫��
// delta - �V�t�g�ʃ�
function eigByArnoldiILUCG(a,b,n,delta){
  var size=a.length,u=[],bu,abu,h1,i,j;
  n=Math.min(n||size,size);
  var h=numeric.rep([n,n],0);
  if((delta===null) || (delta===undefined)) delta=EIG_SHIFT;
  var adb=shiftMatrix(a,b,delta);
  var ablu=getILU(adb);
  adb=toSparse(adb);
  b=toSparse(b);
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  var uu=numeric.dotVV(u[0],u[0]);
  u[0]=numeric.mul(1/Math.sqrt(uu),u[0]);
  for(i=0;i<n;i++){
    bu=sparseDotMV(b,u[i]);
    abu=solveILU(adb,ablu,bu);
    var v=abu;
    for(j=0;j<=i;j++){
      h1=numeric.dot(u[j],abu);
      h[j][i]=h1;
      v=numeric.sub(v,numeric.mul(h1,u[j]));
    }
    if(i<n-1){
      for(j=0;j<=i;j++){
      	var uv=numeric.dot(u[j],v);
      	v=numeric.sub(v,numeric.mul(uv,u[j]));
      }
      uu=numeric.dotVV(v,v);
      h1=Math.sqrt(uu);
      h[i+1][i]=h1;
      u[i+1]=numeric.mul(1/h1,v);
    }
  }
  var eig=eigen(h),e1=eig[0],e2=[];
  for(i=0;i<e1.length;i++) e2[i]=1/e1[i]+delta;
  return {lambda:e2,ut:numeric.dot(eig[1],u)};
}

// �R�d�Ίp���s��̌ŗL�l�E�ŗL�x�N�g�������߂�
// alp,bet - �R�d�Ίp���s��̑Ίp����,���ׂ̗̐���
function tdmEig(alp,bet){
  var size=alp.length,t=numeric.diag(alp);
  for(var i=0;i<size-1;i++){
    t[i][i+1]=bet[i];
    t[i+1][i]=bet[i];
  }
  return eigen(t);
}

// �s��̌ŗL�l�E�ŗL�x�N�g�������߂�
// m - �s��
function eigen(m){
  var e=[],ev=[],size=m.length,i;
  var eig=numeric.eig(m),q=numeric.transpose(eig.E.x);
  for(i=0;i<size;i++) ev.push([eig.lambda.x[i],q[i]]);
  ev.sort(function(v1,v2){return Math.abs(v2[0])-Math.abs(v1[0]);});
  for(i=0;i<size;i++){
    e[i]=ev[i][0];
    q[i]=ev[i][1];
  }
  return [e,q];
}

// �V�t�g�s��A-��B��Ԃ�
// a,b - �s��A,B
// delta - �V�t�g�ʃ�
function shiftMatrix(a,b,delta){
  var size=a.length,adb=[],j;
  for(var i=0;i<size;i++){
    var ai=a[i],bi=b[i],s=[];
    for(j in ai){
      if(ai.hasOwnProperty(j)){
      	j=parseInt(j);
      	s[j]=ai[j];
      }
    }
    if(delta!==0){
      for(j in bi){
      	if(bi.hasOwnProperty(j)){
      	  j=parseInt(j);
      	  if(j in s){
      	    s[j]-=delta*bi[j];
      	  }
      	  else{
      	    s[j]=-delta*bi[j];
      	  }
      	}
      }
    }
    adb[i]=s;
  }
  return adb;
}

// �s��ƃx�N�g���̐ς��v�Z����
// matrix - �a�s��
// x - �x�N�g��
function sparseDotVMV(matrix,x){
  var row=matrix[0],col=matrix[1],val=matrix[2],m=row.length-1,s=0;
  for(var i=0;i<m;i++){
    var j0=row[i],j1=row[i+1],xi=x[i];
    for(var j=j0;j<j1;j++){
      s+=xi*val[j]*x[col[j]];
    }
  }
  return s;
}

// �f�[�^�^
var NONE=-1;		// ��f�[�^
var DISPLACEMENT=0;	// �ψ�
var STRAIN=1;		// �c
var STRESS=2;		// ����
var S_ENERGY=3;		// �c�G�l���M�[���x
var TEMPERATURE=4;	// ���x
// ����
var X=0;		// x����
var Y=1;		// y����
var Z=2;		// z����
var RX=3;		// x����]����
var RY=4;		// y����]����
var RZ=5;		// z����]����
var XY=3;		// xy����f����
var YZ=4;		// yz����f����
var ZX=5;		// zx����f����
var MAGNITUDE=6;	// �傫��
var MAX_PRINCIPAL=7;	// �ő�听��
var MIN_PRINCIPAL=8;	// �ŏ��听��
var MID_PRINCIPAL=9;	// ���Ԏ听��
var MAX_SHARE=10;	// �ő傹��f����
var VON_MISES=11;	// �~�[�[�X����
var SHIFT=12;		// �����V�t�g��
// �ψʂ̐���
var DISP_COMPONENT=['Mag.','x','y','z'];
var DISP2_COMPONENT=['Mag.','x','y','z','rotx','roty','rotz'];
// �c�̐���
var STRAIN_COMPONENT=['Max.prin.','Min.prin.','Mid.prin.',
      	      	      'Max.share',
      	      	      'x','y','z','xy','yz','zx'];
// ���͂̐���
var STRESS_COMPONENT=['Max.prin.','Min.prin.','Mid.prin.',
      	      	      'Max.share','Von mises',
      	      	      'x','y','z','xy','yz','zx'];
// �c�G�l���M�[���x�̐���
var ENERGY_COMPONENT=['Energy'];
var COMP_MAP={'Mag.':MAGNITUDE,'x':X,'y':Y,'z':Z,
      	      'rotx':RX,'roty':RY,'rotz':RZ,'xy':XY,'yz':YZ,'zx':ZX,
      	      'Max.prin.':MAX_PRINCIPAL,'Min.prin.':MIN_PRINCIPAL,
      	      'Mid.prin.':MID_PRINCIPAL,'Max.share':MAX_SHARE,
      	      'Von mises':VON_MISES,'Energy':0,
      	      'x 1':X,'y 1':Y,'z 1':Z,'xy 1':XY,'yz 1':YZ,'zx 1':ZX,
      	      'Max.prin. 1':MAX_PRINCIPAL,'Min.prin. 1':MIN_PRINCIPAL,
      	      'Mid.prin. 1':MID_PRINCIPAL,'Max.share 1':MAX_SHARE,
      	      'Von mises 1':VON_MISES,'Energy 1':0,
      	      'x 2':X+SHIFT,'y 2':Y+SHIFT,'z 2':Z+SHIFT,
      	      'xy 2':XY+SHIFT,'yz 2':YZ+SHIFT,'zx 2':ZX+SHIFT,
      	      'Max.prin. 2':MAX_PRINCIPAL+SHIFT,
      	      'Min.prin. 2':MIN_PRINCIPAL+SHIFT,
      	      'Mid.prin. 2':MID_PRINCIPAL+SHIFT,
      	      'Max.share 2':MAX_SHARE+SHIFT,
      	      'Von mises 2':VON_MISES+SHIFT,'Energy 2':1};
var EIG_EPS=1e-10;		// �ŗL�l�v�Z�̎���臒l
var NODE_DATA=0;		// �ߓ_�f�[�^
var ELEMENT_DATA=1;		// �v�f�f�[�^
var VIBRATION='Vibration';	// �U�����
var BUCKLING='Buckling';	// �������

//--------------------------------------------------------------------//
// �v�Z����
var Result=function(){
  this.displacement=[];		// �ψ�
  this.dispMax=0;		// �ψʂ̑傫���̍ő�l
  this.angleMax=0;		// ��]�p�̑傫���̍ő�l
  this.strain1=[];		// �ߓ_�c
  this.stress1=[];		// �ߓ_����
  this.sEnergy1=[];		// �ߓ_�c�G�l���M�[���x
  this.strain2=[];
  this.stress2=[];
  this.sEnergy2=[];
  this.temperature=[];		// �ߓ_���x
  this.tempMax=0;		// ���x�̍ő�l
  this.eigenValue=[];		// �ŗL�l�f�[�^
  this.calculated=false;	// �v�Z�O���v�Z���ʖ���
  this.type=NODE_DATA;		// �f�[�^�ێ��`�ԁF�ߓ_�f�[�^
  this.value=[];		// �R���^�[�}�f�[�^
  this.minValue=0;		// �R���^�[�}�f�[�^�ŏ��l
  this.maxValue=0;		// �R���^�[�}�f�[�^�ő�l
};

// �v�Z���ʂ���������
Result.prototype.clear=function(){
  this.displacement.length=0;
  this.strain1.length=0;
  this.strain2.length=0;
  this.stress1.length=0;
  this.stress2.length=0;
  this.sEnergy1.length=0;
  this.sEnergy2.length=0;
  this.temperature.length=0;
  this.dispMax=0;
  this.angleMax=0;
  this.tempMax=0;
  this.eigenValue.length=0;
  this.calculated=false;
  this.value.length=0;
  this.minValue=0;
  this.maxValue=0;
};

// �ߓ_�ψʂ�ݒ肷��
// bc - ���E����
// disp - �ߓ_�ψʂ�\���x�N�g��
// nodeCount - �ߓ_��
Result.prototype.setDisplacement=function(bc,disp,nodeCount){
  this.displacement.length=0;
  this.dispMax=0;
  this.angleMax=0;
  var rests=bc.restraints,ii=0;
  for(var i=0;i<nodeCount;i++){
    var v=new Vector3R(),i0=bc.nodeIndex[i],bcDof=bc.dof[i],r=-1,x=v.x;
    for(var j=0;j<bcDof;j++){
      var bcl=bc.bcList[i0+j];
      if(bcl<0){
      	x[j]=disp[ii];
      	ii++;
      }
      else{
      	r=parseInt(bcl/6);
      	x[j]=rests[r].x[j];
      }
    }
    if((r>=0) && rests[r].coords){
      v.x=rests[r].coords.toGlobal(x);
    }
    this.dispMax=Math.max(this.dispMax,v.magnitude());
    this.angleMax=Math.max(this.angleMax,v.magnitudeR());
    this.displacement.push(v);
  }
  this.calculated=true;
};

// �ߓ_���x��ݒ肷��
// bc - ���E����
// t - �ߓ_���x��\���x�N�g��
// nodeCount - �ߓ_��
Result.prototype.setTemperature=function(bc,t,nodeCount){
  this.temperature.length=0;
  var temp=bc.temperature,ii=0;
  for(var i=0;i<nodeCount;i++){
    var tt;
    if(bc.bcList[i]<0){
      tt=t[ii];
      ii++;
    }
    else{
      tt=temp[bc.bcList[i]].t;
    }
    this.tempMax=Math.max(this.tempMax,tt);
    this.temperature.push(tt);
  }
  this.calculated=true;
};

// �ߓ_�̍\����͌��ʂɒl��������
// i - �ߓ_�̃C���f�b�N�X
// eps1,str1,se1,eps2,str2,se2 - �\�ʁE���ʂ̘c�C���́C�c�G�l���M�[���x
Result.prototype.addStructureData=function(i,eps1,str1,se1,
      	      	      	      	      	   eps2,str2,se2){
  this.strain1[i].add(eps1);
  this.stress1[i].add(str1);
  this.sEnergy1[i]+=se1;
  this.strain2[i].add(eps2);
  this.stress2[i].add(str2);
  this.sEnergy2[i]+=se2;
};

// �ߓ_�̍\����͌��ʂɒl���|����
// i - �ߓ_�̃C���f�b�N�X
// coef - �v�Z���ʂɊ|����W��
Result.prototype.mulStructureData=function(i,coef){
  this.strain1[i].mul(coef);
  this.stress1[i].mul(coef);
  this.sEnergy1[i]*=coef;
  this.strain2[i].mul(coef);
  this.stress2[i].mul(coef);
  this.sEnergy2[i]*=coef;
};

// �ŗL�l�f�[�^��ǉ�����
// ev - �ŗL�l
Result.prototype.addEigenValue=function(ev){
  this.eigenValue.push(ev);
  this.calculated=true;
};

// �R���^�[�}�f�[�^��ݒ肷��
// param - �f�[�^�̎��
// component - �f�[�^�̐���
// data - �R���^�[�}�Q�ƌ�
Result.prototype.setContour=function(param,component,data){
  if(param<0) return;
  data=data||this;
  var dpara=[data.displacement,data.strain1,data.stress1,data.sEnergy1,
      	     data.temperature];
  var count=dpara[param].length;
  if(count===0) return;
  this.value.length=0;
  this.value[0]=data.getData(param,component,0);
  this.minValue=this.value[0];
  this.maxValue=this.value[0];
  for(var i=1;i<count;i++){
    this.value[i]=data.getData(param,component,i);
    this.minValue=Math.min(this.minValue,this.value[i]);
    this.maxValue=Math.max(this.maxValue,this.value[i]);
  }
};

// �f�[�^�����o��
// param - �f�[�^�̎��
// component - �f�[�^�̐���
// index - �ߓ_�̃C���f�b�N�X
Result.prototype.getData=function(param,component,index){
  switch(param){
    case DISPLACEMENT:
      switch(component){
      	case X:
      	case Y:
      	case Z:
      	case RX:
      	case RY:
      	case RZ:
      	  return this.displacement[index].x[component];
      	case MAGNITUDE:
      	  return this.displacement[index].magnitude();
      }
      break;
    case STRAIN:
      if(component<SHIFT){
      	return this.getTensorComp(this.strain1[index],component);
      }
      else{
      	return this.getTensorComp(this.strain2[index],component-SHIFT);
      }
      break;
    case STRESS:
      if(component<SHIFT){
      	return this.getTensorComp(this.stress1[index],component);
      }
      else{
      	return this.getTensorComp(this.stress2[index],component-SHIFT);
      }
      break;
    case S_ENERGY:
      if(component===0){
      	return this.sEnergy1[index];
      }
      else{
      	return this.sEnergy2[index];
      }
      break;
    case TEMPERATURE:
      return this.temperature[index];
  }
  return 0;
};

// �c�E���͂����o��
// s - �c or ����
// component - �f�[�^�̐���
Result.prototype.getTensorComp=function(s,component){
  if(component<6){
    return s.vector()[component];
  }
  else if(component<=10){
    var pri=s.principal();
    if(component===MAX_PRINCIPAL)      return pri[0];
    else if(component===MIN_PRINCIPAL) return pri[2];
    else if(component===MID_PRINCIPAL) return pri[1];
    else if(component===MAX_SHARE)     return 0.5*(pri[0]-pri[2]);
  }
  else if(component===VON_MISES){
    return s.mises();
  }
  return 0;
};

// �ߓ_�c�E���͂�����������
// count - �ߓ_��
Result.prototype.initStrainAndStress=function(count){
  this.strain1.length=0;
  this.strain2.length=0;
  this.stress1.length=0;
  this.stress2.length=0;
  this.sEnergy1.length=0;
  this.sEnergy2.length=0;
  var zeros=[0,0,0,0,0,0];
  for(var i=0;i<count;i++){
    this.strain1[i]=new Strain(zeros);
    this.strain2[i]=new Strain(zeros);
    this.stress1[i]=new Stress(zeros);
    this.stress2[i]=new Stress(zeros);
    this.sEnergy1[i]=0;
    this.sEnergy2[i]=0;
  }
};

// �f�[�^�������Ԃ�
// nodes - �ߓ_
// elems - �v�f
Result.prototype.toStrings=function(nodes,elems){
  var s=[],tuple,i;
  if(this.type===ELEMENT_DATA){
    s.push('ResultType\tElement');
    tuple=elems;
  }
  else{
    s.push('ResultType\tNode');
    tuple=nodes;
  }
  for(i=0;i<this.displacement.length;i++){
    s.push('Displacement\t'+nodes[i].label.toString(10)+'\t'+
      	   this.displacement[i].x.join('\t'));
  }
  for(i=0;i<this.strain1.length;i++){
    s.push('Strain1\t'+tuple[i].label.toString(10)+'\t'+
      	   this.strain1[i].vector().join('\t'));
  }
  for(i=0;i<this.stress1.length;i++){
    s.push('Stress1\t'+tuple[i].label.toString(10)+'\t'+
      	   this.stress1[i].vector().join('\t'));
  }
  for(i=0;i<this.sEnergy1.length;i++){
    s.push('StrEnergy1\t'+tuple[i].label.toString(10)+'\t'+
      	   this.sEnergy1[i]);
  }
  for(i=0;i<this.strain2.length;i++){
    s.push('Strain2\t'+tuple[i].label.toString(10)+'\t'+
      	   this.strain2[i].vector().join('\t'));
  }
  for(i=0;i<this.stress2.length;i++){
    s.push('Stress2\t'+tuple[i].label.toString(10)+'\t'+
      	   this.stress2[i].vector().join('\t'));
  }
  for(i=0;i<this.sEnergy2.length;i++){
    s.push('StrEnergy2\t'+tuple[i].label.toString(10)+'\t'+
      	   this.sEnergy2[i]);
  }
  for(i=0;i<this.temperature.length;i++){
    s.push('Temp\t'+nodes[i].label.toString(10)+'\t'+
      	   this.temperature[i]);
  }
  for(i=0;i<this.eigenValue.length;i++){
    Array.prototype.push.apply
      (s,this.eigenValue[i].toStrings(nodes,tuple));
  }
  return s;
};

//--------------------------------------------------------------------//
// �ŗL�l
// value - �ŗL�l�E�ŗL�U����
// type - ��͎��
var EigenValue=function(value,type){
  this.value=value;
  this.type=type;
  this.displacement=[];		// �ψ�
  this.sEnergy1=[];		// �ߓ_�c�G�l���M�[���x
  this.sEnergy2=[];
  this.dispMax=0;
  this.angleMax=0;
};

// �ψʂ�ݒ肷��
// bc - ���E����
// disp - �ψʂ�\���ŗL�x�N�g��
// nodeCount - �ߓ_��
EigenValue.prototype.setDisplacement=function(bc,disp,nodeCount){
  this.displacement.length=0;
  this.dispMax=0;
  this.angleMax=0;
  var rests=bc.restraints,ii=0;
  for(var i=0;i<nodeCount;i++){
    var v=new Vector3R(),i0=bc.nodeIndex[i],bcDof=bc.dof[i],r=-1,x=v.x;
    for(var j=0;j<bcDof;j++){
      var bcl=bc.bcList[i0+j];
      if(bcl<0){
      	x[j]=disp[ii];
      	ii++;
      }
      else{
      	r=parseInt(bcl/6);
      }
    }
    if((r>=0) && rests[r].coords){
      v.x=rests[r].coords.toGlobal(x);
    }
    this.dispMax=Math.max(this.dispMax,v.magnitude());
    this.angleMax=Math.max(this.angleMax,v.magnitudeR());
    this.displacement.push(v);
  }
};

// �f�[�^�����o��
// param - �f�[�^�̎��
// component - �f�[�^�̐���
// index - �ߓ_�̃C���f�b�N�X
EigenValue.prototype.getData=function(param,component,index){
  switch(param){
    case DISPLACEMENT:
      switch(component){
      	case X:
      	case Y:
      	case Z:
      	case RX:
      	case RY:
      	case RZ:
      	  return this.displacement[index].x[component];
      	case MAGNITUDE:
      	  return this.displacement[index].magnitude();
      }
      break;
    case S_ENERGY:
      if(component===0){
      	return this.sEnergy1[index];
      }
      else{
      	return this.sEnergy2[index];
      }
      break;
  }
  return 0;
};

// �ߓ_�c�E���͂�����������
// count - �ߓ_��
EigenValue.prototype.initStrainEnergy=function(count){
  this.sEnergy1.length=0;
  this.sEnergy2.length=0;
  for(var i=0;i<count;i++){
    this.sEnergy1[i]=0;
    this.sEnergy2[i]=0;
  }
};

// �f�[�^�������Ԃ�
// nodes - �ߓ_
// tuple - �ߓ_or�v�f
EigenValue.prototype.toStrings=function(nodes,tuple){
  var s=[],i;
  s.push('EigenValue\t'+this.type+'\t'+this.value);
  for(i=0;i<this.displacement.length;i++){
    s.push('Displacement\t'+nodes[i].label.toString(10)+'\t'+
      	   this.displacement[i].x.join('\t'));
  }
  for(i=0;i<this.sEnergy1.length;i++){
    s.push('StrEnergy1\t'+tuple[i].label.toString(10)+'\t'+
      	   this.sEnergy1[i]);
  }
  for(i=0;i<this.sEnergy2.length;i++){
    s.push('StrEnergy2\t'+tuple[i].label.toString(10)+'\t'+
      	   this.sEnergy2[i]);
  }
  return s;
};

//--------------------------------------------------------------------//
// �R�����Ώ̃e���\��
// s - ����
var SymmetricTensor3=function(s){
  this.xx=s[0];
  this.yy=s[1];
  this.zz=s[2];
  this.xy=s[3];
  this.yz=s[4];
  this.zx=s[5];
};

// �e���\�����x�N�g���Ƃ��ĕԂ�
SymmetricTensor3.prototype.vector=function(){
  return [this.xx,this.yy,this.zz,this.xy,this.yz,this.zx];
};

// �e���\����������
// t - ������e���\��
SymmetricTensor3.prototype.add=function(t){
  this.xx+=t.xx;
  this.yy+=t.yy;
  this.zz+=t.zz;
  this.xy+=t.xy;
  this.yz+=t.yz;
  this.zx+=t.zx;
};

// �����ɃX�J���[���|����
// a - �|����X�J���[
SymmetricTensor3.prototype.mul=function(a){
  this.xx*=a;
  this.yy*=a;
  this.zz*=a;
  this.xy*=a;
  this.yz*=a;
  this.zx*=a;
};

// �ŗL�l��Ԃ�
SymmetricTensor3.prototype.principal=function(){
  return eigenvalue(this,100).lambda;
};

// �e���\������]������
// d - �����]���}�g���b�N�X
SymmetricTensor3.prototype.rotate=function(d){
  var mat=[[this.xx,this.xy,this.zx],[this.xy,this.yy,this.yz],
      	   [this.zx,this.yz,this.zz]];
  var s=[0,0,0,0,0,0];
  for(var i=0;i<3;i++){
    for(var j=0;j<3;j++){
      var mij=mat[i][j];
      for(var k=0;k<3;k++){
      	s[k]+=d[k][i]*d[k][j]*mij;
      	s[k+3]+=d[k][i]*d[(k+1)%3][j]*mij;
      }
    }
  }
  this.xx=s[0];
  this.yy=s[1];
  this.zz=s[2];
  this.xy=s[3];
  this.yz=s[4];
  this.zx=s[5];
};

// �e���\���̓��ς��v�Z����
// t - ����̃e���\��
SymmetricTensor3.prototype.innerProduct=function(t){
  return this.xx*t.xx+this.yy*t.yy+this.zz*t.zz+
      	 2*(this.xy*t.xy+this.yz*t.yz+this.zx*t.zx);
};

// Jacobie�@�őΏ̃e���\���̌ŗL�l�����߂�
// Numeric.js�ł͑Ίp�v�f��0�i�Ⴆ�΂���f�݂̂̏����j���Ƌ��߂��Ȃ�
// st - �Ώ̃e���\��
// iterMax - �����񐔂̍ő�l
function eigenvalue(st,iterMax){
  var m=[[st.xx,st.xy,st.zx],[st.xy,st.yy,st.yz],
      	 [st.zx,st.yz,st.zz]];
  return eigenByJacob(m,iterMax);
}

// Jacobie�@�őΏ̃e���\���̌ŗL�l�����߂�
// m - �Ώ̍s��
// iterMax - �����񐔂̍ő�l
function eigenByJacob(m,iterMax){
  var size=m.length,abs=Math.abs,i,j,iter,dataMax=0;
  var ev=numeric.identity(size);
  for(i=0;i<size;i++){
    for(j=i;j<size;j++){
      dataMax=Math.max(dataMax,abs(m[i][j]));
    }
  }
  var tolerance=EIG_EPS*dataMax;
// �l��0�̏ꍇ
  if(dataMax===0) return {lambda:numeric.getDiag(m),ev:ev};
  for(iter=0;iter<iterMax;iter++){
    var im=0,jm=0,ndMax=0;
    for(i=0;i<2;i++){
      for(j=i+1;j<3;j++){
      	var absm=abs(m[i][j]);
      	if(absm>ndMax){
      	  ndMax=absm;
      	  im=i;
      	  jm=j;
      	}
      }
    }
    if(ndMax<tolerance) break;
    var mim=m[im],mjm=m[jm];
    var alpha=0.5*(mim[im]-mjm[jm]);
    var beta=0.5/Math.sqrt(alpha*alpha+ndMax*ndMax);
    var cc2=0.5+abs(alpha)*beta,cs=-beta*mim[jm];
    if(alpha<0) cs=-cs;
    var cc=Math.sqrt(cc2),ss=cs/cc;
    var aij=2*(alpha*cc2-mim[jm]*cs),aii=mjm[jm]+aij,ajj=mim[im]-aij;
    for(i=0;i<3;i++){
      var mi=m[i],evi=ev[i];
      var a1=mi[im]*cc-mi[jm]*ss;
      var a2=mi[im]*ss+mi[jm]*cc;
      mi[im]=a1;
      mi[jm]=a2;
      mim[i]=a1;
      mjm[i]=a2;
      a1=evi[im]*cc-evi[jm]*ss;
      a2=evi[im]*ss+evi[jm]*cc;
      evi[im]=a1;
      evi[jm]=a2;
    }
    mim[im]=aii;
    mim[jm]=0;
    mjm[im]=0;
    mjm[jm]=ajj;
  }
  m=numeric.getDiag(m);
// �ŗL�l��傫�����ɓ���ւ���
  var eig=[];
  ev=numeric.transpose(ev);
  for(i=0;i<size;i++) eig.push([m[i],ev[i]]);
  eig.sort(function(v1,v2){return v2[0]-v1[0];});
  for(i=0;i<size;i++){
    m[i]=eig[i][0];
    ev[i]=eig[i][1];
  }
  return {lambda:m,ev:numeric.transpose(ev)};
}

//--------------------------------------------------------------------//
// �c
// s - ����
var Strain=function(s){
  SymmetricTensor3.call(this,s);
  this.xy=0.5*s[3];
  this.yz=0.5*s[4];
  this.zx=0.5*s[5];
};

// �e���\�����x�N�g���Ƃ��ĕԂ�
Strain.prototype.vector=function(){
  return [this.xx,this.yy,this.zz,2*this.xy,2*this.yz,2*this.zx];
};

//--------------------------------------------------------------------//
// ����
// s - ����
var Stress=function(s){
  SymmetricTensor3.call(this,s);
};

// �~�[�[�X���͂�Ԃ�
Stress.prototype.mises=function(){
  var dxy=this.xx-this.yy,dyz=this.yy-this.zz,dzx=this.zz-this.xx;
  var ss=dxy*dxy+dyz*dyz+dzx*dzx;
  var tt=this.xy*this.xy+this.yz*this.yz+this.zx*this.zx;
  return Math.sqrt(0.5*ss+3*tt);
};

//--------------------------------------------------------------------//
// ���ʕ\���ݒ�
var ResultView=function(){
  this.dispCoef=document.getElementById('dispcoef');	// �ό`�\���{��
  this.eigen=document.getElementById('eigenvalue');	// �ŗL�l�f�[�^
  this.contour=document.getElementById('contour');	// �R���^�[�}�\���f�[�^
  this.component=document.getElementById('component');	// �R���^�[�}�\������
};

// �É�͂̐ݒ������������
ResultView.prototype.setInitStatic=function(){
  removeOptions(this.eigen);
  this.setContourSelect();
  this.setConfig();
};

// �ŗL�l��͂̐ݒ������������
ResultView.prototype.setInitEigen=function(){
  removeOptions(this.eigen);
  var eigenValue=model.result.eigenValue;
  for(var i=0;i<eigenValue.length;i++){
    this.eigen.appendChild(createOption('�ŗL�l'+(i+1),i));
  }
  removeOptions(this.contour);
  this.contour.appendChild(createOption('�R���^�[����',NONE));
  this.contour.appendChild(createOption('�ψ�',DISPLACEMENT));
  this.contour.appendChild(createOption('�c�G�l���M�[���x',S_ENERGY));
  this.setResComp();
  this.setConfig();
};

// �\������R���^�[�}�f�[�^��ݒ肷��
ResultView.prototype.setContourSelect=function(){
  removeOptions(this.eigen);
  removeOptions(this.contour);
  this.contour.appendChild(createOption('�R���^�[����',NONE));
  if(model.result.displacement.length>0){
    this.contour.appendChild(createOption('�ψ�',DISPLACEMENT));
  }
  if(model.result.strain1.length>0){
    this.contour.appendChild(createOption('�c',STRAIN));
  }
  if(model.result.stress1.length>0){
    this.contour.appendChild(createOption('����',STRESS));
  }
  if(model.result.sEnergy1.length>0){
    this.contour.appendChild(createOption('�c�G�l���M�[���x',S_ENERGY));
  }
  if(model.result.temperature.length>0){
    this.contour.appendChild(createOption('���x',TEMPERATURE));
  }
  removeOptions(this.component);
};

// �\��������ݒ肷��
ResultView.prototype.setResComp=function(){
  if(!model.result.calculated) return;
  removeOptions(this.component);
  switch(parseInt(this.contour.value)){
    case DISPLACEMENT:
      if(model.hasShellBar){
      	setOptions(this.component,DISP2_COMPONENT,-1);
      }
      else{
      	setOptions(this.component,DISP_COMPONENT,-1);
      }
      break;
    case STRAIN:
      if(model.hasShellBar){
      	setOptions(this.component,STRAIN_COMPONENT,1);
      	setOptions(this.component,STRAIN_COMPONENT,2);
      }
      else{
      	setOptions(this.component,STRAIN_COMPONENT,-1);
      }
      break;
    case STRESS:
      if(model.hasShellBar){
      	setOptions(this.component,STRESS_COMPONENT,1);
      	setOptions(this.component,STRESS_COMPONENT,2);
      }
      else{
      	setOptions(this.component,STRESS_COMPONENT,-1);
      }
      break;
    case S_ENERGY:
      if(model.hasShellBar){
      	setOptions(this.component,ENERGY_COMPONENT,1);
      	setOptions(this.component,ENERGY_COMPONENT,2);
      }
      else{
      	setOptions(this.component,ENERGY_COMPONENT,-1);
      }
      break;
  }
};

// �ݒ��\���ɔ��f������
ResultView.prototype.setConfig=function(){
  var eigen=parseInt(this.eigen.value);
  var dcoef=parseFloat(this.dispCoef.value);
  var param=parseInt(this.contour.value);
  var coef,comp,minValue,maxValue;
  if(isFinite(eigen)){
    var eigenValue=model.result.eigenValue[eigen];
    coef=dcoef*Math.min(bounds.size/eigenValue.dispMax,
      	      	      	1/eigenValue.angleMax);
    viewObj.setDisplacement(eigenValue.displacement,coef);
    showEigenValue(eigen,eigenValue.type,eigenValue.value);
    if(param<0){
      viewObj.clearContour();
      colorBar.clear();
    }
    else{
      comp=parseInt(this.component.value);
      model.result.setContour(param,comp,eigenValue);
      minValue=model.result.minValue;
      maxValue=model.result.maxValue;
      switch(param){
      	case DISPLACEMENT:
      	case TEMPERATURE:
      	  viewObj.setContour(model.result.value,minValue,maxValue);
      	  break;
      	default:
      	  viewObj.setContour(model.result.value,minValue,maxValue,
      	      	      	     model.result.type);
      	  break;
      }
      colorBar.draw(minValue,maxValue);
    }
  }
  else{
    coef=dcoef*Math.min(bounds.size/model.result.dispMax,
      	      	      	1/model.result.angleMax);
    viewObj.setDisplacement(model.result.displacement,coef);
    if(param<0){
      viewObj.clearContour();
      colorBar.clear();
    }
    else{
      comp=parseInt(this.component.value);
      model.result.setContour(param,comp);
      minValue=model.result.minValue;
      maxValue=model.result.maxValue;
      switch(param){
      	case DISPLACEMENT:
      	case TEMPERATURE:
      	  viewObj.setContour(model.result.value,minValue,maxValue);
      	  break;
      	default:
      	  viewObj.setContour(model.result.value,minValue,maxValue,
      	      	      	     model.result.type);
      	  break;
      }
      colorBar.draw(minValue,maxValue);
    }
  }
};

// �ݒ���o�b�N�A�b�v����
ResultView.prototype.stock=function(){
  this.coef0=this.dispCoef.value;
  this.contour0=[];
  this.comp0=[];
  var i;
  for(i=0;i<this.contour.childNodes.length;i++){
    this.contour0[i]=this.contour.childNodes[i];
  }
  this.contIndex=this.contour.selectedIndex;
  for(i=0;i<this.component.childNodes.length;i++){
    this.comp0[i]=this.component.childNodes[i];
  }
  this.compIndex=this.component.selectedIndex;
};

// �ݒ�����ɖ߂�
ResultView.prototype.reset=function(){
  this.dispCoef.value=this.coef0;
  removeOptions(this.contour);
  removeOptions(this.component);
  var i;
  for(i=0;i<this.contour0.length;i++){
    this.contour.appendChild(this.contour0[i]);
  }
  this.contour.selectedIndex=this.contIndex;
  for(i=0;i<this.comp0.length;i++){
    this.component.appendChild(this.comp0[i]);
  }
  this.component.selectedIndex=this.compIndex;
};

// �v�Z���ʂ̐�����\������
// sel - �R���{�{�b�N�X
// component - ����
// data - �f�[�^�ԍ��i1:�\��,2:����,-1:�ԍ��Ȃ��j
function setOptions(sel,component,data){
  for(var i=0;i<component.length;i++){
    var c=component[i];
    if(data>0) c+=' '+data;
    sel.appendChild(createOption(c,COMP_MAP[c]));
  }
}

// �I�v�V�����v�f���쐬����
// text - �I�v�V�����̃e�L�X�g
// value - �I�v�V�����̒l
function createOption(text,value){
  var opt=document.createElement('option');
  opt.value=value;
  opt.text=text;
  return opt;
}

// �R���{�{�b�N�X�̃I�v�V�������폜����
// sel - �R���{�{�b�N�X
function removeOptions(sel){
  if(sel.hasChildNodes()){
    while(sel.childNodes.length>0){
      sel.removeChild(sel.firstChild);
    }
  }
}

// ���ʕ\���ݒ�E�B���h�E��\������
function showResultWindow(){
  showModalWindow(RESULT_WINDOW);
  resultView.stock();
}

// �v�Z���ʂ���������
function removeRes(){
  model.result.clear();
  viewObj.removeResult();
  colorBar.clear();
  showInfo();
  resultView.setContourSelect();
  hideModalWindow(RESULT_WINDOW);
}

// ���ʕ\���ݒ���X�V����
function setResultConfig(){
  hideModalWindow(RESULT_WINDOW);
  resultView.setConfig();
}

// ���ʕ\���ݒ��������
function cancelResultConfig(){
  hideModalWindow(RESULT_WINDOW);
  resultView.reset();
}

inherits(Strain,SymmetricTensor3);
inherits(Stress,SymmetricTensor3);

var CPICK_FONT_SIZE=14;				// �t�H���g�T�C�Y
var CPICK_FONT=CPICK_FONT_SIZE+"px 'Arial'";	// �t�H���g

//--------------------------------------------------------------------//
// �J���[�s�b�J�[
// canvas - �`��L�����o�X
// div - �~�`�`�敪����
// x,y,width,height - �`��ʒu,��,����
var ColorPicker=function(canvas,div,x,y,width,height){
  this.canvas=canvas;
  this.context=canvas.getContext('2d');	// �R���e�L�X�g
  this.context.font=CPICK_FONT;
  this.context.textBaseline='top';
  this.div=div||360;
  x=x||0;
  y=y||0;
  width=width||canvas.width;
  height=height||canvas.height;
// ���S���W
  var size=Math.min(width,height);
  this.cx=parseInt(x+0.5*size);
  this.cy=parseInt(y+0.5*size);
// �F���~�`�X���C�_�[���a
  this.ro=parseInt(0.48*size);
  this.rc=parseInt(0.9*this.ro);
  this.ri=parseInt(0.8*this.ro);
// �ʓx�P�x�}�b�v�`��ʒu,�T�C�Y
  this.rect=parseInt(1.3*this.ri);
  this.rx=parseInt(this.cx-0.5*this.rect);
  this.ry=parseInt(this.cy-0.5*this.rect);
  this.pt=parseInt(Math.max(0.07*this.ri,1));
  this.hsl=[0,0,0];			// ���݂̐F (HSL�F���)
// �e�L�X�g�ʒu
  if(width>=height){
    this.tx=size+10;
    this.ty=10;
  }
  else{
    this.tx=10;
    this.ty=size+10;
  }
  this.initMouseEvent();
};

// ���݂̐F��ݒ肷��
// rgb - �w�肷��F (RGB)
ColorPicker.prototype.setColor=function(rgb){
  var r=rgb[0],g=rgb[1],b=rgb[2];
  var rgbmax=Math.max(Math.max(r,g),b);
  var rgbmin=Math.min(Math.min(r,g),b);
  this.hsl[2]=0.5*(rgbmax+rgbmin);
  if(rgbmax===rgbmin){
    this.hsl[0]=0;
    this.hsl[1]=0;
  }
  else{
    var del=rgbmax-rgbmin;
    this.hsl[1]=del/(1-Math.abs(2*this.hsl[2]-1));
    if(rgbmin===b)      this.hsl[0]=((g-r)/del+1)/6;
    else if(rgbmin===r) this.hsl[0]=((b-g)/del+3)/6;
    else if(rgbmin===g) this.hsl[0]=((r-b)/del+5)/6;
  }
  this.draw();
};

// ���݂̐F (RGB) ��Ԃ�
ColorPicker.prototype.getColor=function(){
  var h=this.hsl[0],s=this.hsl[1],l=this.hsl[2];
  var rgbmax=l+0.5*s*(1-Math.abs(2*l-1));
  var rgbmin=2*l-rgbmax;
  return [hueToRGB(rgbmax,rgbmin,h+1/3),hueToRGB(rgbmax,rgbmin,h),
      	  hueToRGB(rgbmax,rgbmin,h-1/3)];
};

// �J���[�s�b�J�[��`�悷��
ColorPicker.prototype.draw=function(){
  this.context.clearRect(0,0,this.canvas.width,this.canvas.height);
// �F���X���C�_�[
  var dth=2*Math.PI/this.div,dh=360/this.div;
  var cc0=1,ss0=0,th,h,s,l,i;
  for(i=0;i<this.div;i++){
    th=dth*(i+1);
    var cc1=Math.cos(th),ss1=Math.sin(th);
    h=dh*i;
    this.context.fillStyle='hsl('+h+',100%,50%)';
    this.context.strokeStyle=this.context.fillStyle;
    this.context.beginPath();
    this.context.moveTo(this.cx+this.ri*cc0,this.cy-this.ri*ss0);
    this.context.lineTo(this.cx+this.ro*cc0,this.cy-this.ro*ss0);
    this.context.lineTo(this.cx+this.ro*cc1,this.cy-this.ro*ss1);
    this.context.lineTo(this.cx+this.ri*cc1,this.cy-this.ri*ss1);
    this.context.closePath();
    this.context.fill();
    this.context.stroke();
    cc0=cc1;
    ss0=ss1;
  }
  th=2*Math.PI*this.hsl[0];
  cc0=Math.cos(th);
  ss0=Math.sin(th);
  this.drawPointer(this.cx+this.rc*cc0,this.cy-this.rc*ss0);

// �ʓx�P�x�}�b�v
  h=360*this.hsl[0];
  var dl=100/this.rect;
  for(i=0;i<this.rect;i++){
    var y=this.ry+i;
    l=dl*(this.rect-i);
    var grad=this.context.createLinearGradient
      (this.rx,y,this.rx+this.rect,y);
    grad.addColorStop(0,'hsl('+h+',0%,'+l+'%)');
    grad.addColorStop(1,'hsl('+h+',100%,'+l+'%)');
    this.context.fillStyle=grad;
    this.context.fillRect(this.rx,y,this.rect,1);
  }
  this.drawPointer(parseInt(this.rx+this.rect*this.hsl[1]),
      	      	   parseInt(this.ry+this.rect*(1-this.hsl[2])));

// �e�L�X�g
  this.context.fillStyle='#000000';
  this.context.fillText('H:'+parseInt(h),this.tx,this.ty);
  s=parseInt(100*this.hsl[1]);
  l=parseInt(100*this.hsl[2]);
  if(this.tx<=this.ty){
    var w=parseInt(this.canvas.width/3);
    this.context.fillText('S:'+s+'%',this.tx+w,this.ty);
    this.context.fillText('L:'+l+'%',this.tx+2*w,this.ty);
  }
  else{
    this.context.fillText('S:'+s+'%',this.tx,this.ty+CPICK_FONT_SIZE);
    this.context.fillText('L:'+l+'%',this.tx,this.ty+2*CPICK_FONT_SIZE);
  }
};

// �|�C���^��`�悷��
// x,y - �`��ʒu
ColorPicker.prototype.drawPointer=function(x,y){
  var h=360*this.hsl[0],s=100*this.hsl[1],l=100*this.hsl[2];
  this.context.fillStyle='hsl('+h+','+s+'%,'+l+'%)';
  this.context.beginPath();
  this.context.arc(x,y,this.pt,0,2*Math.PI,false);
  this.context.closePath();
  this.context.fill();
  this.context.strokeStyle='#ffffff';
  this.context.beginPath();
  this.context.arc(x,y,this.pt,0,2*Math.PI,false);
  this.context.closePath();
  this.context.stroke();
  this.context.strokeStyle='#000000';
  this.context.beginPath();
  this.context.arc(x,y,this.pt+1,0,2*Math.PI,false);
  this.context.closePath();
  this.context.stroke();
};

// �}�E�X�C�x���g������������
ColorPicker.prototype.initMouseEvent=function(){
  var mx,my,pressedArea=-1,cp=this;
  this.canvas.addEventListener('mousedown',cpMousePressed,false);

// �}�E�X�{�^�������������̏������s��
  function cpMousePressed(e){
    e.preventDefault();
    e.stopPropagation();
    document.addEventListener('mouseup',cpMouseReleased,false);
    document.addEventListener('mousemove',cpMouseMoved,false);
    var bcr=e.target.getBoundingClientRect();
    mx=e.clientX-bcr.left;
    my=e.clientY-bcr.top;
    if((cp.rx-cp.pt<=mx) && (mx<=cp.rx+cp.rect+cp.pt) &&
       (cp.ry-cp.pt<=my) && (my<=cp.ry+cp.rect+cp.pt)){
      pressedArea=1;
      var sl=satLight();
      cp.hsl[1]=sl[0];
      cp.hsl[2]=sl[1];
    }
    else{
      pressedArea=0;
      cp.hsl[0]=hue();
    }
    cp.draw();
  }

// �}�E�X�{�^���𗣂������̏������s��
  function cpMouseReleased(e){
    e.preventDefault();
    e.stopPropagation();
    document.removeEventListener('mouseup',cpMouseReleased,false);
    document.removeEventListener('mousemove',cpMouseMoved,false);
    pressedArea=-1;
  }

// �}�E�X�{�^�����ړ����������̏������s��
  function cpMouseMoved(e){
    var bcr=e.target.getBoundingClientRect();
    switch(pressedArea){
      case 0:
      	mx=e.clientX-bcr.left;
      	my=e.clientY-bcr.top;
      	cp.hsl[0]=hue();
      	cp.draw();
      	break;
      case 1:
      	mx=e.clientX-bcr.left;
      	my=e.clientY-bcr.top;
      	var sl=satLight();
      	cp.hsl[1]=sl[0];
      	cp.hsl[2]=sl[1];
      	cp.draw();
      	break;
    }
  }

// �F����Ԃ�
  function hue(){
    var h=Math.atan2(cp.cy-my,mx-cp.cx)/(2*Math.PI);
    if(h<0) h+=1;
    return h;
  }

// �ʓx�P�x��Ԃ�
  function satLight(){
    var s=(mx-cp.rx)/cp.rect;
    var l=1-(my-cp.ry)/cp.rect;
    return [Math.min(Math.max(s,0),1),Math.min(Math.max(l,0),1)];
  }
};

// RGB�J���[��Ԃ�
// rgbmax,rgbmin - R,G,B�̍ő�l,�ŏ��l
// h - �F��
function hueToRGB(rgbmax,rgbmin,h){
  if(h<0)      h+=1;
  else if(h>1) h-=1;
  h*=6;
  if(h<1)       return rgbmin+(rgbmax-rgbmin)*h;
  else if(h<=3) return rgbmax;
  else if(h<4)  return rgbmin+(rgbmax-rgbmin)*(4-h);
  else          return rgbmin;
}

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
