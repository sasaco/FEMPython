// データ型
var NONE=-1;		// 空データ
var DISPLACEMENT=0;	// 変位
var STRAIN=1;		// 歪
var STRESS=2;		// 応力
var S_ENERGY=3;		// 歪エネルギー密度
var TEMPERATURE=4;	// 温度
// 成分
var X=0;		// x成分
var Y=1;		// y成分
var Z=2;		// z成分
var RX=3;		// x軸回転成分
var RY=4;		// y軸回転成分
var RZ=5;		// z軸回転成分
var XY=3;		// xyせん断成分
var YZ=4;		// yzせん断成分
var ZX=5;		// zxせん断成分
var MAGNITUDE=6;	// 大きさ
var MAX_PRINCIPAL=7;	// 最大主成分
var MIN_PRINCIPAL=8;	// 最小主成分
var MID_PRINCIPAL=9;	// 中間主成分
var MAX_SHARE=10;	// 最大せん断成分
var VON_MISES=11;	// ミーゼス応力
var SHIFT=12;		// 成分シフト量
// 変位の成分
var DISP_COMPONENT=['Mag.','x','y','z'];
var DISP2_COMPONENT=['Mag.','x','y','z','rotx','roty','rotz'];
// 歪の成分
var STRAIN_COMPONENT=['Max.prin.','Min.prin.','Mid.prin.',
      	      	      'Max.share',
      	      	      'x','y','z','xy','yz','zx'];
// 応力の成分
var STRESS_COMPONENT=['Max.prin.','Min.prin.','Mid.prin.',
      	      	      'Max.share','Von mises',
      	      	      'x','y','z','xy','yz','zx'];
// 歪エネルギー密度の成分
var ENERGY_COMPONENT=['Energy'];
var COMP_MAP={'Mag.':MAGNITUDE,'x':X,'y':Y,'z':Z,
      	      'rotx':RX,'roty':RY,'rotz':RZ,'xy':XY,'yz':YZ,'zx':ZX,
      	      'Max.prin.':MAX_PRINCIPAL,'Min.prin.':MIN_PRINCIPAL,
      	      'Mid.prin.':MID_PRINCIPAL,'Max.share':MAX_SHARE,
      	      'Von mises':VON_MISES,'Energy':0,
      	      'x 1':X,'y 1':Y,'z 1':Z,'xy 1':XY,'yz 1':YZ,'zx 1':ZX,
      	      'Max.prin. 1':MAX_PRINCIPAL,'Min.prin. 1':MIN_PRINCIPAL,
      	      'Mid.prin. 1':MID_PRINCIPAL,'Max.share 1':MAX_SHARE,
      	      'Von mises 1':VON_MISES,'Energy 1':0,
      	      'x 2':X+SHIFT,'y 2':Y+SHIFT,'z 2':Z+SHIFT,
      	      'xy 2':XY+SHIFT,'yz 2':YZ+SHIFT,'zx 2':ZX+SHIFT,
      	      'Max.prin. 2':MAX_PRINCIPAL+SHIFT,
      	      'Min.prin. 2':MIN_PRINCIPAL+SHIFT,
      	      'Mid.prin. 2':MID_PRINCIPAL+SHIFT,
      	      'Max.share 2':MAX_SHARE+SHIFT,
      	      'Von mises 2':VON_MISES+SHIFT,'Energy 2':1};
var EIG_EPS=1e-10;		// 固有値計算の収束閾値
var NODE_DATA=0;		// 節点データ
var ELEMENT_DATA=1;		// 要素データ
var VIBRATION='Vibration';	// 振動解析
var BUCKLING='Buckling';	// 座屈解析

