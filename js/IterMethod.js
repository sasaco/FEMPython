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