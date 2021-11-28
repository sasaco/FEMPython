#--------------------------------------------------------------------#

cls=[]					# コンター図色
CBAR_FONT_SIZE=14				# フォントサイズ
CBAR_FONT=CBAR_FONT_SIZE+"px 'Arial'"	# フォント
FG_COLOR='#ffffff'				# 前景色


# コンター図の色を返す
# z - 0～1の値
def contourColor(z):
    cls[0]=0
    cls[1]=0
    cls[2]=0
    if z<=0:
        cls[2]=1

    elif z<0.25:
        cls[1]=4*z
        cls[2]=1

    elif z<0.5:
        cls[1]=1.2-0.8*z
        cls[2]=2-4*z

    elif z<0.75:
        cls[0]=4*z-2
        cls[1]=0.4+0.8*z

    elif z<1:
        cls[0]=1
        cls[1]=4-4*z

    else:
        cls[0]=1

    return cls


#--------------------------------------------------------------------#
# カラーバー
# canvasId - 表示領域ID
class ColorBar:
    def __init__(self,canvasId):
        self.canvas=document.getElementById(canvasId)	# 描画キャンバス
        self.canvas.width=self.canvas.clientWidth
        self.canvas.height=self.canvas.clientHeight
        self.context=self.canvas.getContext('2d')		# コンテキスト
        self.context.font=CBAR_FONT
        self.context.textBaseline='top'
        self.rect={
            "x":int(self.canvas.width/5),		# カラーバーの描画領域
            "y":int(self.canvas.height/10),
            "width":int(self.canvas.width*3/10),
            "height":int(self.canvas.height*4/5)
        }
        self.gradient=self.context.createLinearGradient(self.rect.x,self.rect.y+self.rect.height,self.rect.x,self.rect.y)
        self.gradient.addColorStop(0,'#0000ff')
        self.gradient.addColorStop(0.25,'#00ffff')
        self.gradient.addColorStop(0.5,'#00cc00')
        self.gradient.addColorStop(0.75,'#ffff00')
        self.gradient.addColorStop(1,'#ff0000')
        xt=self.rect.x+self.rect.width+5
        # 数値表示位置
        self.maxPos={
            "x":xt,
            "y":int(self.rect.y-CBAR_FONT_SIZE/2)
        }
        self.minPos={
            "x":xt,
            "y":self.maxPos.y+self.rect.height
        }


    # コンター図を描画する
    # minValue - コンター図データ最小値
    # maxValue - コンター図データ最大値
    def draw(self, minValue,maxValue):
        self.context.clearRect(0,0,self.canvas.width,self.canvas.height)
        self.context.fillStyle=self.gradient
        self.context.fillRect(self.rect.x,self.rect.y,
                                                    self.rect.width,self.rect.height)
        self.context.strokeStyle=FG_COLOR
        self.context.strokeRect(self.rect.x,self.rect.y,
                                                        self.rect.width,self.rect.height)
        self.context.fillStyle=FG_COLOR
        self.context.fillText(numString(maxValue),self.maxPos.x,self.maxPos.y)
        self.context.fillText(numString(minValue),self.minPos.x,self.minPos.y)


    # コンター図を消去する
    def clear(self):
        self.context.clearRect(0,0,self.canvas.width,self.canvas.height)

