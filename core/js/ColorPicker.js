var CPICK_FONT_SIZE=14;				// �t�H���g�T�C�Y
var CPICK_FONT=CPICK_FONT_SIZE+"px 'Arial'";	// �t�H���g

//--------------------------------------------------------------------//
// �J���[�s�b�J�[
// canvas - �`��L�����o�X
// div - �~�`�`�敪����
// x,y,width,height - �`��ʒu,��,����
var ColorPicker=function(canvas,div,x,y,width,height){
  this.canvas=canvas;
  this.context=canvas.getContext('2d');	// �R���e�L�X�g
  this.context.font=CPICK_FONT;
  this.context.textBaseline='top';
  this.div=div||360;
  x=x||0;
  y=y||0;
  width=width||canvas.width;
  height=height||canvas.height;
// ���S���W
  var size=Math.min(width,height);
  this.cx=parseInt(x+0.5*size);
  this.cy=parseInt(y+0.5*size);
// �F���~�`�X���C�_�[���a
  this.ro=parseInt(0.48*size);
  this.rc=parseInt(0.9*this.ro);
  this.ri=parseInt(0.8*this.ro);
// �ʓx�P�x�}�b�v�`��ʒu,�T�C�Y
  this.rect=parseInt(1.3*this.ri);
  this.rx=parseInt(this.cx-0.5*this.rect);
  this.ry=parseInt(this.cy-0.5*this.rect);
  this.pt=parseInt(Math.max(0.07*this.ri,1));
  this.hsl=[0,0,0];			// ���݂̐F (HSL�F���)
// �e�L�X�g�ʒu
  if(width>=height){
    this.tx=size+10;
    this.ty=10;
  }
  else{
    this.tx=10;
    this.ty=size+10;
  }
  this.initMouseEvent();
};

// ���݂̐F��ݒ肷��
// rgb - �w�肷��F (RGB)
ColorPicker.prototype.setColor=function(rgb){
  var r=rgb[0],g=rgb[1],b=rgb[2];
  var rgbmax=Math.max(Math.max(r,g),b);
  var rgbmin=Math.min(Math.min(r,g),b);
  this.hsl[2]=0.5*(rgbmax+rgbmin);
  if(rgbmax===rgbmin){
    this.hsl[0]=0;
    this.hsl[1]=0;
  }
  else{
    var del=rgbmax-rgbmin;
    this.hsl[1]=del/(1-Math.abs(2*this.hsl[2]-1));
    if(rgbmin===b)      this.hsl[0]=((g-r)/del+1)/6;
    else if(rgbmin===r) this.hsl[0]=((b-g)/del+3)/6;
    else if(rgbmin===g) this.hsl[0]=((r-b)/del+5)/6;
  }
  this.draw();
};

// ���݂̐F (RGB) ��Ԃ�
ColorPicker.prototype.getColor=function(){
  var h=this.hsl[0],s=this.hsl[1],l=this.hsl[2];
  var rgbmax=l+0.5*s*(1-Math.abs(2*l-1));
  var rgbmin=2*l-rgbmax;
  return [hueToRGB(rgbmax,rgbmin,h+1/3),hueToRGB(rgbmax,rgbmin,h),
      	  hueToRGB(rgbmax,rgbmin,h-1/3)];
};

// �J���[�s�b�J�[��`�悷��
ColorPicker.prototype.draw=function(){
  this.context.clearRect(0,0,this.canvas.width,this.canvas.height);
// �F���X���C�_�[
  var dth=2*Math.PI/this.div,dh=360/this.div;
  var cc0=1,ss0=0,th,h,s,l,i;
  for(i=0;i<this.div;i++){
    th=dth*(i+1);
    var cc1=Math.cos(th),ss1=Math.sin(th);
    h=dh*i;
    this.context.fillStyle='hsl('+h+',100%,50%)';
    this.context.strokeStyle=this.context.fillStyle;
    this.context.beginPath();
    this.context.moveTo(this.cx+this.ri*cc0,this.cy-this.ri*ss0);
    this.context.lineTo(this.cx+this.ro*cc0,this.cy-this.ro*ss0);
    this.context.lineTo(this.cx+this.ro*cc1,this.cy-this.ro*ss1);
    this.context.lineTo(this.cx+this.ri*cc1,this.cy-this.ri*ss1);
    this.context.closePath();
    this.context.fill();
    this.context.stroke();
    cc0=cc1;
    ss0=ss1;
  }
  th=2*Math.PI*this.hsl[0];
  cc0=Math.cos(th);
  ss0=Math.sin(th);
  this.drawPointer(this.cx+this.rc*cc0,this.cy-this.rc*ss0);

// �ʓx�P�x�}�b�v
  h=360*this.hsl[0];
  var dl=100/this.rect;
  for(i=0;i<this.rect;i++){
    var y=this.ry+i;
    l=dl*(this.rect-i);
    var grad=this.context.createLinearGradient
      (this.rx,y,this.rx+this.rect,y);
    grad.addColorStop(0,'hsl('+h+',0%,'+l+'%)');
    grad.addColorStop(1,'hsl('+h+',100%,'+l+'%)');
    this.context.fillStyle=grad;
    this.context.fillRect(this.rx,y,this.rect,1);
  }
  this.drawPointer(parseInt(this.rx+this.rect*this.hsl[1]),
      	      	   parseInt(this.ry+this.rect*(1-this.hsl[2])));

// �e�L�X�g
  this.context.fillStyle='#000000';
  this.context.fillText('H:'+parseInt(h),this.tx,this.ty);
  s=parseInt(100*this.hsl[1]);
  l=parseInt(100*this.hsl[2]);
  if(this.tx<=this.ty){
    var w=parseInt(this.canvas.width/3);
    this.context.fillText('S:'+s+'%',this.tx+w,this.ty);
    this.context.fillText('L:'+l+'%',this.tx+2*w,this.ty);
  }
  else{
    this.context.fillText('S:'+s+'%',this.tx,this.ty+CPICK_FONT_SIZE);
    this.context.fillText('L:'+l+'%',this.tx,this.ty+2*CPICK_FONT_SIZE);
  }
};

