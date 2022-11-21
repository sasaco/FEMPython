/*
* https://learn.microsoft.com/ja-jp/visualstudio/python/working-with-c-cpp-python-in-visual-studio?view=vs-2022
*/


#include <Windows.h>
#include <cmath>
#include <pybind11/pybind11.h>
namespace py = pybind11;

// �����}�g���b�N�X���쐬����
// dof - ���f�����R�x
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
// �A�������������I�u�W�F�N�g
class Solver {
    double PRECISION = 1e-10;   // �}�g���b�N�X���x
    int LU_METHOD = 0;	        // LU����@
    int ILUCG_METHOD = 1;	    // �s���SLU�����������z�@
    int method = LU_METHOD;	    // ��������@

    double *matrix;		    // �s�� double�^�̃|�C���^��Ŕz��Ƃ��ėp����
    double *matrix2;		// ��Q�s�� double�^�̃|�C���^��Ŕz��Ƃ��ėp����
    double *vector;		    // �x�N�g�� double�^�̃|�C���^��Ŕz��Ƃ��ėp����

    int dof = 0;			// ���f�����R�x

    void clear();
    void createStiffnessMatrix();
};

// �f�[�^����������
void Solver::clear() {
  delete[] matrix;
  delete[] matrix2;
  delete[] vector;
  dof = 0;
}

// �����}�g���b�N�X�E�׏d�x�N�g�����쐬����
void Solver::createStiffnessMatrix(bc, bcList){
    bc = bc;
    bcList = bcList;
    reducedList = [];
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


*/