//--------------------------------------------------------------------//
// 計算結果
var Result=function(){
  this.displacement=[];		// 変位
  this.dispMax=0;		// 変位の大きさの最大値
  this.angleMax=0;		// 回転角の大きさの最大値
  this.strain1=[];		// 節点歪
  this.stress1=[];		// 節点応力
  this.sEnergy1=[];		// 節点歪エネルギー密度
  this.strain2=[];
  this.stress2=[];
  this.sEnergy2=[];
  this.temperature=[];		// 節点温度
  this.tempMax=0;		// 温度の最大値
  this.eigenValue=[];		// 固有値データ
  this.calculated=false;	// 計算前＝計算結果無し
  this.type=NODE_DATA;		// データ保持形態：節点データ
  this.value=[];		// コンター図データ
  this.minValue=0;		// コンター図データ最小値
  this.maxValue=0;		// コンター図データ最大値
};

// 計算結果を消去する
Result.prototype.clear=function(){
  this.displacement.length=0;
  this.strain1.length=0;
  this.strain2.length=0;
  this.stress1.length=0;
  this.stress2.length=0;
  this.sEnergy1.length=0;
  this.sEnergy2.length=0;
  this.temperature.length=0;
  this.dispMax=0;
  this.angleMax=0;
  this.tempMax=0;
  this.eigenValue.length=0;
  this.calculated=false;
  this.value.length=0;
  this.minValue=0;
  this.maxValue=0;
};

// 節点変位を設定する
// bc - 境界条件
// disp - 節点変位を表すベクトル
// nodeCount - 節点数
Result.prototype.setDisplacement=function(bc,disp,nodeCount){
  this.displacement.length=0;
  this.dispMax=0;
  this.angleMax=0;
  var rests=bc.restraints,ii=0;
  for(var i=0;i<nodeCount;i++){
    var v=new Vector3R(),i0=bc.nodeIndex[i],bcDof=bc.dof[i],r=-1,x=v.x;
    for(var j=0;j<bcDof;j++){
      var bcl=bc.bcList[i0+j];
      if(bcl<0){
      	x[j]=disp[ii];
      	ii++;
      }
      else{
      	r=parseInt(bcl/6);
      	x[j]=rests[r].x[j];
      }
    }
    if((r>=0) && rests[r].coords){
      v.x=rests[r].coords.toGlobal(x);
    }
    this.dispMax=Math.max(this.dispMax,v.magnitude());
    this.angleMax=Math.max(this.angleMax,v.magnitudeR());
    this.displacement.push(v);
  }
  this.calculated=true;
};

// 節点温度を設定する
// bc - 境界条件
// t - 節点温度を表すベクトル
// nodeCount - 節点数
Result.prototype.setTemperature=function(bc,t,nodeCount){
  this.temperature.length=0;
  var temp=bc.temperature,ii=0;
  for(var i=0;i<nodeCount;i++){
    var tt;
    if(bc.bcList[i]<0){
      tt=t[ii];
      ii++;
    }
    else{
      tt=temp[bc.bcList[i]].t;
    }
    this.tempMax=Math.max(this.tempMax,tt);
    this.temperature.push(tt);
  }
  this.calculated=true;
};

// 節点の構造解析結果に値を加える
// i - 節点のインデックス
// eps1,str1,se1,eps2,str2,se2 - 表面・裏面の歪，応力，歪エネルギー密度
Result.prototype.addStructureData=function(i,eps1,str1,se1,
      	      	      	      	      	   eps2,str2,se2){
  this.strain1[i].add(eps1);
  this.stress1[i].add(str1);
  this.sEnergy1[i]+=se1;
  this.strain2[i].add(eps2);
  this.stress2[i].add(str2);
  this.sEnergy2[i]+=se2;
};

// 節点の構造解析結果に値を掛ける
// i - 節点のインデックス
// coef - 計算結果に掛ける係数
Result.prototype.mulStructureData=function(i,coef){
  this.strain1[i].mul(coef);
  this.stress1[i].mul(coef);
  this.sEnergy1[i]*=coef;
  this.strain2[i].mul(coef);
  this.stress2[i].mul(coef);
  this.sEnergy2[i]*=coef;
};

