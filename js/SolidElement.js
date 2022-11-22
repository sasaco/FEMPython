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
