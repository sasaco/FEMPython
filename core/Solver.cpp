/*
* https://learn.microsoft.com/ja-jp/visualstudio/python/working-with-c-cpp-python-in-visual-studio?view=vs-2022
*/


#include <Windows.h>
#include <cmath>
#include <pybind11/pybind11.h>
namespace py = pybind11;

// 剛性マトリックスを作成する
// dof - モデル自由度
void stiffnessMatrix(PyObject* self, PyObject* args) {
    
 
}
PYBIND11_MODULE(stiffnessMatrix, m) {
    m.def("stiffnessMatrix", &stiffnessMatrix, R"pbdoc(
        Compute a hyperbolic tangent of a single argument expressed in radians.
    )pbdoc");

    #ifdef VERSION_INFO
        m.attr("__version__") = VERSION_INFO;
    #else
        m.attr("__version__") = "dev";
    #endif
}




/*
//--------------------------------------------------------------------//
// 連立方程式求解オブジェクト
class Solver {
    double PRECISION = 1e-10;   // マトリックス精度
    int LU_METHOD = 0;	        // LU分解法
    int ILUCG_METHOD = 1;	    // 不完全LU分解共役勾配法
    int method = LU_METHOD;	    // 方程式解法

    double *matrix;		    // 行列 double型のポインタ後で配列として用いる
    double *matrix2;		// 第２行列 double型のポインタ後で配列として用いる
    double *vector;		    // ベクトル double型のポインタ後で配列として用いる

    int dof = 0;			// モデル自由度

    void clear();
    void createStiffnessMatrix();
};

// データを消去する
void Solver::clear() {
  delete[] matrix;
  delete[] matrix2;
  delete[] vector;
  dof = 0;
}

// 剛性マトリックス・荷重ベクトルを作成する
void Solver::createStiffnessMatrix(bc, bcList){
    bc = bc;
    bcList = bcList;
    reducedList = [];
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


*/