// 固有値データを追加する
// ev - 固有値
Result.prototype.addEigenValue=function(ev){
  this.eigenValue.push(ev);
  this.calculated=true;
};

// コンター図データを設定する
// param - データの種類
// component - データの成分
// data - コンター図参照元
Result.prototype.setContour=function(param,component,data){
  if(param<0) return;
  data=data||this;
  var dpara=[data.displacement,data.strain1,data.stress1,data.sEnergy1,
      	     data.temperature];
  var count=dpara[param].length;
  if(count===0) return;
  this.value.length=0;
  this.value[0]=data.getData(param,component,0);
  this.minValue=this.value[0];
  this.maxValue=this.value[0];
  for(var i=1;i<count;i++){
    this.value[i]=data.getData(param,component,i);
    this.minValue=Math.min(this.minValue,this.value[i]);
    this.maxValue=Math.max(this.maxValue,this.value[i]);
  }
};

// データを取り出す
// param - データの種類
// component - データの成分
// index - 節点のインデックス
Result.prototype.getData=function(param,component,index){
  switch(param){
    case DISPLACEMENT:
      switch(component){
      	case X:
      	case Y:
      	case Z:
      	case RX:
      	case RY:
      	case RZ:
      	  return this.displacement[index].x[component];
      	case MAGNITUDE:
      	  return this.displacement[index].magnitude();
      }
      break;
    case STRAIN:
      if(component<SHIFT){
      	return this.getTensorComp(this.strain1[index],component);
      }
      else{
      	return this.getTensorComp(this.strain2[index],component-SHIFT);
      }
      break;
    case STRESS:
      if(component<SHIFT){
      	return this.getTensorComp(this.stress1[index],component);
      }
      else{
      	return this.getTensorComp(this.stress2[index],component-SHIFT);
      }
      break;
    case S_ENERGY:
      if(component===0){
      	return this.sEnergy1[index];
      }
      else{
      	return this.sEnergy2[index];
      }
      break;
    case TEMPERATURE:
      return this.temperature[index];
  }
  return 0;
};

// 歪・応力を取り出す
// s - 歪 or 応力
// component - データの成分
Result.prototype.getTensorComp=function(s,component){
  if(component<6){
    return s.vector()[component];
  }
  else if(component<=10){
    var pri=s.principal();
    if(component===MAX_PRINCIPAL)      return pri[0];
    else if(component===MIN_PRINCIPAL) return pri[2];
    else if(component===MID_PRINCIPAL) return pri[1];
    else if(component===MAX_SHARE)     return 0.5*(pri[0]-pri[2]);
  }
  else if(component===VON_MISES){
    return s.mises();
  }
  return 0;
};

// 節点歪・応力を初期化する
// count - 節点数
Result.prototype.initStrainAndStress=function(count){
  this.strain1.length=0;
  this.strain2.length=0;
  this.stress1.length=0;
  this.stress2.length=0;
  this.sEnergy1.length=0;
  this.sEnergy2.length=0;
  var zeros=[0,0,0,0,0,0];
  for(var i=0;i<count;i++){
    this.strain1[i]=new Strain(zeros);
    this.strain2[i]=new Strain(zeros);
    this.stress1[i]=new Stress(zeros);
    this.stress2[i]=new Stress(zeros);
    this.sEnergy1[i]=0;
    this.sEnergy2[i]=0;
  }
};

