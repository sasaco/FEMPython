var cls=[];					// �R���^�[�}�F
var CBAR_FONT_SIZE=14;				// �t�H���g�T�C�Y
var CBAR_FONT=CBAR_FONT_SIZE+"px 'Arial'";	// �t�H���g
var FG_COLOR='#ffffff';				// �O�i�F

//--------------------------------------------------------------------//
// �J���[�o�[
// canvasId - �\���̈�ID
var ColorBar=function(canvasId){
  this.canvas=document.getElementById(canvasId);	// �`��L�����o�X
  this.canvas.width=this.canvas.clientWidth;
  this.canvas.height=this.canvas.clientHeight;
  this.context=this.canvas.getContext('2d');		// �R���e�L�X�g
  this.context.font=CBAR_FONT;
  this.context.textBaseline='top';
  this.rect={x:parseInt(this.canvas.width/5),		// �J���[�o�[�̕`��̈�
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
// ���l�\���ʒu
  this.maxPos={x:xt,y:parseInt(this.rect.y-CBAR_FONT_SIZE/2)};
  this.minPos={x:xt,y:this.maxPos.y+this.rect.height};
};

// �R���^�[�}��`�悷��
// minValue - �R���^�[�}�f�[�^�ŏ��l
// maxValue - �R���^�[�}�f�[�^�ő�l
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

// �R���^�[�}����������
ColorBar.prototype.clear=function(){
  this.context.clearRect(0,0,this.canvas.width,this.canvas.height);
};

// �R���^�[�}�̐F��Ԃ�
// z - 0�`1�̒l
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
