var cls=[];					// コンター図色
var CBAR_FONT_SIZE=14;				// フォントサイズ
var CBAR_FONT=CBAR_FONT_SIZE+"px 'Arial'";	// フォント
var FG_COLOR='#ffffff';				// 前景色

//--------------------------------------------------------------------//
// カラーバー
// canvasId - 表示領域ID
var ColorBar=function(canvasId){
  this.canvas=document.getElementById(canvasId);	// 描画キャンバス
  this.canvas.width=this.canvas.clientWidth;
  this.canvas.height=this.canvas.clientHeight;
  this.context=this.canvas.getContext('2d');		// コンテキスト
  this.context.font=CBAR_FONT;
  this.context.textBaseline='top';
  this.rect={x:parseInt(this.canvas.width/5),		// カラーバーの描画領域
      	     y:parseInt(this.canvas.height/10),
      	     width:parseInt(this.canvas.width*3/10),
      	     height:parseInt(this.canvas.height*4/5)};
  this.gradient=this.context.createLinearGradient
    (this.rect.x,this.rect.y+this.rect.height,this.rect.x,this.rect.y);
  this.gradient.addColorStop(0,'#0000ff');
  this.gradient.addColorStop(0.25,'#00ffff');
  this.gradient.addColorStop(0.5,'#00cc00');
  this.gradient.addColorStop(0.75,'#ffff00');
  this.gradient.addColorStop(1,'#ff0000');
  var xt=this.rect.x+this.rect.width+5;
// 数値表示位置
  this.maxPos={x:xt,y:parseInt(this.rect.y-CBAR_FONT_SIZE/2)};
  this.minPos={x:xt,y:this.maxPos.y+this.rect.height};
};

// コンター図を描画する
// minValue - コンター図データ最小値
// maxValue - コンター図データ最大値
ColorBar.prototype.draw=function(minValue,maxValue){
  this.context.clearRect(0,0,this.canvas.width,this.canvas.height);
  this.context.fillStyle=this.gradient;
  this.context.fillRect(this.rect.x,this.rect.y,
      	      	      	this.rect.width,this.rect.height);
  this.context.strokeStyle=FG_COLOR;
  this.context.strokeRect(this.rect.x,this.rect.y,
      	      	      	  this.rect.width,this.rect.height);
  this.context.fillStyle=FG_COLOR;
  this.context.fillText(numString(maxValue),this.maxPos.x,this.maxPos.y);
  this.context.fillText(numString(minValue),this.minPos.x,this.minPos.y);
};

// コンター図を消去する
ColorBar.prototype.clear=function(){
  this.context.clearRect(0,0,this.canvas.width,this.canvas.height);
};

// コンター図の色を返す
// z - 0～1の値
function contourColor(z){
  cls[0]=0;
  cls[1]=0;
  cls[2]=0;
  if(z<=0){
    cls[2]=1;
  }
  else if(z<0.25){
    cls[1]=4*z;
    cls[2]=1;
  }
  else if(z<0.5){
    cls[1]=1.2-0.8*z;
    cls[2]=2-4*z;
  }
  else if(z<0.75){
    cls[0]=4*z-2;
    cls[1]=0.4+0.8*z;
  }
  else if(z<1){
    cls[0]=1;
    cls[1]=4-4*z;
  }
  else{
    cls[0]=1;
  }
  return cls;
}