// �|�C���^��`�悷��
// x,y - �`��ʒu
ColorPicker.prototype.drawPointer=function(x,y){
  var h=360*this.hsl[0],s=100*this.hsl[1],l=100*this.hsl[2];
  this.context.fillStyle='hsl('+h+','+s+'%,'+l+'%)';
  this.context.beginPath();
  this.context.arc(x,y,this.pt,0,2*Math.PI,false);
  this.context.closePath();
  this.context.fill();
  this.context.strokeStyle='#ffffff';
  this.context.beginPath();
  this.context.arc(x,y,this.pt,0,2*Math.PI,false);
  this.context.closePath();
  this.context.stroke();
  this.context.strokeStyle='#000000';
  this.context.beginPath();
  this.context.arc(x,y,this.pt+1,0,2*Math.PI,false);
  this.context.closePath();
  this.context.stroke();
};

// �}�E�X�C�x���g������������
ColorPicker.prototype.initMouseEvent=function(){
  var mx,my,pressedArea=-1,cp=this;
  this.canvas.addEventListener('mousedown',cpMousePressed,false);

// �}�E�X�{�^�������������̏������s��
  function cpMousePressed(e){
    e.preventDefault();
    e.stopPropagation();
    document.addEventListener('mouseup',cpMouseReleased,false);
    document.addEventListener('mousemove',cpMouseMoved,false);
    var bcr=e.target.getBoundingClientRect();
    mx=e.clientX-bcr.left;
    my=e.clientY-bcr.top;
    if((cp.rx-cp.pt<=mx) && (mx<=cp.rx+cp.rect+cp.pt) &&
       (cp.ry-cp.pt<=my) && (my<=cp.ry+cp.rect+cp.pt)){
      pressedArea=1;
      var sl=satLight();
      cp.hsl[1]=sl[0];
      cp.hsl[2]=sl[1];
    }
    else{
      pressedArea=0;
      cp.hsl[0]=hue();
    }
    cp.draw();
  }

// �}�E�X�{�^���𗣂������̏������s��
  function cpMouseReleased(e){
    e.preventDefault();
    e.stopPropagation();
    document.removeEventListener('mouseup',cpMouseReleased,false);
    document.removeEventListener('mousemove',cpMouseMoved,false);
    pressedArea=-1;
  }

// �}�E�X�{�^�����ړ����������̏������s��
  function cpMouseMoved(e){
    var bcr=e.target.getBoundingClientRect();
    switch(pressedArea){
      case 0:
      	mx=e.clientX-bcr.left;
      	my=e.clientY-bcr.top;
      	cp.hsl[0]=hue();
      	cp.draw();
      	break;
      case 1:
      	mx=e.clientX-bcr.left;
      	my=e.clientY-bcr.top;
      	var sl=satLight();
      	cp.hsl[1]=sl[0];
      	cp.hsl[2]=sl[1];
      	cp.draw();
      	break;
    }
  }

// �F����Ԃ�
  function hue(){
    var h=Math.atan2(cp.cy-my,mx-cp.cx)/(2*Math.PI);
    if(h<0) h+=1;
    return h;
  }

// �ʓx�P�x��Ԃ�
  function satLight(){
    var s=(mx-cp.rx)/cp.rect;
    var l=1-(my-cp.ry)/cp.rect;
    return [Math.min(Math.max(s,0),1),Math.min(Math.max(l,0),1)];
  }
};

// RGB�J���[��Ԃ�
// rgbmax,rgbmin - R,G,B�̍ő�l,�ŏ��l
// h - �F��
function hueToRGB(rgbmax,rgbmin,h){
  if(h<0)      h+=1;
  else if(h>1) h-=1;
  h*=6;
  if(h<1)       return rgbmin+(rgbmax-rgbmin)*h;
  else if(h<=3) return rgbmax;
  else if(h<4)  return rgbmin+(rgbmax-rgbmin)*(4-h);
  else          return rgbmin;
}
