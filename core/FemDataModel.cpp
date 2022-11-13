double COEF_F_W=0.5/Math.PI;	// f/ω比 1/2π

//--------------------------------------------------------------------//
// FEM データモデル
class FemDataModel{
  this.materials=[];			// 材料
  this.shellParams=[];			// シェルパラメータ
  this.barParams=[];			// 梁パラメータ
  this.coordinates=[];			// 局所座標系
  this.mesh=new MeshModel();		// メッシュモデル
  this.bc=new BoundaryCondition();	// 境界条件
  this.solver=new Solver();		// 連立方程式求解オブジェクト
  this.result=new Result();		// 計算結果
  this.hasShellBar=false;		// シェル要素または梁要素を含まない

  // Method
  void clear();
};

// データを消去する
void FemDataModel::clear(){
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
var FENode =function(label,x,y,z){
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
