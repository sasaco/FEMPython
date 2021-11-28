var KS_RECT=5/6;			// 矩形断面のせん断補正係数
var KS_CIRCLE=6/7;			// 円形断面のせん断補正係数

//--------------------------------------------------------------------//
// 材料
// label - 材料番号
// ee - ヤング率 (縦弾性係数) 
// nu - ポアソン比
// dens - 密度
// hCon - 熱伝導率
// sHeat - 比熱
var Material=function(label,ee,nu,dens,hCon,sHeat){
  this.label=label;
  this.ee=ee;
  this.nu=nu;
  this.dens=dens;
  this.hCon=hCon;
  this.sHeat=sHeat;
  this.gg=0.5*ee/(1+nu);	// 横弾性係数
  this.cv=dens*sHeat;		// 体積比熱
  this.matrix=null;		// 応力 - 歪マトリックス
};

// 平面応力問題の応力 - 歪マトリックスを作成する
Material.prototype.matrix2Dstress=function(){
  var coef=this.ee/(1-this.nu*this.nu);
  return [[coef,coef*this.nu,0],[coef*this.nu,coef,0],[0,0,this.gg]];
};

// 平面歪問題の応力 - 歪マトリックスを作成する
Material.prototype.matrix2Dstrain=function(){
  var coef=this.ee/((1+this.nu)*(1-2*this.nu));
  return [[coef*(1-this.nu),coef*this.nu,0],
      	  [coef*this.nu,coef*(1-this.nu),0],[0,0,this.gg]];
};

// 軸対称問題の応力 - 歪マトリックスを作成する
Material.prototype.matrixAxiSymetric=function(){
  var coef=this.ee/((1+this.nu)*(1-2*this.nu));
  var s1=coef*(1-this.nu),s2=coef*this.nu;
  return [[s1,s2,s2,0],[s2,s1,s2,0],[s2,s2,s1,0],[0,0,0,this.gg]];
};

// 捩り問題の応力 - 歪マトリックスを作成する
Material.prototype.matrixTorsion=function(){
  return [[this.gg,0],[0,this.gg]];
};

// 3次元問題の応力 - 歪マトリックスを作成する
Material.prototype.matrix3D=function(){
  var coef=this.ee/((1+this.nu)*(1-2*this.nu));
  var s1=coef*(1-this.nu),s2=coef*this.nu;
  return [[s1,s2,s2,0,0,0],[s2,s1,s2,0,0,0],[s2,s2,s1,0,0,0],
      	  [0,0,0,this.gg,0,0],[0,0,0,0,this.gg,0],[0,0,0,0,0,this.gg]];
};

// シェル要素の応力 - 歪マトリックスを作成する
Material.prototype.matrixShell=function(){
  var coef=this.ee/(1-this.nu*this.nu);
  var s2=coef*this.nu;
  return [[coef,s2,0,0,0],[s2,coef,0,0,0],[0,0,this.gg,0,0],
      	  [0,0,0,KS_RECT*this.gg,0],[0,0,0,0,KS_RECT*this.gg]];
};

// 材料を表す文字列を返す
Material.prototype.toString=function(){
  return 'Material\t'+this.label.toString(10)+'\t'+
      	 this.ee+'\t'+this.nu+'\t'+this.gg+'\t'+this.dens+'\t'+
      	 this.hCon+'\t'+this.sHeat;
};

//--------------------------------------------------------------------//
// シェルパラメータ
// label - パラメータ番号
// thickness - 厚さ
var ShellParameter=function(label,thickness){
  this.label=label;
  this.thickness=thickness;
};

// シェルパラメータを表す文字列を返す
ShellParameter.prototype.toString=function(){
  return 'ShellParameter\t'+this.label.toString(10)+'\t'+this.thickness;
};

//--------------------------------------------------------------------//
// 梁パラメータ（円形断面）
// label - パラメータ番号
// type - 断面種類
// ss - データ文字列
var BarParameter=function(label,type,ss){
  this.label=label;
  this.type=type;
  var tp=type.toLowerCase();
  if(tp=='circle'){
    this.section=new CircleSection(ss);
  }
  else if(tp=='rectangle'){
    this.section=new RectSection(ss);
  }
};

// 梁パラメータを表す文字列を返す
BarParameter.prototype.toString=function(){
  return 'BarParameter\t'+this.label.toString(10)+'\t'+this.type+'\t'+
      	 this.section.toString();
};
