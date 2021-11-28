from FemDataModel import FemDataModel
#--------------------------------------------------------------------#
# 3次元有限要素法(FEM)

model = None		# FEMデータモデル
viewModel = None		# 表示モデル
viewObj = None		# 表示オブジェクト
bounds = None		# モデル境界
info = None		# モデル情報表示部
colorBar = None		# カラーバー
resultView = None		# 結果表示設定
viewConfig = None		# モデル表示設定
modalWindow = None	# モーダルウィンドウ

FILE_WINDOW=0	# ファイル操作ウィンドウ
CALC_WINDOW=1	# 計算設定ウィンドウ
RESULT_WINDOW=2	# 結果表示設定ウィンドウ
CONFIG_WINDOW=3	# コンフィグウィンドウ

# データを初期化する
# fileName - データファイル名
def initModel(fileName):
    model= FemDataModel()
    initThree()
    resultView=ResultView()
    viewConfig=ViewConfig()
    if fileName!=None:
        readServerFemFile(fileName)

    modalWindow=[document.getElementById('file'),
                document.getElementById('calc'),
                document.getElementById('result'),
                document.getElementById('config')]
    loop()

# three.js の初期化をする
def initThree():
    document.addEventListener('keydown',keyPressed,False)
    bounds=Bounds()
    viewModel=ViewModel('fem-canvas')
    viewObj=ViewObject()
    info=document.getElementById('info-canvas')
    colorBar=ColorBar('colorbar')

# キーを押した時の処理を行う
def keyPressed(e):
    if(e.keyCode == 88): # X
        viewModel.viewX()
    elif(e.keyCode == 89): # Y
        viewModel.viewY()
    elif(e.keyCode == 90): # Z
        viewModel.viewZ()


# 表示オブジェクトを初期化する
def initObject():
    viewObj.remove()
    viewObj.create()
    viewModel.updateLightAndCamera()
    colorBar.clear()
    resultView.setContourSelect()
    showInfo()


# ループ関数
def loop():
    viewModel.update()
    requestAnimationFrame(loop)
    viewModel.setAxis()


# モデル情報を表示する
def showInfo():
    if model.result.calculated:
        if model.result.dispMax==0 and model.result.tempMax!=0:
            info.textContent='温度 Max.:'+numString(model.result.tempMax)
        else:
            info.textContent='変位 Max.:'+numString(model.result.dispMax)
    else:
        info.innerHTML='節点:'+model.mesh.nodes.length+
                                     '<br />要素:'+model.mesh.elements.length

# 固有値を表示する
# index - 固有値のインデックス
# type - 解析種類
# value - 固有値
def showEigenValue(index, type,value):
    if type==BUCKLING:
        info.textContent='固有値'+(index+1)+': '+ numString(value)
    else:
        info.textContent='固有振動数'+(index+1)+': '+ numString(value)


# 数値を表す文字列を返す
# value - 数値
def numString(value):
    vabs = abs(value)
    if(vabs>=1.0E5):
        return value.toExponential(4)
    elif((vabs>=1) or (vabs==0)):
        return value.toFixed(3)
    elif(vabs>=0.01):
        return value.toFixed(4)
    else:
        return value.toExponential(4)


# モーダルウィンドウを表示する
# win - モーダルウィンドウのインデックス
def showModalWindow(win):
    modalWindow[win].style.zIndex=4
    modalWindow[win].style.opacity=1


# モーダルウィンドウを非表示にする
# win - モーダルウィンドウのインデックス
def hideModalWindow(win):
    modalWindow[win].style.zIndex=1
    modalWindow[win].style.opacity=0


# # 継承関係を設定する
# # ctor - 新クラス
# # superCtor - 継承元クラス
# function inherits(ctor,superCtor):
#   if((ctor==undefined) or (ctor==None))
#     throw new TypeError('The constructor to `inherits` must not be '+
#                         'None or undefined.')

#   if((superCtor==undefined) or (superCtor==None))
#     throw new TypeError('The super constructor to `inherits` must '+
#                         'not be None or undefined.')

#   if(superCtor.prototype==undefined)
#     throw new TypeError('The super constructor to `inherits` must '+
#                         'have a prototype.')
#   ctor.super_=superCtor
#   Object.setPrototypeOf(ctor.prototype,superCtor.prototype)
# }