// データ文字列を返す
// nodes - 節点
// elems - 要素
Result.prototype.toStrings=function(nodes,elems){
  var s=[],tuple,i;
  if(this.type===ELEMENT_DATA){
    s.push('ResultType\tElement');
    tuple=elems;
  }
  else{
    s.push('ResultType\tNode');
    tuple=nodes;
  }
  for(i=0;i<this.displacement.length;i++){
    s.push('Displacement\t'+nodes[i].label.toString(10)+'\t'+
      	   this.displacement[i].x.join('\t'));
  }
  for(i=0;i<this.strain1.length;i++){
    s.push('Strain1\t'+tuple[i].label.toString(10)+'\t'+
      	   this.strain1[i].vector().join('\t'));
  }
  for(i=0;i<this.stress1.length;i++){
    s.push('Stress1\t'+tuple[i].label.toString(10)+'\t'+
      	   this.stress1[i].vector().join('\t'));
  }
  for(i=0;i<this.sEnergy1.length;i++){
    s.push('StrEnergy1\t'+tuple[i].label.toString(10)+'\t'+
      	   this.sEnergy1[i]);
  }
  for(i=0;i<this.strain2.length;i++){
    s.push('Strain2\t'+tuple[i].label.toString(10)+'\t'+
      	   this.strain2[i].vector().join('\t'));
  }
  for(i=0;i<this.stress2.length;i++){
    s.push('Stress2\t'+tuple[i].label.toString(10)+'\t'+
      	   this.stress2[i].vector().join('\t'));
  }
  for(i=0;i<this.sEnergy2.length;i++){
    s.push('StrEnergy2\t'+tuple[i].label.toString(10)+'\t'+
      	   this.sEnergy2[i]);
  }
  for(i=0;i<this.temperature.length;i++){
    s.push('Temp\t'+nodes[i].label.toString(10)+'\t'+
      	   this.temperature[i]);
  }
  for(i=0;i<this.eigenValue.length;i++){
    Array.prototype.push.apply
      (s,this.eigenValue[i].toStrings(nodes,tuple));
  }
  return s;
};

//--------------------------------------------------------------------//
// 固有値
// value - 固有値・固有振動数
// type - 解析種類
var EigenValue=function(value,type){
  this.value=value;
  this.type=type;
  this.displacement=[];		// 変位
  this.sEnergy1=[];		// 節点歪エネルギー密度
  this.sEnergy2=[];
  this.dispMax=0;
  this.angleMax=0;
};

// 変位を設定する
// bc - 境界条件
// disp - 変位を表す固有ベクトル
// nodeCount - 節点数
EigenValue.prototype.setDisplacement=function(bc,disp,nodeCount){
  this.displacement.length=0;
  this.dispMax=0;
  this.angleMax=0;
  var rests=bc.restraints,ii=0;
  for(var i=0;i<nodeCount;i++){
    var v=new Vector3R(),i0=bc.nodeIndex[i],bcDof=bc.dof[i],r=-1,x=v.x;
    for(var j=0;j<bcDof;j++){
      var bcl=bc.bcList[i0+j];
      if(bcl<0){
      	x[j]=disp[ii];
      	ii++;
      }
      else{
      	r=parseInt(bcl/6);
      }
    }
    if((r>=0) && rests[r].coords){
      v.x=rests[r].coords.toGlobal(x);
    }
    this.dispMax=Math.max(this.dispMax,v.magnitude());
    this.angleMax=Math.max(this.angleMax,v.magnitudeR());
    this.displacement.push(v);
  }
};

// データを取り出す
// param - データの種類
// component - データの成分
// index - 節点のインデックス
EigenValue.prototype.getData=function(param,component,index){
  switch(param){
    case DISPLACEMENT:
      switch(component){
      	case X:
      	case Y:
      	case Z:
      	case RX:
      	case RY:
      	case RZ:
      	  return this.displacement[index].x[component];
      	case MAGNITUDE:
      	  return this.displacement[index].magnitude();
      }
      break;
    case S_ENERGY:
      if(component===0){
      	return this.sEnergy1[index];
      }
      else{
      	return this.sEnergy2[index];
      }
      break;
  }
  return 0;
};

