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
