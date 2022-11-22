//--------------------------------------------------------------------//
// ランチョス法による固有値計算

var EIG_SHIFT=-0.1;	// シフト量δのデフォルト値

// ランチョス法で一般固有値問題Ax=λBxの３重対角化をする
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
function eigByLanczos(a,b,n){
  var size=a.length,u=[],alp=[],bet=[],bau,al,bt,i;
  n=Math.min(n||size,size);
  var blup=numeric.ccsLUP(numeric.ccsSparse(b));
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  a=toSparse(a);
  b=toSparse(b);
  var ubu=sparseDotVMV(b,u[0]);
  u[0]=numeric.mul(1/Math.sqrt(Math.abs(ubu)),u[0]);
  for(i=0;i<n-1;i++){
    al=sparseDotVMV(a,u[i]);
    alp.push(al);
    bau=numeric.ccsLUPSolve(blup,sparseDotMV(a,u[i]));
    var v=numeric.sub(bau,numeric.mul(al,u[i]));
    if(i>0) v=numeric.sub(v,numeric.mul(bt,u[i-1]));
    for(var j=0;j<=i;j++){
      var uv=numeric.dot(u[j],sparseDotMV(b,v));
      v=numeric.sub(v,numeric.mul(uv,u[j]));
    }
    ubu=sparseDotVMV(b,v);
    bt=Math.sqrt(Math.abs(ubu));
    bet.push(bt);
    u[i+1]=numeric.mul(1/bt,v);
  }
  al=sparseDotVMV(a,u[n-1]);
  alp.push(al);
  var eig=tdmEig(alp,bet);
  return {lambda:eig[0],ut:numeric.dot(eig[1],u)};
}

// ランチョス法で一般固有値問題Ax=λBxの３重対角化をする
// 小さい固有値を優先させるため B(A+δB)^-1Bx=θBx に変換する
// 逆行列はLU分解法
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
// delta - シフト量δ
function eigByLanczosLUP(a,b,n,delta){
  var size=a.length,u=[],alp=[],bet=[],bu,abu,al,bt,i,j;
  n=Math.min(n||size,size);
  if((delta===null) || (delta===undefined)) delta=EIG_SHIFT;
  var ablup=numeric.ccsLUP(numeric.ccsSparse(shiftMatrix(a,b,delta)));
  b=toSparse(b);
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  var ubu=sparseDotVMV(b,u[0]);
  u[0]=numeric.mul(1/Math.sqrt(Math.abs(ubu)),u[0]);
  for(i=0;i<n;i++){
    bu=sparseDotMV(b,u[i]);
    abu=numeric.ccsLUPSolve(ablup,bu);
    al=numeric.dot(bu,abu);
    alp.push(al);
    if(i<n-1){
      var v=numeric.sub(abu,numeric.mul(al,u[i]));
      if(i>0) v=numeric.sub(v,numeric.mul(bt,u[i-1]));
      for(j=0;j<=i;j++){
      	var uv=numeric.dot(u[j],sparseDotMV(b,v));
      	v=numeric.sub(v,numeric.mul(uv,u[j]));
      }
      ubu=sparseDotVMV(b,v);
      bt=Math.sqrt(Math.abs(ubu));
      bet.push(bt);
      u[i+1]=numeric.mul(1/bt,v);
    }
  }
  var eig=tdmEig(alp,bet);
  var e1=eig[0],e2=[];
  for(i=0;i<e1.length;i++) e2[i]=1/e1[i]+delta;
  return {lambda:e2,ut:numeric.dot(eig[1],u)};
}

// ランチョス法で一般固有値問題Ax=λBxの３重対角化をする
// 小さい固有値を優先させるため B(A+δB)^-1Bx=θBx に変換する
// 逆行列はILUCG法
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
// delta - シフト量δ
function eigByLanczosILUCG(a,b,n,delta){
  var size=a.length,u=[],alp=[],bet=[],bu,abu,al,bt,i,j;
  n=Math.min(n||size,size);
  if((delta===null) || (delta===undefined)) delta=EIG_SHIFT;
  var adb=shiftMatrix(a,b,delta);
  var ablu=getILU(adb);
  adb=toSparse(adb);
  b=toSparse(b);
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  var ubu=sparseDotVMV(b,u[0]);
  u[0]=numeric.mul(1/Math.sqrt(Math.abs(ubu)),u[0]);
  for(i=0;i<n;i++){
    bu=sparseDotMV(b,u[i]);
    abu=solveILU(adb,ablu,bu);
    al=numeric.dot(bu,abu);
    alp.push(al);
    if(i<n-1){
      var v=numeric.sub(abu,numeric.mul(al,u[i]));
      if(i>0) v=numeric.sub(v,numeric.mul(bt,u[i-1]));
      for(j=0;j<=i;j++){
      	var uv=numeric.dot(u[j],sparseDotMV(b,v));
      	v=numeric.sub(v,numeric.mul(uv,u[j]));
      }
      ubu=sparseDotVMV(b,v);
      bt=Math.sqrt(Math.abs(ubu));
      bet.push(bt);
      u[i+1]=numeric.mul(1/bt,v);
    }
  }
  var eig=tdmEig(alp,bet),e1=eig[0],e2=[];
  for(i=0;i<e1.length;i++) e2[i]=1/e1[i]+delta;
  return {lambda:e2,ut:numeric.dot(eig[1],u)};
}