// 節点歪・応力を初期化する
// count - 節点数
EigenValue.prototype.initStrainEnergy=function(count){
  this.sEnergy1.length=0;
  this.sEnergy2.length=0;
  for(var i=0;i<count;i++){
    this.sEnergy1[i]=0;
    this.sEnergy2[i]=0;
  }
};

// データ文字列を返す
// nodes - 節点
// tuple - 節点or要素
EigenValue.prototype.toStrings=function(nodes,tuple){
  var s=[],i;
  s.push('EigenValue\t'+this.type+'\t'+this.value);
  for(i=0;i<this.displacement.length;i++){
    s.push('Displacement\t'+nodes[i].label.toString(10)+'\t'+
      	   this.displacement[i].x.join('\t'));
  }
  for(i=0;i<this.sEnergy1.length;i++){
    s.push('StrEnergy1\t'+tuple[i].label.toString(10)+'\t'+
      	   this.sEnergy1[i]);
  }
  for(i=0;i<this.sEnergy2.length;i++){
    s.push('StrEnergy2\t'+tuple[i].label.toString(10)+'\t'+
      	   this.sEnergy2[i]);
  }
  return s;
};

//--------------------------------------------------------------------//
// ３次元対称テンソル
// s - 成分
var SymmetricTensor3=function(s){
  this.xx=s[0];
  this.yy=s[1];
  this.zz=s[2];
  this.xy=s[3];
  this.yz=s[4];
  this.zx=s[5];
};

// テンソルをベクトルとして返す
SymmetricTensor3.prototype.vector=function(){
  return [this.xx,this.yy,this.zz,this.xy,this.yz,this.zx];
};

// テンソルを加える
// t - 加えるテンソル
SymmetricTensor3.prototype.add=function(t){
  this.xx+=t.xx;
  this.yy+=t.yy;
  this.zz+=t.zz;
  this.xy+=t.xy;
  this.yz+=t.yz;
  this.zx+=t.zx;
};

// 成分にスカラーを掛ける
// a - 掛けるスカラー
SymmetricTensor3.prototype.mul=function(a){
  this.xx*=a;
  this.yy*=a;
  this.zz*=a;
  this.xy*=a;
  this.yz*=a;
  this.zx*=a;
};

// 固有値を返す
SymmetricTensor3.prototype.principal=function(){
  return eigenvalue(this,100).lambda;
};

// テンソルを回転させる
// d - 方向余弦マトリックス
SymmetricTensor3.prototype.rotate=function(d){
  var mat=[[this.xx,this.xy,this.zx],[this.xy,this.yy,this.yz],
      	   [this.zx,this.yz,this.zz]];
  var s=[0,0,0,0,0,0];
  for(var i=0;i<3;i++){
    for(var j=0;j<3;j++){
      var mij=mat[i][j];
      for(var k=0;k<3;k++){
      	s[k]+=d[k][i]*d[k][j]*mij;
      	s[k+3]+=d[k][i]*d[(k+1)%3][j]*mij;
      }
    }
  }
  this.xx=s[0];
  this.yy=s[1];
  this.zz=s[2];
  this.xy=s[3];
  this.yz=s[4];
  this.zx=s[5];
};

// テンソルの内積を計算する
// t - 相手のテンソル
SymmetricTensor3.prototype.innerProduct=function(t){
  return this.xx*t.xx+this.yy*t.yy+this.zz*t.zz+
      	 2*(this.xy*t.xy+this.yz*t.yz+this.zx*t.zx);
};

// Jacobie法で対称テンソルの固有値を求める
// Numeric.jsでは対角要素が0（例えばせん断のみの条件）だと求められない
// st - 対称テンソル
// iterMax - 反復回数の最大値
function eigenvalue(st,iterMax){
  var m=[[st.xx,st.xy,st.zx],[st.xy,st.yy,st.yz],
      	 [st.zx,st.yz,st.zz]];
  return eigenByJacob(m,iterMax);
}

