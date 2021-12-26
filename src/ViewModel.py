# #--------------------------------------------------------------------#
# # 表示モデル
# # canvasId - 表示領域ID
# ViewModel(canvasId):
#   self.canvasFrame=document.getElementById(canvasId)		# 描画フレーム
#   self.renderer=THREE.WebGLRenderer(:antialias:True})	# レンダラ―
#   if(!self.renderer):
#     alert("three.js の初期化に失敗しました")
#   }

#   self.renderer.setSize(self.canvasFrame.clientWidth,
#       	      	      	self.canvasFrame.clientHeight)
#   self.canvasFrame.appendChild(self.renderer.domElement)

#   self.renderer.setClearColor(0x000000,1)
#   self.scene=THREE.Scene()					# シーン
#   self.initLight()
#   self.initCamera()
#   self.axis=None
# }

# # 光源を初期化する
# ViewModel.prototype.initLight():
# # 平行光源
#   self.directionalLight=THREE.DirectionalLight(0xffffff,1)
#   bounds.setLightPosition(self.directionalLight.position)
#   self.scene.add(self.directionalLight)
# # 環境光源
#   self.ambientLight=THREE.AmbientLight(0x999999)
#   self.scene.add(self.ambientLight)
# }

# # カメラを初期化する
# ViewModel.prototype.initCamera():
#   aspect=self.canvasFrame.clientWidth/self.canvasFrame.clientHeight
#   side=0.7*bounds.size,c=bounds.center
# # カメラ
#   self.camera=THREE.OrthographicCamera
#     (-side*aspect,side*aspect,side,-side,
#      0.01*bounds.size,100*bounds.size)
#   self.camera.position.set(c.x,c.y,c.z+bounds.viewPoint)
#   self.camera.up.set(0,1,0)
# #  self.camera.lookAt(:x:0,y:0,z:0})	Controls使用時は無効化される

# # トラックボールコントロール
#   self.trackball=THREE.OrthographicTrackballControls
#       	      	   (self.camera,self.canvasFrame)
#   self.trackball.screen.width=self.canvasFrame.clientWidth
#   self.trackball.screen.height=self.canvasFrame.clientHeight
#   bdc=self.canvasFrame.getBoundingClientRect()
#   self.trackball.screen.offetLeft=bdc.left
#   self.trackball.screen.offetTop=bdc.top

#   self.trackball.noRotate=False
#   self.trackball.rotateSpeed=4.0
#   self.trackball.noZoom=False
#   self.trackball.zoomSpeed=4.0
#   self.trackball.noPan=False
#   self.trackball.panSpeed=1.0
#   self.trackball.target.copy(c)
#   self.trackball.staticMoving=True
#   self.trackball.dynamicDampingFactor=0.3
# }

# # 表示オブジェクトを追加する
# ViewModel.prototype.addObject(obj):
#   self.scene.add(obj)
# }

# # 表示オブジェクトを削除する
# ViewModel.prototype.removeObject(obj):
#   self.scene.remove(obj)
# }

# # 座標軸を設定する
# ViewModel.prototype.setAxis():
#   if(self.axis!=None):
#     self.scene.remove(self.axis)
#   }
#   self.axis=THREE.AxisHelper(0.2*bounds.size)
#   self.axis.position.set(0,0,0)
#   self.scene.add(self.axis)
# }

# # 光源・カメラ位置を更新する
# ViewModel.prototype.updateLightAndCamera():
#   bounds.setLightPosition(self.directionalLight.position)
#   aspect=self.canvasFrame.clientWidth/self.canvasFrame.clientHeight
#   side=0.7*bounds.size
#   self.camera.left=-side*aspect
#   self.camera.right=side*aspect
#   self.camera.top=side
#   self.camera.bottom=-side
#   self.camera.near=0.01*bounds.size
#   self.camera.far=100*bounds.size
#   self.trackball.target.copy(bounds.center)
#   self.viewZ()
# }

# # 表示を更新する
# ViewModel.prototype.update():
#   self.trackball.update()
#   self.renderer.render(self.scene,self.camera)
# }

# # 視点をX軸方向にする
# ViewModel.prototype.viewX():
#   c=bounds.center
#   self.camera.position.set(c.x+bounds.viewPoint,c.y,c.z)
#   self.camera.up.set(0,0,1)
#   self.camera.zoom=1
#   self.camera.updateProjectionMatrix()
#   self.trackball.target.copy(c)
# }

# # 視点をY軸方向にする
# ViewModel.prototype.viewY():
#   c=bounds.center
#   self.camera.position.set(c.x,c.y-bounds.viewPoint,c.z)
#   self.camera.up.set(0,0,1)
#   self.camera.zoom=1
#   self.camera.updateProjectionMatrix()
#   self.trackball.target.copy(c)
# }

# # 視点をZ軸方向にする
# ViewModel.prototype.viewZ():
#   c=bounds.center
#   self.camera.position.set(c.x,c.y,c.z+bounds.viewPoint)
#   self.camera.up.set(0,1,0)
#   self.camera.zoom=1
#   self.camera.updateProjectionMatrix()
#   self.trackball.target.copy(c)
# }

# #--------------------------------------------------------------------#
# # モデル境界
# Bounds():
#   self.box=THREE.Box3()
#   self.center=THREE.Vector3()
#   self.size=1
#   self.viewPoint=1
# }

# # モデル境界を設定する
# Bounds.prototype.set():
#   self.box.setFromPoints(model.mesh.nodes)
#   self.center.copy(self.box.getCenter())
#   self.size=max(self.box.max.x-self.box.min.x,
#       	      	     self.box.max.y-self.box.min.y,
#       	      	     self.box.max.z-self.box.min.z)
#   self.viewPoint=2*self.size
# }

# # 光源位置を設定する
# # p - 光源位置
# Bounds.prototype.setLightPosition(p):
#   p.set(self.size,-self.size,self.size)
# }