// アーノルディ法で一般固有値問題Ax=λBxの３重対角化をする
// 小さい固有値を優先させるため (A+δB)^-1Bx=θx に変換する
// 逆行列はLU分解法
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
// delta - シフト量δ
function eigByArnoldiLUP(a,b,n,delta){
  var size=a.length,u=[],bu,abu,h1,i,j;
  n=Math.min(n||size,size);
  var h=numeric.rep([n,n],0);
  if((delta===null) || (delta===undefined)) delta=EIG_SHIFT;
  var ablup=numeric.ccsLUP(numeric.ccsSparse(shiftMatrix(a,b,delta)));
  b=toSparse(b);
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  var uu=numeric.dotVV(u[0],u[0]);
  u[0]=numeric.mul(1/Math.sqrt(uu),u[0]);
  for(i=0;i<n;i++){
    bu=sparseDotMV(b,u[i]);
    abu=numeric.ccsLUPSolve(ablup,bu);
    var v=abu;
    for(j=0;j<=i;j++){
      h1=numeric.dot(u[j],abu);
      h[j][i]=h1;
      v=numeric.sub(v,numeric.mul(h1,u[j]));
    }
    if(i<n-1){
      for(j=0;j<=i;j++){
      	var uv=numeric.dot(u[j],v);
      	v=numeric.sub(v,numeric.mul(uv,u[j]));
      }
      uu=numeric.dotVV(v,v);
      h1=Math.sqrt(uu);
      h[i+1][i]=h1;
      u[i+1]=numeric.mul(1/h1,v);
    }
  }
  var eig=eigen(h),e1=eig[0],e2=[];
  for(i=0;i<e1.length;i++) e2[i]=1/e1[i]+delta;
  return {lambda:e2,ut:numeric.dot(eig[1],u)};
}

// アーノルディ法で一般固有値問題Ax=λBxの３重対角化をする
// 小さい固有値を優先させるため (A+δB)^-1Bx=θx に変換する
// 逆行列はILUCG法
// a,b - 行列A,B
// n - ３重対角化行列の大きさ
// delta - シフト量δ
function eigByArnoldiILUCG(a,b,n,delta){
  var size=a.length,u=[],bu,abu,h1,i,j;
  n=Math.min(n||size,size);
  var h=numeric.rep([n,n],0);
  if((delta===null) || (delta===undefined)) delta=EIG_SHIFT;
  var adb=shiftMatrix(a,b,delta);
  var ablu=getILU(adb);
  adb=toSparse(adb);
  b=toSparse(b);
  u[0]=[];
  for(i=0;i<size;i++) u[0][i]=a[i][i];
  var uu=numeric.dotVV(u[0],u[0]);
  u[0]=numeric.mul(1/Math.sqrt(uu),u[0]);
  for(i=0;i<n;i++){
    bu=sparseDotMV(b,u[i]);
    abu=solveILU(adb,ablu,bu);
    var v=abu;
    for(j=0;j<=i;j++){
      h1=numeric.dot(u[j],abu);
      h[j][i]=h1;
      v=numeric.sub(v,numeric.mul(h1,u[j]));
    }
    if(i<n-1){
      for(j=0;j<=i;j++){
      	var uv=numeric.dot(u[j],v);
      	v=numeric.sub(v,numeric.mul(uv,u[j]));
      }
      uu=numeric.dotVV(v,v);
      h1=Math.sqrt(uu);
      h[i+1][i]=h1;
      u[i+1]=numeric.mul(1/h1,v);
    }
  }
  var eig=eigen(h),e1=eig[0],e2=[];
  for(i=0;i<e1.length;i++) e2[i]=1/e1[i]+delta;
  return {lambda:e2,ut:numeric.dot(eig[1],u)};
}

// ３重対角化行列の固有値・固有ベクトルを求める
// alp,bet - ３重対角化行列の対角成分,その隣の成分
function tdmEig(alp,bet){
  var size=alp.length,t=numeric.diag(alp);
  for(var i=0;i<size-1;i++){
    t[i][i+1]=bet[i];
    t[i+1][i]=bet[i];
  }
  return eigen(t);
}

// 行列の固有値・固有ベクトルを求める
// m - 行列
function eigen(m){
  var e=[],ev=[],size=m.length,i;
  var eig=numeric.eig(m),q=numeric.transpose(eig.E.x);
  for(i=0;i<size;i++) ev.push([eig.lambda.x[i],q[i]]);
  ev.sort(function(v1,v2){return Math.abs(v2[0])-Math.abs(v1[0]);});
  for(i=0;i<size;i++){
    e[i]=ev[i][0];
    q[i]=ev[i][1];
  }
  return [e,q];
}

// シフト行列A-δBを返す
// a,b - 行列A,B
// delta - シフト量δ
function shiftMatrix(a,b,delta){
  var size=a.length,adb=[],j;
  for(var i=0;i<size;i++){
    var ai=a[i],bi=b[i],s=[];
    for(j in ai){
      if(ai.hasOwnProperty(j)){
      	j=parseInt(j);
      	s[j]=ai[j];
      }
    }
    if(delta!==0){
      for(j in bi){
      	if(bi.hasOwnProperty(j)){
      	  j=parseInt(j);
      	  if(j in s){
      	    s[j]-=delta*bi[j];
      	  }
      	  else{
      	    s[j]=-delta*bi[j];
      	  }
      	}
      }
    }
    adb[i]=s;
  }
  return adb;
}

// 行列とベクトルの積を計算する
// matrix - 疎行列
// x - ベクトル
function sparseDotVMV(matrix,x){
  var row=matrix[0],col=matrix[1],val=matrix[2],m=row.length-1,s=0;
  for(var i=0;i<m;i++){
    var j0=row[i],j1=row[i+1],xi=x[i];
    for(var j=j0;j<j1;j++){
      s+=xi*val[j]*x[col[j]];
    }
  }
  return s;
}
