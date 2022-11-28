#include "FemDataModel.h"



FemDataModel::FemDataModel() {
    mesh = MeshModel();		    // メッシュモデル
    bc = BoundaryCondition();	// 境界条件
    solver = Solver();		    // 連立方程式求解オブジェクト
    result = Result();          // 計算結果
    hasShellBar = false;		// シェル要素または梁要素を含まない
}

// データを消去する
void FemDataModel::clear(){
    
    materials.clear();
    shellParams.clear();
    barParams.clear();
    coordinates.clear();
    mesh.clear();
    bc.clear();
    result.clear();
    result.type=NODE_DATA;
    
};

/*
// モデルを初期化する
void FemDataModel::init(){
  solver.method=ILUCG_METHOD;   // デフォルトは反復解法
  auto mats= materials;
  mats.sort(compareLabel);
  mesh.init();
  bc.init();
  reNumbering();
  resetMaterialLabel();
  resetParameterLabel();
  resetCoordinates();
  for(iont i=0;i<mats.size(); i++) {
    auto m2d = mats[i].matrix2Dstress();
    auto msh = mats[i].matrixShell();
    auto m3d = mats[i].matrix3D();
    mats[i].matrix = { 
        m2d:m2d,
        msh:msh,
        m3d:m3d
    };
  }
};

// 節点・要素ポインタを設定する
void FemDataModel::reNumbering(){

  var nodes = mesh.nodes,elements=this.mesh.elements;
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

*/