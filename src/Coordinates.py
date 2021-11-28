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