// Jacobie法で対称テンソルの固有値を求める
// m - 対称行列
// iterMax - 反復回数の最大値
function eigenByJacob(m,iterMax){
  var size=m.length,abs=Math.abs,i,j,iter,dataMax=0;
  var ev=numeric.identity(size);
  for(i=0;i<size;i++){
    for(j=i;j<size;j++){
      dataMax=Math.max(dataMax,abs(m[i][j]));
    }
  }
  var tolerance=EIG_EPS*dataMax;
// 値が0の場合
  if(dataMax===0) return {lambda:numeric.getDiag(m),ev:ev};
  for(iter=0;iter<iterMax;iter++){
    var im=0,jm=0,ndMax=0;
    for(i=0;i<2;i++){
      for(j=i+1;j<3;j++){
      	var absm=abs(m[i][j]);
      	if(absm>ndMax){
      	  ndMax=absm;
      	  im=i;
      	  jm=j;
      	}
      }
    }
    if(ndMax<tolerance) break;
    var mim=m[im],mjm=m[jm];
    var alpha=0.5*(mim[im]-mjm[jm]);
    var beta=0.5/Math.sqrt(alpha*alpha+ndMax*ndMax);
    var cc2=0.5+abs(alpha)*beta,cs=-beta*mim[jm];
    if(alpha<0) cs=-cs;
    var cc=Math.sqrt(cc2),ss=cs/cc;
    var aij=2*(alpha*cc2-mim[jm]*cs),aii=mjm[jm]+aij,ajj=mim[im]-aij;
    for(i=0;i<3;i++){
      var mi=m[i],evi=ev[i];
      var a1=mi[im]*cc-mi[jm]*ss;
      var a2=mi[im]*ss+mi[jm]*cc;
      mi[im]=a1;
      mi[jm]=a2;
      mim[i]=a1;
      mjm[i]=a2;
      a1=evi[im]*cc-evi[jm]*ss;
      a2=evi[im]*ss+evi[jm]*cc;
      evi[im]=a1;
      evi[jm]=a2;
    }
    mim[im]=aii;
    mim[jm]=0;
    mjm[im]=0;
    mjm[jm]=ajj;
  }
  m=numeric.getDiag(m);
// 固有値を大きい順に入れ替える
  var eig=[];
  ev=numeric.transpose(ev);
  for(i=0;i<size;i++) eig.push([m[i],ev[i]]);
  eig.sort(function(v1,v2){return v2[0]-v1[0];});
  for(i=0;i<size;i++){
    m[i]=eig[i][0];
    ev[i]=eig[i][1];
  }
  return {lambda:m,ev:numeric.transpose(ev)};
}

//--------------------------------------------------------------------//
// 歪
// s - 成分
var Strain=function(s){
  SymmetricTensor3.call(this,s);
  this.xy=0.5*s[3];
  this.yz=0.5*s[4];
  this.zx=0.5*s[5];
};

// テンソルをベクトルとして返す
Strain.prototype.vector=function(){
  return [this.xx,this.yy,this.zz,2*this.xy,2*this.yz,2*this.zx];
};

//--------------------------------------------------------------------//
// 応力
// s - 成分
var Stress=function(s){
  SymmetricTensor3.call(this,s);
};

// ミーゼス応力を返す
Stress.prototype.mises=function(){
  var dxy=this.xx-this.yy,dyz=this.yy-this.zz,dzx=this.zz-this.xx;
  var ss=dxy*dxy+dyz*dyz+dzx*dzx;
  var tt=this.xy*this.xy+this.yz*this.yz+this.zx*this.zx;
  return Math.sqrt(0.5*ss+3*tt);
};

//--------------------------------------------------------------------//
// 結果表示設定
var ResultView=function(){
  this.dispCoef=document.getElementById('dispcoef');	// 変形表示倍率
  this.eigen=document.getElementById('eigenvalue');	// 固有値データ
  this.contour=document.getElementById('contour');	// コンター図表示データ
  this.component=document.getElementById('component');	// コンター図表示成分
};

