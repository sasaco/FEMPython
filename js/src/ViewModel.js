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
