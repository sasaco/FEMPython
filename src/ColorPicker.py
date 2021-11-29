CPICK_FONT_SIZE=14				# フォントサイズ
CPICK_FONT=CPICK_FONT_SIZE+"px 'Arial'"	# フォント

#--------------------------------------------------------------------#
# カラーピッカー
# canvas - 描画キャンバス
# div - 円形描画分割数
# x,y,width,height - 描画位置,幅,高さ
ColorPicker(canvas,div,x,y,width,height):
  self.canvas=canvas
  self.context=canvas.getContext('2d')	# コンテキスト
  self.context.font=CPICK_FONT
  self.context.textBaseline='top'
  self.div=divor360
  x=xor0
  y=yor0
  width=widthorcanvas.width
  height=heightorcanvas.height
# 中心座標
  size=min(width,height)
  self.cx=int(x+0.5*size)
  self.cy=int(y+0.5*size)
# 色相円形スライダー半径
  self.ro=int(0.48*size)
  self.rc=int(0.9*self.ro)
  self.ri=int(0.8*self.ro)
# 彩度輝度マップ描画位置,サイズ
  self.rect=int(1.3*self.ri)
  self.rx=int(self.cx-0.5*self.rect)
  self.ry=int(self.cy-0.5*self.rect)
  self.pt=int(max(0.07*self.ri,1))
  self.hsl=[0,0,0]			# 現在の色 (HSL色空間)
# テキスト位置
  if(width>=height):
    self.tx=size+10
    self.ty=10
  }
  else:
    self.tx=10
    self.ty=size+10
  }
  self.initMouseEvent()
}

# 現在の色を設定する
# rgb - 指定する色 (RGB)
ColorPicker.prototype.setColor(rgb):
  r=rgb[0],g=rgb[1],b=rgb[2]
  rgbmax=max(max(r,g),b)
  rgbmin=min(min(r,g),b)
  self.hsl[2]=0.5*(rgbmax+rgbmin)
  if(rgbmax==rgbmin):
    self.hsl[0]=0
    self.hsl[1]=0
  }
  else:
    del=rgbmax-rgbmin
    self.hsl[1]=del/(1-abs(2*self.hsl[2]-1))
    if(rgbmin==b)      self.hsl[0]=((g-r)/del+1)/6
    elif(rgbmin==r) self.hsl[0]=((b-g)/del+3)/6
    elif(rgbmin==g) self.hsl[0]=((r-b)/del+5)/6
  }
  self.draw()
}

# 現在の色 (RGB) を返す
ColorPicker.prototype.getColor():
  h=self.hsl[0],s=self.hsl[1],l=self.hsl[2]
  rgbmax=l+0.5*s*(1-abs(2*l-1))
  rgbmin=2*l-rgbmax
  return [hueToRGB(rgbmax,rgbmin,h+1/3),hueToRGB(rgbmax,rgbmin,h),
      	  hueToRGB(rgbmax,rgbmin,h-1/3)]
}

# カラーピッカーを描画する
ColorPicker.prototype.draw():
  self.context.clearRect(0,0,self.canvas.width,self.canvas.height)
# 色相スライダー
  dth=2*math.pi/self.div,dh=360/self.div
  cc0=1,ss0=0,th,h,s,l,i
  for(i=0i<self.divi++):
    th=dth*(i+1)
    cc1=math.cos(th),ss1=math.sin(th)
    h=dh*i
    self.context.fillStyle='hsl('+h+',100%,50%)'
    self.context.strokeStyle=self.context.fillStyle
    self.context.beginPath()
    self.context.moveTo(self.cx+self.ri*cc0,self.cy-self.ri*ss0)
    self.context.lineTo(self.cx+self.ro*cc0,self.cy-self.ro*ss0)
    self.context.lineTo(self.cx+self.ro*cc1,self.cy-self.ro*ss1)
    self.context.lineTo(self.cx+self.ri*cc1,self.cy-self.ri*ss1)
    self.context.closePath()
    self.context.fill()
    self.context.stroke()
    cc0=cc1
    ss0=ss1
  }
  th=2*math.pi*self.hsl[0]
  cc0=math.cos(th)
  ss0=math.sin(th)
  self.drawPointer(self.cx+self.rc*cc0,self.cy-self.rc*ss0)

# 彩度輝度マップ
  h=360*self.hsl[0]
  dl=100/self.rect
  for(i=0i<self.recti++):
    y=self.ry+i
    l=dl*(self.rect-i)
    grad=self.context.createLinearGradient
      (self.rx,y,self.rx+self.rect,y)
    grad.addColorStop(0,'hsl('+h+',0%,'+l+'%)')
    grad.addColorStop(1,'hsl('+h+',100%,'+l+'%)')
    self.context.fillStyle=grad
    self.context.fillRect(self.rx,y,self.rect,1)
  }
  self.drawPointer(int(self.rx+self.rect*self.hsl[1]),
      	      	   int(self.ry+self.rect*(1-self.hsl[2])))