// 静解析の設定を初期化する
ResultView.prototype.setInitStatic=function(){
  removeOptions(this.eigen);
  this.setContourSelect();
  this.setConfig();
};

// 固有値解析の設定を初期化する
ResultView.prototype.setInitEigen=function(){
  removeOptions(this.eigen);
  var eigenValue=model.result.eigenValue;
  for(var i=0;i<eigenValue.length;i++){
    this.eigen.appendChild(createOption('固有値'+(i+1),i));
  }
  removeOptions(this.contour);
  this.contour.appendChild(createOption('コンター無し',NONE));
  this.contour.appendChild(createOption('変位',DISPLACEMENT));
  this.contour.appendChild(createOption('歪エネルギー密度',S_ENERGY));
  this.setResComp();
  this.setConfig();
};

// 表示するコンター図データを設定する
ResultView.prototype.setContourSelect=function(){
  removeOptions(this.eigen);
  removeOptions(this.contour);
  this.contour.appendChild(createOption('コンター無し',NONE));
  if(model.result.displacement.length>0){
    this.contour.appendChild(createOption('変位',DISPLACEMENT));
  }
  if(model.result.strain1.length>0){
    this.contour.appendChild(createOption('歪',STRAIN));
  }
  if(model.result.stress1.length>0){
    this.contour.appendChild(createOption('応力',STRESS));
  }
  if(model.result.sEnergy1.length>0){
    this.contour.appendChild(createOption('歪エネルギー密度',S_ENERGY));
  }
  if(model.result.temperature.length>0){
    this.contour.appendChild(createOption('温度',TEMPERATURE));
  }
  removeOptions(this.component);
};

// 表示成分を設定する
ResultView.prototype.setResComp=function(){
  if(!model.result.calculated) return;
  removeOptions(this.component);
  switch(parseInt(this.contour.value)){
    case DISPLACEMENT:
      if(model.hasShellBar){
      	setOptions(this.component,DISP2_COMPONENT,-1);
      }
      else{
      	setOptions(this.component,DISP_COMPONENT,-1);
      }
      break;
    case STRAIN:
      if(model.hasShellBar){
      	setOptions(this.component,STRAIN_COMPONENT,1);
      	setOptions(this.component,STRAIN_COMPONENT,2);
      }
      else{
      	setOptions(this.component,STRAIN_COMPONENT,-1);
      }
      break;
    case STRESS:
      if(model.hasShellBar){
      	setOptions(this.component,STRESS_COMPONENT,1);
      	setOptions(this.component,STRESS_COMPONENT,2);
      }
      else{
      	setOptions(this.component,STRESS_COMPONENT,-1);
      }
      break;
    case S_ENERGY:
      if(model.hasShellBar){
      	setOptions(this.component,ENERGY_COMPONENT,1);
      	setOptions(this.component,ENERGY_COMPONENT,2);
      }
      else{
      	setOptions(this.component,ENERGY_COMPONENT,-1);
      }
      break;
  }
};

