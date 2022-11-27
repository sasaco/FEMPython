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