# テキスト
  self.context.fillStyle='#000000'
  self.context.fillText('H:'+int(h),self.tx,self.ty)
  s=int(100*self.hsl[1])
  l=int(100*self.hsl[2])
  if(self.tx<=self.ty):
    w=int(self.canvas.width/3)
    self.context.fillText('S:'+s+'%',self.tx+w,self.ty)
    self.context.fillText('L:'+l+'%',self.tx+2*w,self.ty)
  }
  else:
    self.context.fillText('S:'+s+'%',self.tx,self.ty+CPICK_FONT_SIZE)
    self.context.fillText('L:'+l+'%',self.tx,self.ty+2*CPICK_FONT_SIZE)
  }
}

# ポインタを描画する
# x,y - 描画位置
ColorPicker.prototype.drawPointer(x,y):
  h=360*self.hsl[0],s=100*self.hsl[1],l=100*self.hsl[2]
  self.context.fillStyle='hsl('+h+','+s+'%,'+l+'%)'
  self.context.beginPath()
  self.context.arc(x,y,self.pt,0,2*math.pi,False)
  self.context.closePath()
  self.context.fill()
  self.context.strokeStyle='#ffffff'
  self.context.beginPath()
  self.context.arc(x,y,self.pt,0,2*math.pi,False)
  self.context.closePath()
  self.context.stroke()
  self.context.strokeStyle='#000000'
  self.context.beginPath()
  self.context.arc(x,y,self.pt+1,0,2*math.pi,False)
  self.context.closePath()
  self.context.stroke()
}

# マウスイベントを初期化する
ColorPicker.prototype.initMouseEvent():
  mx,my,pressedArea=-1,cp=this
  self.canvas.addEventListener('mousedown',cpMousePressed,False)

# マウスボタンを押した時の処理を行う
  function cpMousePressed(e):
    e.preventDefault()
    e.stopPropagation()
    document.addEventListener('mouseup',cpMouseReleased,False)
    document.addEventListener('mousemove',cpMouseMoved,False)
    bcr=e.target.getBoundingClientRect()
    mx=e.clientX-bcr.left
    my=e.clientY-bcr.top
    if((cp.rx-cp.pt<=mx) and (mx<=cp.rx+cp.rect+cp.pt) and
       (cp.ry-cp.pt<=my) and (my<=cp.ry+cp.rect+cp.pt)):
      pressedArea=1
      sl=satLight()
      cp.hsl[1]=sl[0]
      cp.hsl[2]=sl[1]
    }
    else:
      pressedArea=0
      cp.hsl[0]=hue()
    }
    cp.draw()
  }

# マウスボタンを離した時の処理を行う
  function cpMouseReleased(e):
    e.preventDefault()
    e.stopPropagation()
    document.removeEventListener('mouseup',cpMouseReleased,False)
    document.removeEventListener('mousemove',cpMouseMoved,False)
    pressedArea=-1
  }

# マウスボタンを移動させた時の処理を行う
  function cpMouseMoved(e):
    bcr=e.target.getBoundingClientRect()
    switch(pressedArea):
      case 0:
      	mx=e.clientX-bcr.left
      	my=e.clientY-bcr.top
      	cp.hsl[0]=hue()
      	cp.draw()
      	break
      case 1:
      	mx=e.clientX-bcr.left
      	my=e.clientY-bcr.top
      	sl=satLight()
      	cp.hsl[1]=sl[0]
      	cp.hsl[2]=sl[1]
      	cp.draw()
      	break
    }
  }

# 色相を返す
  function hue():
    h=math.atan2(cp.cy-my,mx-cp.cx)/(2*math.pi)
    if(h<0) h+=1
    return h
  }

# 彩度輝度を返す
  function satLight():
    s=(mx-cp.rx)/cp.rect
    l=1-(my-cp.ry)/cp.rect
    return [min(max(s,0),1),min(max(l,0),1)]
  }
}

# RGBカラーを返す
# rgbmax,rgbmin - R,G,Bの最大値,最小値
# h - 色相
function hueToRGB(rgbmax,rgbmin,h):
  if(h<0)      h+=1
  elif(h>1) h-=1
  h*=6
  if(h<1)       return rgbmin+(rgbmax-rgbmin)*h
  elif(h<=3) return rgbmax
  elif(h<4)  return rgbmin+(rgbmax-rgbmin)*(4-h)
  else          return rgbmin
}
