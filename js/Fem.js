//--------------------------------------------------------------------//
// 3次元有限要素法(FEM)

var model;		// FEMデータモデル
var viewModel;		// 表示モデル
var viewObj;		// 表示オブジェクト
var bounds;		// モデル境界
var info;		// モデル情報表示部
var colorBar;		// カラーバー
var resultView;		// 結果表示設定
var viewConfig;		// モデル表示設定
var modalWindow;	// モーダルウィンドウ

var FILE_WINDOW=0;	// ファイル操作ウィンドウ
var CALC_WINDOW=1;	// 計算設定ウィンドウ
var RESULT_WINDOW=2;	// 結果表示設定ウィンドウ
var CONFIG_WINDOW=3;	// コンフィグウィンドウ

// データを初期化する
// fileName - データファイル名
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

// three.js の初期化をする
function initThree(){
  document.addEventListener('keydown',keyPressed,false);
  bounds=new Bounds();
  viewModel=new ViewModel('fem-canvas');
  viewObj=new ViewObject();
  info=document.getElementById('info-canvas');
  colorBar=new ColorBar('colorbar');
}

// キーを押した時の処理を行う
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

// 表示オブジェクトを初期化する
function initObject(){
  viewObj.remove();
  viewObj.create();
  viewModel.updateLightAndCamera();
  colorBar.clear();
  resultView.setContourSelect();
  showInfo();
}

// ループ関数
function loop(){
  viewModel.update();
  requestAnimationFrame(loop);
  viewModel.setAxis();
}

// モデル情報を表示する
function showInfo(){
  if(model.result.calculated){
    if((model.result.dispMax===0) && (model.result.tempMax!==0)){
      info.textContent='温度 Max.:'+numString(model.result.tempMax);
    }
    else{
      info.textContent='変位 Max.:'+numString(model.result.dispMax);
    }
  }
  else{
    info.innerHTML='節点:'+model.mesh.nodes.length+
      	      	   '<br />要素:'+model.mesh.elements.length;
  }
}

// 固有値を表示する
// index - 固有値のインデックス
// type - 解析種類
// value - 固有値
function showEigenValue(index,type,value){
  if(type===BUCKLING){
    info.textContent='固有値'+(index+1)+': '+numString(value);
  }
  else{
    info.textContent='固有振動数'+(index+1)+': '+numString(value);
  }
}

// 数値を表す文字列を返す
// value - 数値
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

// モーダルウィンドウを表示する
// win - モーダルウィンドウのインデックス
function showModalWindow(win){
  modalWindow[win].style.zIndex=4;
  modalWindow[win].style.opacity=1;
}

// モーダルウィンドウを非表示にする
// win - モーダルウィンドウのインデックス
function hideModalWindow(win){
  modalWindow[win].style.zIndex=1;
  modalWindow[win].style.opacity=0;
}

// 継承関係を設定する
// ctor - 新クラス
// superCtor - 継承元クラス
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
// ファイル読み込み・書き込み

// サーバー上のFEMデータファイルを読み込む
// fileName - データファイル名
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

// ローカルファイルを読み込む
function readLocalFile() {
  var file=document.getElementById('localreadfile').files[0];
  if((file===null) || (file===undefined)){
    alert('ファイルが指定されていません');
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

// ローカルファイルを書き込む
function writeLocalFile() {
  var file=document.getElementById('localwritefile').value.trim();
  if(file===''){
    alert('ファイル名が空白です');
    return;
  }
  var s=model.toStrings().join('\n')+'\n';
  var blob=new Blob([s],{type:'text/plain'});
  if(window.navigator.msSaveBlob){		// IE専用
    window.navigator.msSaveBlob(blob,file);
  }
  else{						// その他のブラウザ
    var a=document.createElement('a');
    a.href=URL.createObjectURL(blob);
    a.target='_blank';
    a.download=file;
    a.click();
  }
  hideModalWindow(FILE_WINDOW);
}

// FEMデータを読み込む
// s - データ文字列のリスト
function readFemModel(s){
  model.clear();
  var mesh=model.mesh,bc=model.bc,res=[];
  for(var i=0;i<s.length;i++){
    var ss=s[i].trim().replace(/\t/g,' ').split(/\s+/);
    if(ss.length>0){
      var keyWord=ss[0].toLowerCase();
// 材料データ
      if((keyWord=='material') && (ss.length>7)){
      	model.materials.push
      	  (new Material(parseInt(ss[1]),parseFloat(ss[2]),
      	      	      	parseFloat(ss[3]),parseFloat(ss[5]),
      	      	      	parseFloat(ss[6]),parseFloat(ss[7])));
      }
// シェルパラメータ
      else if((keyWord=='shellparameter') && (ss.length>2)){
      	model.shellParams.push
      	  (new ShellParameter(parseInt(ss[1]),parseFloat(ss[2])));
      }
// 梁パラメータ
      else if((keyWord=='barparameter') && (ss.length>4)){
      	model.barParams.push(new BarParameter
      	  (parseInt(ss[1]),ss[2],ss.slice(3,ss.length)));
      }
// 局所座標系
      else if((keyWord=='coordinates') && (ss.length>10)){
      	model.coordinates.push(readCoordinates(ss));
      }
// 節点
      else if((keyWord=='node') && (ss.length>4)){
      	mesh.nodes.push(new FENode(parseInt(ss[1]),parseFloat(ss[2]),
      	      	      	      	   parseFloat(ss[3]),
      	      	      	      	   parseFloat(ss[4])));
      }
// 要素
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
// 境界条件
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
// 計算結果
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

// FEMの計算結果を読み込む
// s - データ文字列のリスト
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

// データポインタを獲得する
// ss - データ文字列
// map - ラベルマップ
function readDataPointer(ss,map){
  var p=parseInt(ss[1]);
  if(p in map){
    return map[p];
  }
  else{
    throw new Error('計算結果'+ss[0]+'の番号'+p+
      	      	    'は存在しません');
  }
}

// 節点番号を読み取る
// ss - 文字列配列
// is - 開始インデックス
// count - 節点数
function readVertex(ss,is,count){
  var vertex=[];
  for(var j=0;j<count;j++) vertex[j]=parseInt(ss[is+j]);
  return vertex;
}

// 局所座標系を読み込む
// ss - データ文字列配列
function readCoordinates(ss){
  var c=[[parseFloat(ss[2]),parseFloat(ss[3]),parseFloat(ss[4])],
      	 [parseFloat(ss[5]),parseFloat(ss[6]),parseFloat(ss[7])],
      	 [parseFloat(ss[8]),parseFloat(ss[9]),parseFloat(ss[10])]];
  for(var i=0;i<3;i++){
    var ci=c[i];
    var cf=ci[0]*ci[0]+ci[1]*ci[1]+ci[2]*ci[2];
    if(cf===0){
      throw new Error('座標系'+ss[2]+'の軸方向ベクトルが0です');
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

// 拘束条件を読み込む
// ss - データ文字列配列
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

// 荷重条件を読み込む
// ss - データ文字列配列
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

// ファイル操作ウィンドウを表示する
function showFileWindow(){
  showModalWindow(FILE_WINDOW);
}

// ファイル操作を取り消す
function cancelFile(){
  hideModalWindow(FILE_WINDOW);
// ファイル選択を消去する
  var localfile=document.getElementById('localreadfile');
  localfile.parentNode.innerHTML=localfile.parentNode.innerHTML;
}

var COEF_F_W=0.5/Math.PI;	// f/ω比 1/2π

//--------------------------------------------------------------------//
// FEM データモデル
var FemDataModel=function(){
  this.materials=[];			// 材料
  this.shellParams=[];			// シェルパラメータ
  this.barParams=[];			// 梁パラメータ
  this.coordinates=[];			// 局所座標系
  this.mesh=new MeshModel();		// メッシュモデル
  this.bc=new BoundaryCondition();	// 境界条件
  this.solver=new Solver();		// 連立方程式求解オブジェクト
  this.result=new Result();		// 計算結果
  this.hasShellBar=false;		// シェル要素または梁要素を含まない
};

// データを消去する
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

// モデルを初期化する
FemDataModel.prototype.init=function(){
  this.solver.method=ILUCG_METHOD;	// デフォルトは反復解法
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

// 節点・要素ポインタを設定する
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

// 材料ポインタを設定する
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
      throw new Error('材料番号'+elements[i].material+
      	      	      'は存在しません');
    }
  }
};

// シェルパラメータ・梁パラメータのポインタを設定する
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
      	throw new Error('パラメータ番号'+elements[i].param+
      	      	      	'は存在しません');
      }
    }
    else if(elements[i].isBar){
      if(elements[i].param in map2){
      	elements[i].param=map2[elements[i].param];
      	shellbars++;
      }
      else{
      	throw new Error('パラメータ番号'+elements[i].param+
      	      	      	'は存在しません');
      }
    }
  }
  this.hasShellBar=(shellbars>0);
  if(this.hasShellBar){		// シェル要素・梁要素を含む場合は直接解法
    this.solver.method=LU_METHOD;
  }
};

// 局所座標系を設定する
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

// 節点の自由度を設定する
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
    if(elem.isShell || elem.isBar){	// シェル要素・梁要素
      var count=elem.nodeCount();
      for(var j=0;j<count;j++){
      	dof[elem.nodes[j]]=6;
      }
    }
  }
  this.solver.dof=this.bc.setPointerStructure(nodeCount);
};

// 静解析をする
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
    alert('拘束条件不足のため計算できません');
  }
  var t1=new Date().getTime();
  console.log('Calculation time:'+(t1-t0)+'ms');
};

// 固有振動数・固有ベクトルを求める
// count - 求める固有振動の数
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