// 設定を表示に反映させる
ResultView.prototype.setConfig=function(){
  var eigen=parseInt(this.eigen.value);
  var dcoef=parseFloat(this.dispCoef.value);
  var param=parseInt(this.contour.value);
  var coef,comp,minValue,maxValue;
  if(isFinite(eigen)){
    var eigenValue=model.result.eigenValue[eigen];
    coef=dcoef*Math.min(bounds.size/eigenValue.dispMax,
      	      	      	1/eigenValue.angleMax);
    viewObj.setDisplacement(eigenValue.displacement,coef);
    showEigenValue(eigen,eigenValue.type,eigenValue.value);
    if(param<0){
      viewObj.clearContour();
      colorBar.clear();
    }
    else{
      comp=parseInt(this.component.value);
      model.result.setContour(param,comp,eigenValue);
      minValue=model.result.minValue;
      maxValue=model.result.maxValue;
      switch(param){
      	case DISPLACEMENT:
      	case TEMPERATURE:
      	  viewObj.setContour(model.result.value,minValue,maxValue);
      	  break;
      	default:
      	  viewObj.setContour(model.result.value,minValue,maxValue,
      	      	      	     model.result.type);
      	  break;
      }
      colorBar.draw(minValue,maxValue);
    }
  }
  else{
    coef=dcoef*Math.min(bounds.size/model.result.dispMax,
      	      	      	1/model.result.angleMax);
    viewObj.setDisplacement(model.result.displacement,coef);
    if(param<0){
      viewObj.clearContour();
      colorBar.clear();
    }
    else{
      comp=parseInt(this.component.value);
      model.result.setContour(param,comp);
      minValue=model.result.minValue;
      maxValue=model.result.maxValue;
      switch(param){
      	case DISPLACEMENT:
      	case TEMPERATURE:
      	  viewObj.setContour(model.result.value,minValue,maxValue);
      	  break;
      	default:
      	  viewObj.setContour(model.result.value,minValue,maxValue,
      	      	      	     model.result.type);
      	  break;
      }
      colorBar.draw(minValue,maxValue);
    }
  }
};

// 設定をバックアップする
ResultView.prototype.stock=function(){
  this.coef0=this.dispCoef.value;
  this.contour0=[];
  this.comp0=[];
  var i;
  for(i=0;i<this.contour.childNodes.length;i++){
    this.contour0[i]=this.contour.childNodes[i];
  }
  this.contIndex=this.contour.selectedIndex;
  for(i=0;i<this.component.childNodes.length;i++){
    this.comp0[i]=this.component.childNodes[i];
  }
  this.compIndex=this.component.selectedIndex;
};

// 設定を元に戻す
ResultView.prototype.reset=function(){
  this.dispCoef.value=this.coef0;
  removeOptions(this.contour);
  removeOptions(this.component);
  var i;
  for(i=0;i<this.contour0.length;i++){
    this.contour.appendChild(this.contour0[i]);
  }
  this.contour.selectedIndex=this.contIndex;
  for(i=0;i<this.comp0.length;i++){
    this.component.appendChild(this.comp0[i]);
  }
  this.component.selectedIndex=this.compIndex;
};

// 計算結果の成分を表示する
// sel - コンボボックス
// component - 成分
// data - データ番号（1:表面,2:裏面,-1:番号なし）
function setOptions(sel,component,data){
  for(var i=0;i<component.length;i++){
    var c=component[i];
    if(data>0) c+=' '+data;
    sel.appendChild(createOption(c,COMP_MAP[c]));
  }
}

// オプション要素を作成する
// text - オプションのテキスト
// value - オプションの値
function createOption(text,value){
  var opt=document.createElement('option');
  opt.value=value;
  opt.text=text;
  return opt;
}

// コンボボックスのオプションを削除する
// sel - コンボボックス
function removeOptions(sel){
  if(sel.hasChildNodes()){
    while(sel.childNodes.length>0){
      sel.removeChild(sel.firstChild);
    }
  }
}

// 結果表示設定ウィンドウを表示する
function showResultWindow(){
  showModalWindow(RESULT_WINDOW);
  resultView.stock();
}

// 計算結果を消去する
function removeRes(){
  model.result.clear();
  viewObj.removeResult();
  colorBar.clear();
  showInfo();
  resultView.setContourSelect();
  hideModalWindow(RESULT_WINDOW);
}

// 結果表示設定を更新する
function setResultConfig(){
  hideModalWindow(RESULT_WINDOW);
  resultView.setConfig();
}

// 結果表示設定を取り消す
function cancelResultConfig(){
  hideModalWindow(RESULT_WINDOW);
  resultView.reset();
}

inherits(Strain,SymmetricTensor3);
inherits(Stress,SymmetricTensor3);