// 線形座屈解析をする
// count - 求める固有値の数
FemDataModel.prototype.calcBuckling=function(count){
  var t0=new Date().getTime();
  if(this.bc.restraints.length===0){
    throw new Error('拘束条件がありません');
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

// 節点歪・応力・歪エネルギー密度を計算する
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

// 要素歪・応力・歪エネルギー密度を計算する
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

// 固有値データの節点歪エネルギー密度を計算する
// ev - 固有値データ
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

// 固有値データの要素歪エネルギー密度を計算する
// ev - 固有値データ
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

// データ文字列を返す
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

// 節点集合の節点ラベルを再設定する
// map - ラベルマップ
// s - 節点集合
function resetNodes(map,s){
  for(var i=0;i<s.nodes.length;i++){
    if(s.nodes[i] in map){
      s.nodes[i]=map[s.nodes[i]];
    }
    else{
      throw new Error('節点番号'+s.nodes[i]+'は存在しません');
    }
  }
}

// 節点ポインタを再設定する
// map - ラベルマップ
// bc - 境界条件
function resetNodePointer(map,bc){
  if(bc.node in map){
    bc.node=map[bc.node];
  }
  else{
    throw new Error('節点番号'+bc.node+'は存在しません');
  }
}

// 要素ポインタを再設定する
// map - ラベルマップ
// bc - 境界条件
function resetElementPointer(map,bc){
  if(bc.element in map){
    bc.element=map[bc.element];
  }
  else{
    throw new Error('要素番号'+bc.element+'は存在しません');
  }
}

// 局所座標系を再設定する
// map - ラベルマップ
// bc - 境界条件
function resetCoordinatesPointer(map,bc){
  var coords=bc.coords;
  if((coords===null) || (coords===undefined)){
  }
  else if(coords in map){
    bc.coords=map[coords];
    bc.globalX=bc.coords.toGlobal(bc.x);
  }
  else{
    throw new Error('局所座標系番号'+coords+'は存在しません');
  }
}

//--------------------------------------------------------------------//
// メッシュモデル
var MeshModel=function(){
  this.nodes=[];		// 節点
  this.elements=[];		// 要素
  this.freeFaces=[];		// 表面
  this.faceEdges=[];		// 表面の要素辺
};

// 節点を返す
// s - 節点集合
MeshModel.prototype.getNodes=function(s){
  var p=[];
  for(var i=0;i<s.nodes.length;i++){
    p[i]=this.nodes[s.nodes[i]];
  }
  return p;
};

// データを消去する
MeshModel.prototype.clear=function(){
  this.nodes.length=0;
  this.elements.length=0;
  this.freeFaces.length=0;
  this.faceEdges.length=0;
};

// モデルを初期化する
MeshModel.prototype.init=function(){
  this.nodes.sort(compareLabel);
  bounds.set();
};

// 要素の鏡像向きを揃える
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

// 表面を取り出す
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

// 表面の要素辺を取り出す
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

// 形状データを取り出す
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

// 要素辺の形状データを取り出す
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

// 梁要素の形状データを取り出す
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
// 節点
// label - 節点ラベル
// x,y,z - x,y,z座標
var FENode=function(label,x,y,z){
  THREE.Vector3.call(this,x,y,z);
  this.label=label;
};

// 節点のコピーを返す
FENode.prototype.clone=function(){
  return new this.constructor(this.label,this.x,this.y,this.z);
};

// 節点を表す文字列を返す
FENode.prototype.toString=function(){
  return 'Node\t'+this.label.toString(10)+'\t'+
      	 this.x+'\t'+this.y+'\t'+this.z;
};

//--------------------------------------------------------------------//
// 節点集合
// nodes - 節点番号
var Nodes=function(nodes){
  this.nodes=nodes;
};

// 節点数を返す
Nodes.prototype.nodeCount=function(){
  return this.nodes.length;
};

// 重心位置を返す
// p - 頂点座標
function center(p){
  var x=0,y=0,z=0,cc=1.0/p.length;
  for(var i=0;i<p.length;i++){
    x+=p[i].x;
    y+=p[i].y;
    z+=p[i].z;
  }
  return new THREE.Vector3(cc*x,cc*y,cc*z);
}

// 法線ベクトルを返す
// p - 頂点座標
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

// ラベルを比較する
// o1,o2 - 比較する対象
function compareLabel(o1,o2){
  if(o1.label<o2.label)      return -1;
  else if(o1.label>o2.label) return 1;
  else                       return 0;
}

// 行列の和を計算する
// a - 基準行列
// da - 加える行列
function addMatrix(a,da){
  for(var i=0;i<a.length;i++){
    for(var j=0;j<a[i].length;j++){
      a[i][j]+=da[i][j];
    }
  }
}

// ベクトルの和を計算する
// v - 基準ベクトル
// dv - 加えるベクトル
function addVector(v,dv){
  for(var i=0;i<v.length;i++){
    v[i]+=dv[i];
  }
}

inherits(FENode,THREE.Vector3);

//--------------------------------------------------------------------//
// 表示モデル
// canvasId - 表示領域ID
var ViewModel=function(canvasId){
  this.canvasFrame=document.getElementById(canvasId);		// 描画フレーム
  this.renderer=new THREE.WebGLRenderer({antialias:true});	// レンダラ―
  if(!this.renderer){
    alert("three.js の初期化に失敗しました");
  }

  this.renderer.setSize(this.canvasFrame.clientWidth,
      	      	      	this.canvasFrame.clientHeight);
  this.canvasFrame.appendChild(this.renderer.domElement);

  this.renderer.setClearColor(0x000000,1);
  this.scene=new THREE.Scene();					// シーン
  this.initLight();
  this.initCamera();
  this.axis=null;
};

// 光源を初期化する
ViewModel.prototype.initLight=function(){
// 平行光源
  this.directionalLight=new THREE.DirectionalLight(0xffffff,1);
  bounds.setLightPosition(this.directionalLight.position);
  this.scene.add(this.directionalLight);
// 環境光源
  this.ambientLight=new THREE.AmbientLight(0x999999);
  this.scene.add(this.ambientLight);
};

// カメラを初期化する
ViewModel.prototype.initCamera=function(){
  var aspect=this.canvasFrame.clientWidth/this.canvasFrame.clientHeight;
  var side=0.7*bounds.size,c=bounds.center;
// カメラ
  this.camera=new THREE.OrthographicCamera
    (-side*aspect,side*aspect,side,-side,
     0.01*bounds.size,100*bounds.size);
  this.camera.position.set(c.x,c.y,c.z+bounds.viewPoint);
  this.camera.up.set(0,1,0);
//  this.camera.lookAt({x:0,y:0,z:0});	Controls使用時は無効化される

// トラックボールコントロール
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

// 表示オブジェクトを追加する
ViewModel.prototype.addObject=function(obj){
  this.scene.add(obj);
};

// 表示オブジェクトを削除する
ViewModel.prototype.removeObject=function(obj){
  this.scene.remove(obj);
};

// 座標軸を設定する
ViewModel.prototype.setAxis=function(){
  if(this.axis!==null){
    this.scene.remove(this.axis);
  }
  this.axis=new THREE.AxisHelper(0.2*bounds.size);
  this.axis.position.set(0,0,0);
  this.scene.add(this.axis);
};

// 光源・カメラ位置を更新する
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

// 表示を更新する
ViewModel.prototype.update=function(){
  this.trackball.update();
  this.renderer.render(this.scene,this.camera);
};

// 視点をX軸方向にする
ViewModel.prototype.viewX=function(){
  var c=bounds.center;
  this.camera.position.set(c.x+bounds.viewPoint,c.y,c.z);
  this.camera.up.set(0,0,1);
  this.camera.zoom=1;
  this.camera.updateProjectionMatrix();
  this.trackball.target.copy(c);
};

// 視点をY軸方向にする
ViewModel.prototype.viewY=function(){
  var c=bounds.center;
  this.camera.position.set(c.x,c.y-bounds.viewPoint,c.z);
  this.camera.up.set(0,0,1);
  this.camera.zoom=1;
  this.camera.updateProjectionMatrix();
  this.trackball.target.copy(c);
};

// 視点をZ軸方向にする
ViewModel.prototype.viewZ=function(){
  var c=bounds.center;
  this.camera.position.set(c.x,c.y,c.z+bounds.viewPoint);
  this.camera.up.set(0,1,0);
  this.camera.zoom=1;
  this.camera.updateProjectionMatrix();
  this.trackball.target.copy(c);
};

//--------------------------------------------------------------------//
// モデル境界
var Bounds=function(){
  this.box=new THREE.Box3();
  this.center=new THREE.Vector3();
  this.size=1;
  this.viewPoint=1;
};

// モデル境界を設定する
Bounds.prototype.set=function(){
  this.box.setFromPoints(model.mesh.nodes);
  this.center.copy(this.box.getCenter());
  this.size=Math.max(this.box.max.x-this.box.min.x,
      	      	     this.box.max.y-this.box.min.y,
      	      	     this.box.max.z-this.box.min.z);
  this.viewPoint=2*this.size;
};

// 光源位置を設定する
// p - 光源位置
Bounds.prototype.setLightPosition=function(p){
  p.set(this.size,-this.size,this.size);
};

// ベクトル最小長さ
var MIN_VECTOR=1e-8;		// ベクトル長さの最小値
// 拘束条件表示マテリアル
var REST_MAT=new THREE.MeshBasicMaterial({color:0x0066ff});
var LOAD_COLOR=0x00ff00;	// 荷重条件表示色
var PRESS_COLOR=0xff00ff;	// 面圧条件表示色
var HTC_COLOR=0xffcc00;		// 熱伝達境界条件表示色
// 節点温度条件表示マテリアル
var TEMP_MAT=new THREE.MeshBasicMaterial({color:0xff3300});
// 要素表示マテリアル
var elemMat=new THREE.MeshStandardMaterial
  ({color:0xffffff,roughness:0.2,metalness:0.5,
    transparent:true,opacity:0.8,
    vertexColors:THREE.VertexColors,side:THREE.DoubleSide});
// 要素辺の表示マテリアル
var EDGE_MAT=new THREE.LineBasicMaterial({color:0xffffff});
// 梁要素の表示マテリアル
var BAR_MAT=new THREE.LineBasicMaterial
  ({color:0xffffff,vertexColors:THREE.VertexColors});
var meshColors=[0.9,0.9,0.9];	// メッシュのデフォルト表示色

//--------------------------------------------------------------------//
// 表示オブジェクト
var ViewObject=function(){
  this.mesh=null;	// 計算メッシュ
  this.edge=null;	// 要素辺
  this.bar=null;	// 梁要素
  this.rest=null;	// 拘束条件
  this.load=null;	// 荷重条件
  this.press=null;	// 面圧条件
  this.htc=null;	// 熱伝達境界条件
  this.temp=null;	// 節点温度条件
  this.showEdge=true;	// 要素辺表示スイッチ
};

// 表示オブジェクトを作成する
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

// 拘束条件表示オブジェクトを作成する
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

// 荷重条件表示オブジェクトを作成する
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

// 面圧条件表示オブジェクトを作成する
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

// 熱伝達境界条件表示オブジェクトを作成する
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

// 節点温度表示オブジェクトを作成する
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

// 変位を設定する
// disp - 変位
// coef - 表示係数
ViewObject.prototype.setDisplacement=function(disp,coef){
  if(disp.length===0) return;
  setGeomDisplacement(this.mesh.geometry,disp,coef);
  setGeomDisplacement(this.edge.geometry,disp,coef);
  this.bar.setDisplacement(disp,coef);
};

// コンター図を設定する
// value - コンター図データ
// minValue,maxValue - コンター図データ最小値,最大値
// type - データ保持形態
ViewObject.prototype.setContour=function(value,minValue,maxValue,type){
  var coef=1;
  if(maxValue!==minValue) coef=1/(maxValue-minValue);
  setGeomContour(this.mesh.geometry,value,minValue,coef,type);
  this.bar.setContour(value,minValue,coef,type);
};

// 結果表示を消去する
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

// コンター図を消去する
ViewObject.prototype.clearContour=function(){
  clearGeomContour(this.mesh.geometry);
  this.bar.clearContour();
};

// 要素辺表示を切り替える
// showEdge - 要素辺を表示する場合はtrue
ViewObject.prototype.setShowEdge=function(showEdge){
  this.showEdge=showEdge;
  this.setEdgeView();
};

// 要素辺表示を切り替える
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

// 表示オブジェクトを削除する
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

// 矢印集合の表示オブジェクトを削除する
// grp - 矢印集合
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

// 変位を設定する
// geometry - 座標を設定する形状データ
// disp - 変位
// coef - 表示係数
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

// 形状データのコンター図を設定する
// geometry - 対象となる形状データ
// value - コンター図データ
// minValue - コンター図データ最小値
// coef - データ変換係数
// type - データ保持形態
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

// 形状データの結果表示を消去する
// geometry - 対象となる形状データ
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

// 形状データのコンター図を消去する
// geometry - 対象となる形状データ
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
// 拘束条件表示オブジェクト
// rest - 拘束条件
// size - 表示サイズ
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

// 表示オブジェクトを削除する
RestraintHelper.prototype.removeChildren=function(){
  for(var i=this.children.length-1;i>=0;i--){
    var child=this.children[i];
    child.geometry.dispose();
    child.material.dispose();
    this.remove(child);
  }
};

//--------------------------------------------------------------------//
// 梁要素表示オブジェクト
// geometry - 要素中心線の形状データ
var BarObject=function(geometry){
  THREE.Group.call(this);
  this.center=new THREE.LineSegments(geometry,BAR_MAT);
  this.add(this.center);
  var param=geometry.param;
  var count=2*param.length;	// １要素に断面２つ（内側と外側）
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

// 断面の位置を設定する
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

// 変位を設定する
// disp - 変位
// coef - 表示係数
BarObject.prototype.setDisplacement=function(disp,coef){
  setGeomDisplacement(this.center.geometry,disp,coef);
  this.setSection();
};

// コンター図を設定する
// value - コンター図データ
// minValue - コンター図データ最小値
// coef - データ変換係数
// type - データ保持形態
BarObject.prototype.setContour=function(value,minValue,coef,type){
  setGeomContour(this.center.geometry,value,minValue,coef,type);
};

// 結果表示を消去する
BarObject.prototype.removeResult=function(){
  removeGeomResult(this.center.geometry);
  this.setSection();
};

// コンター図を消去する
BarObject.prototype.clearContour=function(){
  clearGeomContour(this.center.geometry);
};

// 表示オブジェクトを削除する
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
// モデル表示設定
var ViewConfig=function(){
  var canvas=document.getElementById('el-color');	// 要素表示色設定用
  canvas.width=canvas.clientWidth;
  canvas.height=canvas.clientHeight;
  this.cpicker=new ColorPicker(canvas);			// カラーピッカー
  this.showEdge=document.getElementById('showedge');	// 要素辺表示
  this.lightx=document.getElementById('lightx');	// 光源位置設定用
  this.lighty=document.getElementById('lighty');
  this.lightz=document.getElementById('lightz');
};

// コンフィグウィンドウを表示する
ViewConfig.prototype.show=function(){
  this.cpicker.setColor(meshColors);
  var lp=viewModel.directionalLight.position;
  this.lightx.value=numString(lp.x);
  this.lighty.value=numString(lp.y);
  this.lightz.value=numString(lp.z);
};

// コンフィグを設定する
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

// コンフィグウィンドウを表示する
function showConfig(){
  showModalWindow(CONFIG_WINDOW);
  viewConfig.show();
}

// コンフィグを設定する
function setConfig(){
  hideModalWindow(CONFIG_WINDOW);
  viewConfig.set();
}

// コンフィグを取り消す
function cancelConfig(){
  hideModalWindow(CONFIG_WINDOW);
}

inherits(RestraintHelper,THREE.Group);
inherits(BarObject,THREE.Group);

var cls=[];					// コンター図色
var CBAR_FONT_SIZE=14;				// フォントサイズ
var CBAR_FONT=CBAR_FONT_SIZE+"px 'Arial'";	// フォント
var FG_COLOR='#ffffff';				// 前景色

//--------------------------------------------------------------------//
// カラーバー
// canvasId - 表示領域ID
var ColorBar=function(canvasId){
  this.canvas=document.getElementById(canvasId);	// 描画キャンバス
  this.canvas.width=this.canvas.clientWidth;
  this.canvas.height=this.canvas.clientHeight;
  this.context=this.canvas.getContext('2d');		// コンテキスト
  this.context.font=CBAR_FONT;
  this.context.textBaseline='top';
  this.rect={x:parseInt(this.canvas.width/5),		// カラーバーの描画領域
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
// 数値表示位置
  this.maxPos={x:xt,y:parseInt(this.rect.y-CBAR_FONT_SIZE/2)};
  this.minPos={x:xt,y:this.maxPos.y+this.rect.height};
};

// コンター図を描画する
// minValue - コンター図データ最小値
// maxValue - コンター図データ最大値
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

// コンター図を消去する
ColorBar.prototype.clear=function(){
  this.context.clearRect(0,0,this.canvas.width,this.canvas.height);
};

// コンター図の色を返す
// z - 0～1の値
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

var KS_RECT=5/6;			// 矩形断面のせん断補正係数
var KS_CIRCLE=6/7;			// 円形断面のせん断補正係数

//--------------------------------------------------------------------//
// 材料
// label - 材料番号
// ee - ヤング率 (縦弾性係数) 
// nu - ポアソン比
// dens - 密度
// hCon - 熱伝導率
// sHeat - 比熱
var Material=function(label,ee,nu,dens,hCon,sHeat){
  this.label=label;
  this.ee=ee;
  this.nu=nu;
  this.dens=dens;
  this.hCon=hCon;
  this.sHeat=sHeat;
  this.gg=0.5*ee/(1+nu);	// 横弾性係数
  this.cv=dens*sHeat;		// 体積比熱
  this.matrix=null;		// 応力 - 歪マトリックス
};

// 平面応力問題の応力 - 歪マトリックスを作成する
Material.prototype.matrix2Dstress=function(){
  var coef=this.ee/(1-this.nu*this.nu);
  return [[coef,coef*this.nu,0],[coef*this.nu,coef,0],[0,0,this.gg]];
};

// 平面歪問題の応力 - 歪マトリックスを作成する
Material.prototype.matrix2Dstrain=function(){
  var coef=this.ee/((1+this.nu)*(1-2*this.nu));
  return [[coef*(1-this.nu),coef*this.nu,0],
      	  [coef*this.nu,coef*(1-this.nu),0],[0,0,this.gg]];
};

// 軸対称問題の応力 - 歪マトリックスを作成する
Material.prototype.matrixAxiSymetric=function(){
  var coef=this.ee/((1+this.nu)*(1-2*this.nu));
  var s1=coef*(1-this.nu),s2=coef*this.nu;
  return [[s1,s2,s2,0],[s2,s1,s2,0],[s2,s2,s1,0],[0,0,0,this.gg]];
};

// 捩り問題の応力 - 歪マトリックスを作成する
Material.prototype.matrixTorsion=function(){
  return [[this.gg,0],[0,this.gg]];
};

// 3次元問題の応力 - 歪マトリックスを作成する
Material.prototype.matrix3D=function(){
  var coef=this.ee/((1+this.nu)*(1-2*this.nu));
  var s1=coef*(1-this.nu),s2=coef*this.nu;
  return [[s1,s2,s2,0,0,0],[s2,s1,s2,0,0,0],[s2,s2,s1,0,0,0],
      	  [0,0,0,this.gg,0,0],[0,0,0,0,this.gg,0],[0,0,0,0,0,this.gg]];
};

// シェル要素の応力 - 歪マトリックスを作成する
Material.prototype.matrixShell=function(){
  var coef=this.ee/(1-this.nu*this.nu);
  var s2=coef*this.nu;
  return [[coef,s2,0,0,0],[s2,coef,0,0,0],[0,0,this.gg,0,0],
      	  [0,0,0,KS_RECT*this.gg,0],[0,0,0,0,KS_RECT*this.gg]];
};

// 材料を表す文字列を返す
Material.prototype.toString=function(){
  return 'Material\t'+this.label.toString(10)+'\t'+
      	 this.ee+'\t'+this.nu+'\t'+this.gg+'\t'+this.dens+'\t'+
      	 this.hCon+'\t'+this.sHeat;
};

//--------------------------------------------------------------------//
// シェルパラメータ
// label - パラメータ番号
// thickness - 厚さ
var ShellParameter=function(label,thickness){
  this.label=label;
  this.thickness=thickness;
};

// シェルパラメータを表す文字列を返す
ShellParameter.prototype.toString=function(){
  return 'ShellParameter\t'+this.label.toString(10)+'\t'+this.thickness;
};

//--------------------------------------------------------------------//
// 梁パラメータ（円形断面）
// label - パラメータ番号
// type - 断面種類
// ss - データ文字列
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

// 梁パラメータを表す文字列を返す
BarParameter.prototype.toString=function(){
  return 'BarParameter\t'+this.label.toString(10)+'\t'+this.type+'\t'+
      	 this.section.toString();
};

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

//--------------------------------------------------------------------//
// 局所座標系
// label - 座標系ラベル
// n11,n12,n13,n21,n22,n23,n31,n32,n33 - 座標変換マトリックスの成分
var Coordinates=function(label,n11,n12,n13,n21,n22,n23,n31,n32,n33){
  this.label=label;
  this.c=new THREE.Matrix3().set(n11,n12,n13,n21,n22,n23,n31,n32,n33);
};

// グローバル座標系に変換する
// x - ベクトル成分
Coordinates.prototype.toGlobal=function(x){
  var y=[],e=this.c.elements;
  for(var i=0;i<3;i++){
    y[i]=e[i]*x[0]+e[i+3]*x[1]+e[i+6]*x[2];
    y[i+3]=e[i]*x[3]+e[i+3]*x[4]+e[i+6]*x[5];
  }
  return y;
};

// 剛性マトリックスを変換する
// matrix - 剛性マトリックス
// dof - マトリックスの自由度
// idx0 - 節点ポインタ
// ndof - 節点自由度
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

// 荷重ベクトルを変換する
// vector - 荷重ベクトル
// dof - マトリックスの自由度
// idx0 - 節点ポインタ
// ndof - 節点自由度
Coordinates.prototype.transVector=function(vector,dof,idx0,ndof){
  var e=this.c.elements;
  for(var j=idx0;j<idx0+ndof;j+=3){
    var x1=vector[j],x2=vector[j+1],x3=vector[j+2];
    vector[j]=e[0]*x1+e[1]*x2+e[2]*x3;
    vector[j+1]=e[3]*x1+e[4]*x2+e[5]*x3;
    vector[j+2]=e[6]*x1+e[7]*x2+e[8]*x3;
  }
};

// 局所座標系を表す文字列を返す
Coordinates.prototype.toString=function(){
  return 'Coordinates\t'+this.label.toString(10)+'\t'+
      	  this.c.elements.join('\t');
};

//--------------------------------------------------------------------//
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

// 三角形2次要素のガウス積分の積分点座標
var GTRI2=[1/6,2/3];
// 四面体2次要素のガウス積分の積分点座標
var GTETRA2=[0.25-0.05*Math.sqrt(5),0.25+0.15*Math.sqrt(5)];
// 四角形1次要素のガウス積分の積分点座標
var GX2=[-1/Math.sqrt(3),1/Math.sqrt(3)];
// 四角形2次要素のガウス積分の積分点座標
var GX3=[-Math.sqrt(0.6),0,Math.sqrt(0.6)];
// ガウス積分の重み係数
var GW3=[5/9,8/9,5/9];
var C1_3=1/3,C1_6=1/6,C1_12=1/12,C1_24=1/24;	// 1/3,1/6,1/12,1/24

//--------------------------------------------------------------------//
// 要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var FElement=function(label,material,nodes){
  Nodes.call(this,nodes);
  this.label=label;
  this.material=material;
  this.isShell=false;		// シェル要素ではない
  this.isBar=false;		// 梁要素ではない
};

// 要素境界を返す
// element - 要素ラベル
FElement.prototype.borders=function(element){
  var count=this.borderCount(),borders=[];
  for(var i=0;i<count;i++) borders[i]=this.border(element,i);
  return borders;
};

// 積分点の剛性マトリックスを返す
// d - 応力-歪マトリックス
// b - 歪-変位マトリックスの転置行列
// coef - 係数
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

// 節点変位を1次元配列に変換する
// u - 節点変位
// dof - 節点自由度
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

// 節点変位を局所座標系・1次元配列に変換する
// u - 節点変位
// d - 方向余弦マトリックス
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

// 節点を入れ替える
// i1,i2 - 節点インデックス
function swap(nodes,i1,i2){
  var t=nodes[i1];
  nodes[i1]=nodes[i2];
  nodes[i2]=t;
}

// 平面上の角度を求める
// p0 - 基点
// p1,p2 - 頂点
function planeAngle(p0,p1,p2){
  var v1=p1.clone().sub(p0).normalize();
  var v2=p2.clone().sub(p0).normalize();
  return Math.acos(Math.min(Math.max(v1.dot(v2),0),1));
}

// 三角形の立体角を球面過剰から求める
// p0 - 基点
// p1,p2,p3 - 頂点
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

// 方向余弦マトリックスを返す
// p - 頂点座標
// axis - 断面基準方向ベクトル
function dirMatrix(p,axis){
  var v=dirVectors(p,axis);
  return [[v[0].x,v[1].x,v[2].x],[v[0].y,v[1].y,v[2].y],
      	  [v[0].z,v[1].z,v[2].z]];
}

// 方向余弦マトリックスを返す
// p - 頂点座標
// axis - 断面基準方向ベクトル
function dirVectors(p,axis){
  var v1,v2,v3;
  if(p.length==2){		// 梁要素
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
  else if(p.length>2){		// シェル要素
    v3=normalVector(p);
    v2=p[1].clone().sub(p[0]);
    v2=v3.clone().cross(v2).normalize();
    v1=v2.clone().cross(v3);
    return [v1,v2,v3];
  }
  return null;
}

// 剛性マトリックスの方向を修正する
// d - 方向余弦マトリックス
// k - 剛性マトリックス
function toDir(d,k){
  var a=numeric.dot(d,k);
  for(var i=0;i<k.length;i++){
    var ki=k[i],ai=a[i];
    for(var j=0;j<ki.length;j++){
      ki[j]=numeric.dotVV(ai,d[j]);
    }
  }
}

// 剛性マトリックスの方向を修正する
// d - 方向余弦マトリックス
// k - 剛性マトリックス
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

// 四面体2次要素の節点のξ,η,ζ座標
var TETRA2_NODE=[[0,0,0],[1,0,0],[0,1,0],[0,0,1],[0.5,0,0],[0.5,0.5,0],
      	      	 [0,0.5,0],[0,0,0.5],[0.5,0,0.5],[0,0.5,0.5]];
// 四面体2次要素の積分点のξ,η,ζ座標,重み係数
var TETRA2_INT=[[GTETRA2[0],GTETRA2[0],GTETRA2[0],C1_24],
      	      	[GTETRA2[1],GTETRA2[0],GTETRA2[0],C1_24],
      	      	[GTETRA2[0],GTETRA2[1],GTETRA2[0],C1_24],
      	      	[GTETRA2[0],GTETRA2[0],GTETRA2[1],C1_24]];
// 楔形1次要素の節点のξ,η,ζ座標
var WEDGE1_NODE=[[0,0,-1],[1,0,-1],[0,1,-1],[0,0,1],[1,0,1],[0,1,1]];
// 楔形1次要素の積分点のξ,η,ζ座標,重み係数
var WEDGE1_INT=[[C1_3,C1_3,GX2[0],0.5],[C1_3,C1_3,GX2[1],0.5]];
// 楔形2次要素の節点のξ,η,ζ座標
var WEDGE2_NODE=[[0,0,-1],[1,0,-1],[0,1,-1],[0,0,1],[1,0,1],[0,1,1],
      	      	 [0.5,0,-1],[0.5,0.5,-1],[0,0.5,-1],
      	      	 [0.5,0,1],[0.5,0.5,1],[0,0.5,1],
      	      	 [0,0,0],[1,0,0],[0,1,0]];
// 楔形2次要素の積分点のξ,η,ζ座標,重み係数
var WEDGE2_INT=[[GTRI2[0],GTRI2[0],GX3[0],C1_6*GW3[0]],
      	      	[GTRI2[1],GTRI2[0],GX3[0],C1_6*GW3[0]],
      	      	[GTRI2[0],GTRI2[1],GX3[0],C1_6*GW3[0]],
      	      	[GTRI2[0],GTRI2[0],GX3[1],C1_6*GW3[1]],
      	      	[GTRI2[1],GTRI2[0],GX3[1],C1_6*GW3[1]],
      	      	[GTRI2[0],GTRI2[1],GX3[1],C1_6*GW3[1]],
      	      	[GTRI2[0],GTRI2[0],GX3[2],C1_6*GW3[2]],
      	      	[GTRI2[1],GTRI2[0],GX3[2],C1_6*GW3[2]],
      	      	[GTRI2[0],GTRI2[1],GX3[2],C1_6*GW3[2]]];
// 六面体1次要素の節点のξ,η,ζ座標
var HEXA1_NODE=[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],
      	      	[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]];
// 六面体1次要素の積分点のξ,η,ζ座標,重み係数
var HEXA1_INT=[[GX2[0],GX2[0],GX2[0],1],[GX2[1],GX2[0],GX2[0],1],
      	       [GX2[0],GX2[1],GX2[0],1],[GX2[1],GX2[1],GX2[0],1],
      	       [GX2[0],GX2[0],GX2[1],1],[GX2[1],GX2[0],GX2[1],1],
      	       [GX2[0],GX2[1],GX2[1],1],[GX2[1],GX2[1],GX2[1],1]];
// 六面体2次要素の節点のξ,η,ζ座標
var HEXA2_NODE=[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],
      	      	[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1],
      	      	[0,-1,-1],[1,0,-1],[0,1,-1],[-1,0,-1],
      	      	[0,-1,1],[1,0,1],[0,1,1],[-1,0,1],
      	      	[-1,-1,0],[1,-1,0],[1,1,0],[-1,1,0]];
// 六面体2次要素の積分点のξ,η,ζ座標,重み係数
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
// 六面体1次要素の質量マトリックス係数
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
// ソリッド要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
// nodeP - 節点のξ,η,ζ座標
// intP - 積分点のξ,η,ζ座標,重み係数
var SolidElement=function(label,material,nodes,nodeP,intP){
  FElement.call(this,label,material,nodes);
  this.nodeP=nodeP;
  this.intP=intP;
};

// ヤコビ行列を返す
// p - 要素節点
// sf - 形状関数行列
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

// 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
// p - 要素節点
// ja - ヤコビ行列
// sf - 形状関数行列
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

// 歪 - 変位マトリックスの転置行列を返す
// grad - 形状関数の勾配
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

// 積分点の形状関数マトリックス [ NiNj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
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

// 積分点の拡散マトリックス [ ∇Ni・∇Nj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
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

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
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

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
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

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
SolidElement.prototype.shapeFunctionMatrix=function(p,coef){
  var count=this.nodeCount(),s=numeric.rep([count,count],0);
  for(var i=0;i<this.intP.length;i++){
    addMatrix(s,this.shapePart(p,this.intP[i],coef*this.intP[i][3]));
  }
  return s;
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
SolidElement.prototype.gradMatrix=function(p,coef){
  var count=this.nodeCount(),g=numeric.rep([count,count],0);
  for(var i=0;i<this.intP.length;i++){
    addMatrix(g,this.gradPart(p,this.intP[i],coef*this.intP[i][3]));
  }
  return g;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
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

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
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

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// x - ξ,η,ζ座標
SolidElement.prototype.strainPart=function(p,v,x){
  var sf=this.shapeFunction(x[0],x[1],x[2]);
  var ja=this.jacobianMatrix(p,sf);
  var sm=this.strainMatrix(this.grad(p,ja,sf));
  return numeric.dotVM(v,sm);
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
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

// 要素を表す文字列を返す
// materials - 材料
// p - 節点
SolidElement.prototype.toString=function(materials,p){
  var s=this.getName()+'\t'+this.label.toString(10)+'\t'+
      	materials[this.material].label.toString(10);
  for(var i=0;i<this.nodes.length;i++){
    s+='\t'+p[this.nodes[i]].label.toString(10);
  }
  return s;
};

//--------------------------------------------------------------------//
// 四面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var TetraElement1=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,null,null);
};

// 要素名称を返す
TetraElement1.prototype.getName=function(){
  return 'TetraElement1';
};

// 節点数を返す
TetraElement1.prototype.nodeCount=function(){
  return 4;
};

// 要素境界数を返す
TetraElement1.prototype.borderCount=function(){
  return 4;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
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

// 要素を鏡像反転する
TetraElement1.prototype.mirror=function(){
  swap(this.nodes,1,2);
};

// 要素節点の角度を返す
// p - 要素節点
TetraElement1.prototype.angle=function(p){
  var th=[];
  for(var i=0;i<4;i++){
    th[i]=solidAngle(p[i],p[(i+1)%4],p[(i+2)%4],p[(i+3)%4]);
  }
  return th;
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
TetraElement1.prototype.shapeFunction=function(xsi,eta,zeta){
  return [[1-xsi-eta-zeta,-1,-1,-1],[xsi,1,0,0],[eta,0,1,0],
      	  [zeta,0,0,1]];
};

// ヤコビアンを返す
// p - 要素節点
TetraElement1.prototype.jacobian=function(p){
  var p0x=p[0].x,p0y=p[0].y,p0z=p[0].z;
  var j11=(p[2].y-p0y)*(p[3].z-p0z)-(p[3].y-p0y)*(p[2].z-p0z);
  var j21=(p[3].y-p0y)*(p[1].z-p0z)-(p[1].y-p0y)*(p[3].z-p0z);
  var j31=(p[1].y-p0y)*(p[2].z-p0z)-(p[2].y-p0y)*(p[1].z-p0z);
  return (p[1].x-p0x)*j11+(p[2].x-p0x)*j21+(p[3].x-p0x)*j31;
};

// 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
// p - 要素節点
// ja - ヤコビアン
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

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
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

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.stiffnessMatrix=function(p,d1){
  var ja=this.jacobian(p);
  return this.stiffPart(d1,this.strainMatrix(this.grad(p,ja)),
      	      	      	C1_6*Math.abs(ja));
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
TetraElement1.prototype.shapeFunctionMatrix=function(p,coef){
  var value=0.1*C1_6*coef*Math.abs(this.jacobian(p)),vh=0.5*value;
  return [[value,vh,vh,vh],[vh,value,vh,vh],[vh,vh,value,vh],
      	  [vh,vh,vh,value]];
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
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

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
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

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
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

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
TetraElement1.prototype.elementStrainStress=function(p,u,d1){
  var sm=this.strainMatrix(this.grad(p,this.jacobian(p)));
  var eps=numeric.dotVM(this.toArray(u,3),sm);
  var str=numeric.dotMV(d1,eps);
  var energy=0.5*numeric.dotVV(eps,str);
  return [new Strain(eps),new Stress(str),energy];
};

//--------------------------------------------------------------------//
// 四面体2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var TetraElement2=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,TETRA2_NODE,TETRA2_INT);
};

// 要素名称を返す
TetraElement2.prototype.getName=function(){
  return 'TetraElement2';
};

// 節点数を返す
TetraElement2.prototype.nodeCount=function(){
  return 10;
};

// 要素境界数を返す
TetraElement2.prototype.borderCount=function(){
  return 4;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
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

// 要素を鏡像反転する
TetraElement2.prototype.mirror=function(){
  swap(this.nodes,1,2);
  swap(this.nodes,4,6);
  swap(this.nodes,8,9);
};

// 要素節点の角度を返す
// p - 要素節点
TetraElement2.prototype.angle=function(p){
  return [solidAngle(p[0],p[4],p[6],p[7]),
      	  solidAngle(p[1],p[5],p[4],p[8]),
      	  solidAngle(p[2],p[6],p[5],p[9]),
      	  solidAngle(p[3],p[7],p[8],p[9]),
      	  planeAngle(p[4],p[2],p[3]),planeAngle(p[5],p[0],p[3]),
      	  planeAngle(p[6],p[1],p[3]),planeAngle(p[7],p[1],p[2]),
      	  planeAngle(p[8],p[2],p[0]),planeAngle(p[9],p[0],p[1])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
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
// 楔形1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var WedgeElement1=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,WEDGE1_NODE,WEDGE1_INT);
};

// 要素名称を返す
WedgeElement1.prototype.getName=function(){
  return 'WedgeElement1';
};

// 節点数を返す
WedgeElement1.prototype.nodeCount=function(){
  return 6;
};

// 要素境界数を返す
WedgeElement1.prototype.borderCount=function(){
  return 5;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
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

// 要素を鏡像反転する
WedgeElement1.prototype.mirror=function(){
  swap(this.nodes,1,2);
  swap(this.nodes,4,5);
};

// 要素節点の角度を返す
// p - 要素節点
WedgeElement1.prototype.angle=function(p){
  var th=[];
  for(var i=0;i<3;i++){
    th[i]=solidAngle(p[i],p[(i+1)%3],p[(i+2)%3],p[i+3]);
    th[i+3]=solidAngle(p[i+3],p[(i+1)%3+3],p[(i+2)%3+3],p[i]);
  }
  return th;
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
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

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
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
// 楔形2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var WedgeElement2=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,WEDGE2_NODE,WEDGE2_INT);
};

// 要素名称を返す
WedgeElement2.prototype.getName=function(){
  return 'WedgeElement2';
};

// 節点数を返す
WedgeElement2.prototype.nodeCount=function(){
  return 15;
};

// 要素境界数を返す
WedgeElement2.prototype.borderCount=function(){
  return 5;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
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

// 要素を鏡像反転する
WedgeElement2.prototype.mirror=function(){
  swap(this.nodes,1,2);
  swap(this.nodes,4,5);
  swap(this.nodes,6,8);
  swap(this.nodes,9,11);
  swap(this.nodes,13,14);
};

// 要素節点の角度を返す
// p - 要素節点
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

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
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
// 六面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var HexaElement1=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,HEXA1_NODE,HEXA1_INT);
};

// 要素名称を返す
HexaElement1.prototype.getName=function(){
  return 'HexaElement1';
};

// 節点数を返す
HexaElement1.prototype.nodeCount=function(){
  return 8;
};

// 要素境界数を返す
HexaElement1.prototype.borderCount=function(){
  return 6;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
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

// 要素を鏡像反転する
HexaElement1.prototype.mirror=function(){
  swap(this.nodes,1,3);
  swap(this.nodes,5,7);
};

// 要素節点の角度を返す
// p - 要素節点
HexaElement1.prototype.angle=function(p){
  var th=[];
  for(var i=0;i<4;i++){
    th[i]=solidAngle(p[i],p[(i+1)%4],p[(i+3)%4],p[i+4]);
    th[i+4]=solidAngle(p[i+4],p[(i+1)%4+4],p[(i+3)%4+4],p[i]);
  }
  return th;
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
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

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
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
// 六面体セレンディピティ族2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var HexaElement2=function(label,material,nodes){
  SolidElement.call(this,label,material,nodes,HEXA2_NODE,HEXA2_INT);
};

// 要素名称を返す
HexaElement2.prototype.getName=function(){
  return 'HexaElement2';
};

// 節点数を返す
HexaElement2.prototype.nodeCount=function(){
  return 20;
};

// 要素境界数を返す
HexaElement2.prototype.borderCount=function(){
  return 6;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
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

// 要素を鏡像反転する
HexaElement2.prototype.mirror=function(){
  swap(this.nodes,1,3);
  swap(this.nodes,5,7);
  swap(this.nodes,8,11);
  swap(this.nodes,9,10);
  swap(this.nodes,12,15);
  swap(this.nodes,13,14);
  swap(this.nodes,17,19);
};

// 要素節点の角度を返す
// p - 要素節点
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

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
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

// 三角形1次要素の節点のξ,η座標
var TRI1_NODE=[[0,0],[1,0],[0,1]];
// 三角形1次要素の積分点のξ,η座標,重み係数
var TRI1_INT=[[C1_3,C1_3,0.5]];
// 三角形2次要素の積分点のξ,η座標,重み係数
var TRI2_INT=[[GTRI2[0],GTRI2[0],C1_6],[GTRI2[1],GTRI2[0],C1_6],
      	      [GTRI2[0],GTRI2[1],C1_6]];
// 四角形1次要素の節点のξ,η座標
var QUAD1_NODE=[[-1,-1],[1,-1],[1,1],[-1,1]];
// 四角形1次要素の積分点のξ,η座標,重み係数
var QUAD1_INT=[[GX2[0],GX2[0],1],[GX2[1],GX2[0],1],[GX2[0],GX2[1],1],
      	       [GX2[1],GX2[1],1]];
// 三角形1次要素の質量マトリックス係数
var TRI1_MASS1=[[1,0.5,0.5],[0.5,1,0.5],[0.5,0.5,1]];

//--------------------------------------------------------------------//
// シェル要素
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
// nodeP - 節点のξ,η座標
// intP - 積分点のξ,η座標,重み係数
var ShellElement=function(label,material,param,nodes,nodeP,intP){
  FElement.call(this,label,material,nodes);
  this.param=param;
  this.isShell=true;
  this.nodeP=nodeP;
  this.intP=intP;
};

// 要素境界数を返す
ShellElement.prototype.borderCount=function(){
  return 2;
};

// 要素節点の角度を返す
// p - 要素節点
ShellElement.prototype.angle=function(p){
  var th=[],count=this.nodeCount();
  for(var i=0;i<count;i++){
    th[i]=planeAngle(p[i],p[(i+1)%count],p[(i+count-1)%count]);
  }
  return th;
};

// ヤコビ行列を返す
// p - 要素節点
// sf - 形状関数行列
// n - 法線ベクトル
// t - 要素厚さ
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

// 逆ヤコビ行列を返す
// ja - ヤコビ行列
// d - 方向余弦マトリックス
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

// 形状関数の勾配 [ dNi/dx dNi/dy ] を返す
// p - 要素節点
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
// t - 要素厚さ
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

// 歪 - 変位マトリックスの転置行列を返す
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
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

// 歪 - 変位マトリックスの転置行列を返す
// ただし歪は要素面座標、変位は全体座標
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
// zeta - 節点のζ座標
// t - 要素厚さ
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

// 積分点の形状関数マトリックス [ NiNj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
// t - 要素厚さ
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

// 積分点の拡散マトリックス [ ∇Ni・∇Nj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
// t - 要素厚さ
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

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
ShellElement.prototype.shapeFunctionMatrix=function(p,coef,sp){
  var count=this.nodeCount(),s=numeric.rep([count,count],0);
  var t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    addMatrix(s,this.shapePart(p,this.intP[i],coef*this.intP[i][2],t));
  }
  return s;
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
ShellElement.prototype.gradMatrix=function(p,coef,sp){
  var count=this.nodeCount(),g=numeric.rep([count,count],0);
  var t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    addMatrix(g,this.gradPart(p,this.intP[i],coef*this.intP[i][2],t));
  }
  return g;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
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

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
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

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// n - 法線ベクトル
// d - 方向余弦マトリックス
// xsi,eta,zeta - ξ,η,ζ座標
// t - 要素厚さ
ShellElement.prototype.strainPart=function(p,v,n,d,xsi,eta,zeta,t){
  var sf=this.shapeFunction(xsi,eta);
  var ja=this.jacobianMatrix(p,sf,n,t);
  var sm=this.strainMatrix(ja,sf,d,zeta,t);
  return numeric.dotVM(v,sm);
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
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

// ベクトルを歪に変換する
// s - 歪ベクトル
ShellElement.prototype.toStrain=function(s){
  return new Strain([s[0],s[1],0,s[2],s[3],s[4]]);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
ShellElement.prototype.toStress=function(s){
  return new Stress([s[0],s[1],0,s[2],s[3],s[4]]);
};

// 要素を表す文字列を返す
// materials - 材料
// params - シェルパラメータ
// p - 節点
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
// 三角形1次要素 (薄肉シェル)
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
var TriElement1=function(label,material,param,nodes){
  ShellElement.call(this,label,material,param,nodes,TRI1_NODE,TRI1_INT);
};

// 要素名称を返す
TriElement1.prototype.getName=function(){
  return 'TriElement1';
};

// 節点数を返す
TriElement1.prototype.nodeCount=function(){
  return 3;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
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

// 要素境界辺を返す
// element - 要素ラベル
// index - 要素境界辺のインデックス
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

// 要素を鏡像反転する
TriElement1.prototype.mirror=function(){
  swap(this.nodes,1,2);
};

// １次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
TriElement1.prototype.shapeFunction=function(xsi,eta){
  return [[1-xsi-eta,-1,-1],[xsi,1,0],[eta,0,1]];
};

// ２次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
TriElement1.prototype.shapeFunction2=function(xsi,eta){
  var xe=1-xsi-eta;
  return [[xe*(2*xe-1),1-4*xe,1-4*xe],[xsi*(2*xsi-1),4*xsi-1,0],
      	  [eta*(2*eta-1),0,4*eta-1],[4*xe*xsi,4*(xe-xsi),-4*xsi],
      	  [4*xsi*eta,4*eta,4*xsi],[4*xe*eta,-4*eta,4*(xe-eta)]];
};

// 角度の形状関数行列 [ Hxi Hyi dHxi/dξ dHyi/dξ dHxi/dη dHyi/dη ] を返す
// p - 要素節点
// d - 方向余弦マトリックス
// xsi,eta - 要素内部ξ,η座標
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

// ヤコビアンを返す
// p - 要素節点
TriElement1.prototype.jacobian=function(p){
  var p0x=p[0].x,p0y=p[0].y,p0z=p[0].z;
  var j1=(p[1].y-p0y)*(p[2].z-p0z)-(p[1].z-p0z)*(p[2].y-p0y);
  var j2=(p[1].z-p0z)*(p[2].x-p0x)-(p[1].x-p0x)*(p[2].z-p0z);
  var j3=(p[1].x-p0x)*(p[2].y-p0y)-(p[1].y-p0y)*(p[2].x-p0x);
  return Math.sqrt(j1*j1+j2*j2+j3*j3);
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
// t - 要素厚さ
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

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
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

// 歪 - 変位マトリックスの転置行列を返す
// sf - 形状関数行列
// jinv - 逆ヤコビ行列
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

// 面外歪 - 変位マトリックスの転置行列を返す
// sf - 形状関数行列
// jinv - 逆ヤコビ行列
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

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// t - 要素厚さ
TriElement1.prototype.shapeFunctionMatrix=function(p,coef,t){
  var ds=coef*this.jacobian(p)/12;
  var count=3*this.nodeCount(),s=numeric.rep([count],0.5*ds);
  for(var i=0;i<count;i++) s[i][i]=ds;
  return s;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
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

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// n - 法線ベクトル
// d - 方向余弦マトリックス
// xsi,eta,zeta - ξ,η,ζ座標
// t - 要素厚さ
TriElement1.prototype.strainPart=function(p,v,n,d,xsi,eta,zeta,t){
  var sf1=this.shapeFunction(xsi,eta);
  var ja=this.jacobianMatrix(p,sf1,n,t);
  var jinv=this.jacobInv(ja,d);
  var sf3=this.shapeFunction3(p,d,xsi,eta);
  var sm=this.strainMatrix(sf1,sf3,jinv,d,zeta,t);
  return numeric.dotVM(v,sm);
};

// 歪 - 変位マトリックスの転置行列を返す
// ただし歪は要素面座標、変位は全体座標
// sf1 - 面内変形の形状関数行列
// sf3 - 面外変形の形状関数行列
// jinv - 逆ヤコビ行列
// d - 方向余弦マトリックス
// zeta - 節点のζ座標
// t - 要素厚さ
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

// ベクトルを歪に変換する
// s - 歪ベクトル
TriElement1.prototype.toStrain=function(s){
  return new Strain([s[0],s[1],0,s[2],0,0]);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
TriElement1.prototype.toStress=function(s){
  return new Stress([s[0],s[1],0,s[2],0,0]);
};

//--------------------------------------------------------------------//
// 四角形1次要素 (MITC4)
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
var QuadElement1=function(label,material,param,nodes){
  ShellElement.call(this,label,material,param,nodes,
      	      	    QUAD1_NODE,QUAD1_INT);
};

// 要素名称を返す
QuadElement1.prototype.getName=function(){
  return 'QuadElement1';
};

// 節点数を返す
QuadElement1.prototype.nodeCount=function(){
  return 4;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
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

// 要素境界辺を返す
// element - 要素ラベル
// index - 要素境界辺のインデックス
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

// 要素を鏡像反転する
QuadElement1.prototype.mirror=function(){
  swap(this.nodes,1,3);
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
QuadElement1.prototype.shapeFunction=function(xsi,eta){
  return [[0.25*(1-xsi)*(1-eta),-0.25*(1-eta),-0.25*(1-xsi)],
      	  [0.25*(1+xsi)*(1-eta),0.25*(1-eta),-0.25*(1+xsi)],
      	  [0.25*(1+xsi)*(1+eta),0.25*(1+eta),0.25*(1+xsi)],
      	  [0.25*(1-xsi)*(1+eta),-0.25*(1+eta),0.25*(1-xsi)]];
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
// t - 要素厚さ
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

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
QuadElement1.prototype.stiffnessMatrix=function(p,d1,sp){
  var size=6*this.nodeCount(),kk=numeric.rep([size,size],0);
  var n=normalVector(p),t=sp.thickness;
  for(var i=0;i<this.intP.length;i++){
    var ks=this.stiffPart(p,d1,n,this.intP[i][0],this.intP[i][1],t);
    addMatrix(kk,ks);
  }
  return kk;
};

// 積分点の剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// n - 法線ベクトル
// xsi,eta - 要素内部ξ,η座標
// t - 要素厚さ
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

// 節点座標を局所座標系に変換する
// d - 方向余弦マトリックス
// p - 要素節点
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

var PRECISION=1e-10;	// マトリックス精度
var LU_METHOD=0;	// LU分解法
var ILUCG_METHOD=1;	// 不完全LU分解共役勾配法

//--------------------------------------------------------------------//
// 連立方程式求解オブジェクト
var Solver=function(){
  this.matrix=[];		// 行列
  this.matrix2=[];		// 第２行列
  this.vector=[];		// ベクトル
  this.dof=0;			// モデル自由度
  this.method=LU_METHOD;	// 方程式解法
};

// データを消去する
Solver.prototype.clear=function(){
  this.matrix.length=0;
  this.matrix2.length=0;
  this.vector.length=0;
  this.dof=0;
};

// 剛性マトリックス・荷重ベクトルを作成する
Solver.prototype.createStiffnessMatrix=function(){
  var i,bc=model.bc,bcList=bc.bcList,reducedList=[];
  for(i=0;i<bcList.length;i++){
    if(bcList[i]<0){
      reducedList.push(i);
    }
  }

// 剛性マトリックス・荷重ベクトルの作成
  var matrix1=stiffnessMatrix(this.dof),vector1=loadVector(this.dof);

// 拘束自由度を除去する
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

// 剛性マトリックス・質量マトリックスを作成する
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

// 幾何剛性マトリックスを作成する
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

// 熱計算のマトリックス・ベクトルを計算する
Solver.prototype.createHeatMatrix=function(){
  var i,bcList=model.bc.bcList,reducedList=[];
  for(i=0;i<bcList.length;i++){
    if(bcList[i]<0){
      reducedList.push(i);
    }
  }

// 伝熱マトリックス・熱境界条件ベクトルの作成
  var matrix1=heatMatrix(),vector1=tempVector(matrix1);

// 拘束自由度を除去する
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

// 行列の一部を抽出する
// matrix1,vector1 - 元のマトリックス,ベクトル
// list - 抽出部分のリスト
Solver.prototype.extruct=function(matrix1,vector1,list){
  this.matrix.length=0;
  this.vector.length=0;
  for(var i=0;i<list.length;i++){
    this.vector[i]=vector1[list[i]];
    this.matrix[i]=extructRow(matrix1[list[i]],list);
  }
};

// 連立方程式を解く
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

// ランチョス法で固有値・固有ベクトルを求める
// n - ３重対角化行列の大きさ
// delta - シフト量δ
Solver.prototype.eigenByLanczos=function(n,delta){
  switch(this.method){
    case LU_METHOD:
      return eigByLanczosLUP(this.matrix,this.matrix2,n,delta);
    case ILUCG_METHOD:
      return eigByLanczosILUCG(this.matrix,this.matrix2,n,delta);
  }
};

// アーノルディ法で固有値・固有ベクトルを求める
// n - ３重対角化行列の大きさ
// delta - シフト量δ
Solver.prototype.eigenByArnoldi=function(n,delta){
  switch(this.method){
    case LU_METHOD:
      return eigByArnoldiLUP(this.matrix,this.matrix2,n,delta);
    case ILUCG_METHOD:
      return eigByArnoldiILUCG(this.matrix,this.matrix2,n,delta);
  }
};

// 質量マトリックスを作成する
// dof - モデル自由度
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
// 座標変換
  var rests=model.bc.restraints;
  var index=model.bc.nodeIndex,bcdof=model.bc.dof;
  for(i=0;i<rests.length;i++){
    var ri=rests[i];
    if(ri.coords){
      ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i]);
    }
  }
// 絶対値が小さい成分を除去する
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

// 剛性マトリックスを作成する
// dof - モデル自由度
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
// 座標変換
  var rests=model.bc.restraints;
  var index=model.bc.nodeIndex,bcdof=model.bc.dof;
  for(i=0;i<rests.length;i++){
    var ri=rests[i];
    if(ri.coords){
      ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i]);
    }
  }
// 絶対値が小さい成分を除去する
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

// 幾何剛性マトリックスを作成する
// dof - モデル自由度
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
// 座標変換
  var rests=model.bc.restraints;
  var index=model.bc.nodeIndex,bcdof=model.bc.dof;
  for(i=0;i<rests.length;i++){
    var ri=rests[i];
    if(ri.coords){
      ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i]);
    }
  }
// 絶対値が小さい成分を除去・符号反転
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

// 要素のマトリックスを設定する
// element - 要素
// dof - 自由度
// matrix - 全体剛性マトリックス
// km - 要素の剛性マトリックス
// kmax - 成分の絶対値の最大値
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

// 荷重ベクトルを作成する
// dof - モデル自由度
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

// 伝熱マトリックスを作成する
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

// 熱境界条件ベクトルを作成する
// matrix - 伝熱マトリックス
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

// 行列の行から一部を抽出する
// mrow - 元のマトリックスの行データ
// list - 抽出部分のリスト
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

// 計算を開始する
function calcStart(){
  info.textContent='計算中・・・';
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

// 静解析の計算を開始する
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

// 固有振動解析の計算を開始する
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

// 線形座屈解析の計算を開始する
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

// 計算設定ウィンドウを表示する
function showCalc(){
  showModalWindow(CALC_WINDOW);
  var elems=document.getElementsByName('method');
  elems[model.solver.method].checked=true;
}

// 計算設定を取り消す
function calcCancel(){
  hideModalWindow(CALC_WINDOW);
}

//--------------------------------------------------------------------//
// 反復解法（不完全LU分解共役勾配法）

var ILUCG_THRES=1e-10;	// 不完全LU分解共役勾配法の収束閾値のデフォルト値

// 不完全LU分解をする
// a - 行列
function getILU(a){
  var m=a.length,i,j,diag=[],col=[],val=[],d=[],colData=[];
  for(i=0;i<m;i++){
    col[i]=[];
    val[i]=[];
    diag[i]=-1;
    colData[i]=[];
  }
// 列からの検索用ポインタを設定する
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
// 不完全LU分解をする
  for(var k=0;k<m-1;k++){
    var vk=val[k],ck=col[k],dk=diag[k],cdk=colData[k];
    if((dk<0) || (vk[dk]===0)) throw new Error('対角成分が0です');
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

// LU分解法で連立方程式の解を求める
// lu - LU分解した疎行列
// p - ベクトル
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

// 不完全LU分解共役勾配法で連立方程式の解を求める
// matrix - 元の行列
// ilu - 不完全LU分解した疎行列
// p - ベクトル
// iterMax - 反復回数の上限
// thres - 収束閾値
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
      throw new Error('方程式求解エラー at iter='+iter);
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

// 行列とベクトルの積を計算する
// matrix - 疎行列
// x - ベクトル
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

// 行ベースの疎行列に変換する
// a - 元の行列
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
// ランチョス法による固有値計算

var EIG_SHIFT=-0.1;	// シフト量δのデフォルト値

// ランチョス法で一般固有値問題Ax=λBxの３重対角化をする
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
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

// ランチョス法で一般固有値問題Ax=λBxの３重対角化をする
// 小さい固有値を優先させるため B(A+δB)^-1Bx=θBx に変換する
// 逆行列はLU分解法
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
// delta - シフト量δ
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

// ランチョス法で一般固有値問題Ax=λBxの３重対角化をする
// 小さい固有値を優先させるため B(A+δB)^-1Bx=θBx に変換する
// 逆行列はILUCG法
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
// delta - シフト量δ
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

// アーノルディ法で一般固有値問題Ax=λBxの３重対角化をする
// 小さい固有値を優先させるため (A+δB)^-1Bx=θx に変換する
// 逆行列はLU分解法
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
// delta - シフト量δ
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

// アーノルディ法で一般固有値問題Ax=λBxの３重対角化をする
// 小さい固有値を優先させるため (A+δB)^-1Bx=θx に変換する
// 逆行列はILUCG法
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
// delta - シフト量δ
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

// ３重対角化行列の固有値・固有ベクトルを求める
// alp,bet - ３重対角化行列の対角成分,その隣の成分
function tdmEig(alp,bet){
  var size=alp.length,t=numeric.diag(alp);
  for(var i=0;i<size-1;i++){
    t[i][i+1]=bet[i];
    t[i+1][i]=bet[i];
  }
  return eigen(t);
}

// 行列の固有値・固有ベクトルを求める
// m - 行列
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

// シフト行列A-δBを返す
// a,b - 行列A,B
// delta - シフト量δ
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

// 行列とベクトルの積を計算する
// matrix - 疎行列
// x - ベクトル
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

// データ型
var NONE=-1;		// 空データ
var DISPLACEMENT=0;	// 変位
var STRAIN=1;		// 歪
var STRESS=2;		// 応力
var S_ENERGY=3;		// 歪エネルギー密度
var TEMPERATURE=4;	// 温度
// 成分
var X=0;		// x成分
var Y=1;		// y成分
var Z=2;		// z成分
var RX=3;		// x軸回転成分
var RY=4;		// y軸回転成分
var RZ=5;		// z軸回転成分
var XY=3;		// xyせん断成分
var YZ=4;		// yzせん断成分
var ZX=5;		// zxせん断成分
var MAGNITUDE=6;	// 大きさ
var MAX_PRINCIPAL=7;	// 最大主成分
var MIN_PRINCIPAL=8;	// 最小主成分
var MID_PRINCIPAL=9;	// 中間主成分
var MAX_SHARE=10;	// 最大せん断成分
var VON_MISES=11;	// ミーゼス応力
var SHIFT=12;		// 成分シフト量
// 変位の成分
var DISP_COMPONENT=['Mag.','x','y','z'];
var DISP2_COMPONENT=['Mag.','x','y','z','rotx','roty','rotz'];
// 歪の成分
var STRAIN_COMPONENT=['Max.prin.','Min.prin.','Mid.prin.',
      	      	      'Max.share',
      	      	      'x','y','z','xy','yz','zx'];
// 応力の成分
var STRESS_COMPONENT=['Max.prin.','Min.prin.','Mid.prin.',
      	      	      'Max.share','Von mises',
      	      	      'x','y','z','xy','yz','zx'];
// 歪エネルギー密度の成分
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
var EIG_EPS=1e-10;		// 固有値計算の収束閾値
var NODE_DATA=0;		// 節点データ
var ELEMENT_DATA=1;		// 要素データ
var VIBRATION='Vibration';	// 振動解析
var BUCKLING='Buckling';	// 座屈解析

//--------------------------------------------------------------------//
// 計算結果
var Result=function(){
  this.displacement=[];		// 変位
  this.dispMax=0;		// 変位の大きさの最大値
  this.angleMax=0;		// 回転角の大きさの最大値
  this.strain1=[];		// 節点歪
  this.stress1=[];		// 節点応力
  this.sEnergy1=[];		// 節点歪エネルギー密度
  this.strain2=[];
  this.stress2=[];
  this.sEnergy2=[];
  this.temperature=[];		// 節点温度
  this.tempMax=0;		// 温度の最大値
  this.eigenValue=[];		// 固有値データ
  this.calculated=false;	// 計算前＝計算結果無し
  this.type=NODE_DATA;		// データ保持形態：節点データ
  this.value=[];		// コンター図データ
  this.minValue=0;		// コンター図データ最小値
  this.maxValue=0;		// コンター図データ最大値
};

// 計算結果を消去する
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

// 節点変位を設定する
// bc - 境界条件
// disp - 節点変位を表すベクトル
// nodeCount - 節点数
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

// 節点温度を設定する
// bc - 境界条件
// t - 節点温度を表すベクトル
// nodeCount - 節点数
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

// 節点の構造解析結果に値を加える
// i - 節点のインデックス
// eps1,str1,se1,eps2,str2,se2 - 表面・裏面の歪，応力，歪エネルギー密度
Result.prototype.addStructureData=function(i,eps1,str1,se1,
      	      	      	      	      	   eps2,str2,se2){
  this.strain1[i].add(eps1);
  this.stress1[i].add(str1);
  this.sEnergy1[i]+=se1;
  this.strain2[i].add(eps2);
  this.stress2[i].add(str2);
  this.sEnergy2[i]+=se2;
};

// 節点の構造解析結果に値を掛ける
// i - 節点のインデックス
// coef - 計算結果に掛ける係数
Result.prototype.mulStructureData=function(i,coef){
  this.strain1[i].mul(coef);
  this.stress1[i].mul(coef);
  this.sEnergy1[i]*=coef;
  this.strain2[i].mul(coef);
  this.stress2[i].mul(coef);
  this.sEnergy2[i]*=coef;
};

// 固有値データを追加する
// ev - 固有値
Result.prototype.addEigenValue=function(ev){
  this.eigenValue.push(ev);
  this.calculated=true;
};

// コンター図データを設定する
// param - データの種類
// component - データの成分
// data - コンター図参照元
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

// データを取り出す
// param - データの種類
// component - データの成分
// index - 節点のインデックス
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

// 歪・応力を取り出す
// s - 歪 or 応力
// component - データの成分
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

// 節点歪・応力を初期化する
// count - 節点数
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

// データ文字列を返す
// nodes - 節点
// elems - 要素
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
// 固有値
// value - 固有値・固有振動数
// type - 解析種類
var EigenValue=function(value,type){
  this.value=value;
  this.type=type;
  this.displacement=[];		// 変位
  this.sEnergy1=[];		// 節点歪エネルギー密度
  this.sEnergy2=[];
  this.dispMax=0;
  this.angleMax=0;
};

// 変位を設定する
// bc - 境界条件
// disp - 変位を表す固有ベクトル
// nodeCount - 節点数
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

// データを取り出す
// param - データの種類
// component - データの成分
// index - 節点のインデックス
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

// 節点歪・応力を初期化する
// count - 節点数
EigenValue.prototype.initStrainEnergy=function(count){
  this.sEnergy1.length=0;
  this.sEnergy2.length=0;
  for(var i=0;i<count;i++){
    this.sEnergy1[i]=0;
    this.sEnergy2[i]=0;
  }
};

// データ文字列を返す
// nodes - 節点
// tuple - 節点or要素
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
// ３次元対称テンソル
// s - 成分
var SymmetricTensor3=function(s){
  this.xx=s[0];
  this.yy=s[1];
  this.zz=s[2];
  this.xy=s[3];
  this.yz=s[4];
  this.zx=s[5];
};

// テンソルをベクトルとして返す
SymmetricTensor3.prototype.vector=function(){
  return [this.xx,this.yy,this.zz,this.xy,this.yz,this.zx];
};

// テンソルを加える
// t - 加えるテンソル
SymmetricTensor3.prototype.add=function(t){
  this.xx+=t.xx;
  this.yy+=t.yy;
  this.zz+=t.zz;
  this.xy+=t.xy;
  this.yz+=t.yz;
  this.zx+=t.zx;
};

// 成分にスカラーを掛ける
// a - 掛けるスカラー
SymmetricTensor3.prototype.mul=function(a){
  this.xx*=a;
  this.yy*=a;
  this.zz*=a;
  this.xy*=a;
  this.yz*=a;
  this.zx*=a;
};

// 固有値を返す
SymmetricTensor3.prototype.principal=function(){
  return eigenvalue(this,100).lambda;
};

// テンソルを回転させる
// d - 方向余弦マトリックス
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

// テンソルの内積を計算する
// t - 相手のテンソル
SymmetricTensor3.prototype.innerProduct=function(t){
  return this.xx*t.xx+this.yy*t.yy+this.zz*t.zz+
      	 2*(this.xy*t.xy+this.yz*t.yz+this.zx*t.zx);
};

// Jacobie法で対称テンソルの固有値を求める
// Numeric.jsでは対角要素が0（例えばせん断のみの条件）だと求められない
// st - 対称テンソル
// iterMax - 反復回数の最大値
function eigenvalue(st,iterMax){
  var m=[[st.xx,st.xy,st.zx],[st.xy,st.yy,st.yz],
      	 [st.zx,st.yz,st.zz]];
  return eigenByJacob(m,iterMax);
}

// Jacobie法で対称テンソルの固有値を求める
// m - 対称行列
// iterMax - 反復回数の最大値
function eigenByJacob(m,iterMax){
  var size=m.length,abs=Math.abs,i,j,iter,dataMax=0;
  var ev=numeric.identity(size);
  for(i=0;i<size;i++){
    for(j=i;j<size;j++){
      dataMax=Math.max(dataMax,abs(m[i][j]));
    }
  }
  var tolerance=EIG_EPS*dataMax;
// 値が0の場合
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
// 固有値を大きい順に入れ替える
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
// 歪
// s - 成分
var Strain=function(s){
  SymmetricTensor3.call(this,s);
  this.xy=0.5*s[3];
  this.yz=0.5*s[4];
  this.zx=0.5*s[5];
};

// テンソルをベクトルとして返す
Strain.prototype.vector=function(){
  return [this.xx,this.yy,this.zz,2*this.xy,2*this.yz,2*this.zx];
};

//--------------------------------------------------------------------//
// 応力
// s - 成分
var Stress=function(s){
  SymmetricTensor3.call(this,s);
};

// ミーゼス応力を返す
Stress.prototype.mises=function(){
  var dxy=this.xx-this.yy,dyz=this.yy-this.zz,dzx=this.zz-this.xx;
  var ss=dxy*dxy+dyz*dyz+dzx*dzx;
  var tt=this.xy*this.xy+this.yz*this.yz+this.zx*this.zx;
  return Math.sqrt(0.5*ss+3*tt);
};

//--------------------------------------------------------------------//
// 結果表示設定
var ResultView=function(){
  this.dispCoef=document.getElementById('dispcoef');	// 変形表示倍率
  this.eigen=document.getElementById('eigenvalue');	// 固有値データ
  this.contour=document.getElementById('contour');	// コンター図表示データ
  this.component=document.getElementById('component');	// コンター図表示成分
};

// 静解析の設定を初期化する
ResultView.prototype.setInitStatic=function(){
  removeOptions(this.eigen);
  this.setContourSelect();
  this.setConfig();
};

// 固有値解析の設定を初期化する
ResultView.prototype.setInitEigen=function(){
  removeOptions(this.eigen);
  var eigenValue=model.result.eigenValue;
  for(var i=0;i<eigenValue.length;i++){
    this.eigen.appendChild(createOption('固有値'+(i+1),i));
  }
  removeOptions(this.contour);
  this.contour.appendChild(createOption('コンター無し',NONE));
  this.contour.appendChild(createOption('変位',DISPLACEMENT));
  this.contour.appendChild(createOption('歪エネルギー密度',S_ENERGY));
  this.setResComp();
  this.setConfig();
};

// 表示するコンター図データを設定する
ResultView.prototype.setContourSelect=function(){
  removeOptions(this.eigen);
  removeOptions(this.contour);
  this.contour.appendChild(createOption('コンター無し',NONE));
  if(model.result.displacement.length>0){
    this.contour.appendChild(createOption('変位',DISPLACEMENT));
  }
  if(model.result.strain1.length>0){
    this.contour.appendChild(createOption('歪',STRAIN));
  }
  if(model.result.stress1.length>0){
    this.contour.appendChild(createOption('応力',STRESS));
  }
  if(model.result.sEnergy1.length>0){
    this.contour.appendChild(createOption('歪エネルギー密度',S_ENERGY));
  }
  if(model.result.temperature.length>0){
    this.contour.appendChild(createOption('温度',TEMPERATURE));
  }
  removeOptions(this.component);
};

// 表示成分を設定する
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

// 設定を表示に反映させる
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

// 設定をバックアップする
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

// 設定を元に戻す
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

// 計算結果の成分を表示する
// sel - コンボボックス
// component - 成分
// data - データ番号（1:表面,2:裏面,-1:番号なし）
function setOptions(sel,component,data){
  for(var i=0;i<component.length;i++){
    var c=component[i];
    if(data>0) c+=' '+data;
    sel.appendChild(createOption(c,COMP_MAP[c]));
  }
}

// オプション要素を作成する
// text - オプションのテキスト
// value - オプションの値
function createOption(text,value){
  var opt=document.createElement('option');
  opt.value=value;
  opt.text=text;
  return opt;
}

// コンボボックスのオプションを削除する
// sel - コンボボックス
function removeOptions(sel){
  if(sel.hasChildNodes()){
    while(sel.childNodes.length>0){
      sel.removeChild(sel.firstChild);
    }
  }
}

// 結果表示設定ウィンドウを表示する
function showResultWindow(){
  showModalWindow(RESULT_WINDOW);
  resultView.stock();
}

// 計算結果を消去する
function removeRes(){
  model.result.clear();
  viewObj.removeResult();
  colorBar.clear();
  showInfo();
  resultView.setContourSelect();
  hideModalWindow(RESULT_WINDOW);
}

// 結果表示設定を更新する
function setResultConfig(){
  hideModalWindow(RESULT_WINDOW);
  resultView.setConfig();
}

// 結果表示設定を取り消す
function cancelResultConfig(){
  hideModalWindow(RESULT_WINDOW);
  resultView.reset();
}

inherits(Strain,SymmetricTensor3);
inherits(Stress,SymmetricTensor3);

var CPICK_FONT_SIZE=14;				// フォントサイズ
var CPICK_FONT=CPICK_FONT_SIZE+"px 'Arial'";	// フォント

//--------------------------------------------------------------------//
// カラーピッカー
// canvas - 描画キャンバス
// div - 円形描画分割数
// x,y,width,height - 描画位置,幅,高さ
var ColorPicker=function(canvas,div,x,y,width,height){
  this.canvas=canvas;
  this.context=canvas.getContext('2d');	// コンテキスト
  this.context.font=CPICK_FONT;
  this.context.textBaseline='top';
  this.div=div||360;
  x=x||0;
  y=y||0;
  width=width||canvas.width;
  height=height||canvas.height;
// 中心座標
  var size=Math.min(width,height);
  this.cx=parseInt(x+0.5*size);
  this.cy=parseInt(y+0.5*size);
// 色相円形スライダー半径
  this.ro=parseInt(0.48*size);
  this.rc=parseInt(0.9*this.ro);
  this.ri=parseInt(0.8*this.ro);
// 彩度輝度マップ描画位置,サイズ
  this.rect=parseInt(1.3*this.ri);
  this.rx=parseInt(this.cx-0.5*this.rect);
  this.ry=parseInt(this.cy-0.5*this.rect);
  this.pt=parseInt(Math.max(0.07*this.ri,1));
  this.hsl=[0,0,0];			// 現在の色 (HSL色空間)
// テキスト位置
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

// 現在の色を設定する
// rgb - 指定する色 (RGB)
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

// 現在の色 (RGB) を返す
ColorPicker.prototype.getColor=function(){
  var h=this.hsl[0],s=this.hsl[1],l=this.hsl[2];
  var rgbmax=l+0.5*s*(1-Math.abs(2*l-1));
  var rgbmin=2*l-rgbmax;
  return [hueToRGB(rgbmax,rgbmin,h+1/3),hueToRGB(rgbmax,rgbmin,h),
      	  hueToRGB(rgbmax,rgbmin,h-1/3)];
};

// カラーピッカーを描画する
ColorPicker.prototype.draw=function(){
  this.context.clearRect(0,0,this.canvas.width,this.canvas.height);
// 色相スライダー
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

// 彩度輝度マップ
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

// テキスト
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

// ポインタを描画する
// x,y - 描画位置
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

// マウスイベントを初期化する
ColorPicker.prototype.initMouseEvent=function(){
  var mx,my,pressedArea=-1,cp=this;
  this.canvas.addEventListener('mousedown',cpMousePressed,false);

// マウスボタンを押した時の処理を行う
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

// マウスボタンを離した時の処理を行う
  function cpMouseReleased(e){
    e.preventDefault();
    e.stopPropagation();
    document.removeEventListener('mouseup',cpMouseReleased,false);
    document.removeEventListener('mousemove',cpMouseMoved,false);
    pressedArea=-1;
  }

// マウスボタンを移動させた時の処理を行う
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

// 色相を返す
  function hue(){
    var h=Math.atan2(cp.cy-my,mx-cp.cx)/(2*Math.PI);
    if(h<0) h+=1;
    return h;
  }

// 彩度輝度を返す
  function satLight(){
    var s=(mx-cp.rx)/cp.rect;
    var l=1-(my-cp.ry)/cp.rect;
    return [Math.min(Math.max(s,0),1),Math.min(Math.max(l,0),1)];
  }
};

// RGBカラーを返す
// rgbmax,rgbmin - R,G,Bの最大値,最小値
// h - 色相
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
// Wilson-Taylor非適合六面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var HexaElement1WT=function(label,material,nodes){
  HexaElement1.call(this,label,material,nodes);
  this.te=null;		// 非適合モードの変換マトリックス
};

// 要素境界名称を返す
HexaElement1WT.prototype.getName=function(){
  return 'HexaElement1WT';
};

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
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

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// x - ξ,η,ζ座標
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

// 非適合モードの歪 - 変位マトリックスの転置行列を返す
// ji - ヤコビ逆行列の要素配列
// x - ξ,η,ζ座標
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

// 積分点の剛性マトリックス [B1t][D][B2]を返す
// d - 応力-歪マトリックス
// b1 - 歪-変位マトリックスの転置行列 B1t
// b2 - 歪-変位マトリックスの転置行列 B2t
// coef - 係数
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
