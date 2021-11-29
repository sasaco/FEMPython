# ベクトル最小長さ
MIN_VECTOR=1e-8		# ベクトル長さの最小値
# 拘束条件表示マテリアル
REST_MAT=THREE.MeshBasicMaterial(:color:0x0066ff})
LOAD_COLOR=0x00ff00	# 荷重条件表示色
PRESS_COLOR=0xff00ff	# 面圧条件表示色
HTC_COLOR=0xffcc00		# 熱伝達境界条件表示色
# 節点温度条件表示マテリアル
TEMP_MAT=THREE.MeshBasicMaterial(:color:0xff3300})
# 要素表示マテリアル
elemMat=THREE.MeshStandardMaterial
  (:color:0xffffff,roughness:0.2,metalness:0.5,
    transparent:True,opacity:0.8,
    vertexColors:THREE.VertexColors,side:THREE.DoubleSide})
# 要素辺の表示マテリアル
EDGE_MAT=THREE.LineBasicMaterial(:color:0xffffff})
# 梁要素の表示マテリアル
BAR_MAT=THREE.LineBasicMaterial
  (:color:0xffffff,vertexColors:THREE.VertexColors})
meshColors=[0.9,0.9,0.9]	# メッシュのデフォルト表示色

#--------------------------------------------------------------------#
# 表示オブジェクト
ViewObject():
  self.mesh=None	# 計算メッシュ
  self.edge=None	# 要素辺
  self.bar=None	# 梁要素
  self.rest=None	# 拘束条件
  self.load=None	# 荷重条件
  self.press=None	# 面圧条件
  self.htc=None	# 熱伝達境界条件
  self.temp=None	# 節点温度条件
  self.showEdge=True	# 要素辺表示スイッチ
}

# 表示オブジェクトを作成する
ViewObject.prototype.create():
  geometry=model.mesh.getGeometry()
  self.mesh=THREE.Mesh(geometry,elemMat)
  viewModel.addObject(self.mesh)
  geometry=model.mesh.getEdgeGeometry()
  self.edge=THREE.LineSegments(geometry,EDGE_MAT)
  viewModel.addObject(self.edge)
  geometry=model.mesh.getBarGeometry()
  self.bar=new BarObject(geometry)
  viewModel.addObject(self.bar)
  self.createRestraint()
  self.createLoad()
  self.createPress()
  self.createHTC()
  self.createTemperature()
  self.setEdgeView()
}

# 拘束条件表示オブジェクトを作成する
ViewObject.prototype.createRestraint():
  hs=0.02*bounds.size,rests=model.bc.restraints
  self.rest=THREE.Group()
  for(i=0i<rests.lengthi++):
    r=new RestraintHelper(rests[i],hs)
    r.position.copy(model.mesh.nodes[rests[i].node])
    self.rest.add(r)
  }
  viewModel.addObject(self.rest)
}

# 荷重条件表示オブジェクトを作成する
ViewObject.prototype.createLoad():
  coef=0.1*bounds.size/max(model.bc.loadMax,MIN_VECTOR)
  hl=0.02*bounds.size,hw=0.5*hl,loads=model.bc.loads
  self.load=THREE.Group()
  for(i=0i<loads.lengthi++):
    ld=loads[i]
    l=ld.magnitude()
    if(l>=MIN_VECTOR):
      gx=ld.globalX
      dir=THREE.Vector3(gx[0],gx[1],gx[2]).normalize()
      l=max(coef*l,MIN_VECTOR+hl)
      arrow=THREE.ArrowHelper
      	(dir,model.mesh.nodes[ld.node],l,LOAD_COLOR,hl,hw)
      self.load.add(arrow)
    }
  }
  viewModel.addObject(self.load)
}

# 面圧条件表示オブジェクトを作成する
ViewObject.prototype.createPress():
  coef=0.1*bounds.size/max(model.bc.pressMax,MIN_VECTOR)
  hl=0.02*bounds.size,hw=0.5*hl,prs=model.bc.pressures
  self.press=THREE.Group()
  for(i=0i<prs.lengthi++):
    ps=prs[i]
    eb=ps.getBorder(model.mesh.elements[ps.element])
    p=model.mesh.getNodes(eb)
    c=center(p),norm=normalVector(p)
    l=ps.press
    if(l>=MIN_VECTOR):
      dir=norm.clone().negate()
      l=max(coef*l,MIN_VECTOR+hl)
      p0=c.clone().addScaledVector(dir,-l)
      arrow=THREE.ArrowHelper(dir,p0,l,PRESS_COLOR,hl,hw)
      self.press.add(arrow)
    }
  }
  viewModel.addObject(self.press)
}

# 熱伝達境界条件表示オブジェクトを作成する
ViewObject.prototype.createHTC():
  l=0.1*bounds.size
  hl=0.04*bounds.size,hw=0.5*hl,htcs=model.bc.htcs
  self.htc=THREE.Group()
  for(i=0i<htcs.lengthi++):
    ht=htcs[i],elem=model.mesh.elements[ht.element]
    eb=ht.getBorder(elem)
    p=model.mesh.getNodes(eb)
    c=center(p),norm=normalVector(p)
    if(eb.isEdge):
      norm=eb.normalVector(p,model.mesh.getNodes(elem))
    }
    dir=norm.clone().negate()
    p0=c.clone().addScaledVector(dir,-l)
    arrow=THREE.ArrowHelper(dir,p0,l,HTC_COLOR,hl,hw)
    self.htc.add(arrow)
  }
  viewModel.addObject(self.htc)
}

# 節点温度表示オブジェクトを作成する
ViewObject.prototype.createTemperature():
  rr=0.01*bounds.size,temps=model.bc.temperature
  self.temp=THREE.Group()
  for(i=0i<temps.lengthi++):
    mesh=THREE.Mesh(THREE.SphereGeometry(rr),TEMP_MAT)
    mesh.position.copy(model.mesh.nodes[temps[i].node])
    self.temp.add(mesh)
  }
  viewModel.addObject(self.temp)
}

# 変位を設定する
# disp - 変位
# coef - 表示係数
ViewObject.prototype.setDisplacement(disp,coef):
  if(disp.length==0) return
  setGeomDisplacement(self.mesh.geometry,disp,coef)
  setGeomDisplacement(self.edge.geometry,disp,coef)
  self.bar.setDisplacement(disp,coef)
}

# コンター図を設定する
# value - コンター図データ
# minValue,maxValue - コンター図データ最小値,最大値
# type - データ保持形態
ViewObject.prototype.setContour(value,minValue,maxValue,type):
  coef=1
  if(maxValue!=minValue) coef=1/(maxValue-minValue)
  setGeomContour(self.mesh.geometry,value,minValue,coef,type)
  self.bar.setContour(value,minValue,coef,type)
}

# 結果表示を消去する
ViewObject.prototype.removeResult():
  removeGeomResult(self.mesh.geometry)
  self.bar.removeResult()

  geom=self.edge.geometry,label=geom.nodes,angle=geom.angle
  pos=geom.attributes.position.array
  for(i=0i<label.lengthi++):
    i3=3*i,p=model.mesh.nodes[label[i]]
    pos[i3]=p.x
    pos[i3+1]=p.y
    pos[i3+2]=p.z
    angle[i3]=0
    angle[i3+1]=0
    angle[i3+2]=0
  }
  geom.attributes.position.needsUpdate=True
}

# コンター図を消去する
ViewObject.prototype.clearContour():
  clearGeomContour(self.mesh.geometry)
  self.bar.clearContour()
}

# 要素辺表示を切り替える
# showEdge - 要素辺を表示する場合はTrue
ViewObject.prototype.setShowEdge(showEdge):
  self.showEdge=showEdge
  self.setEdgeView()
}

# 要素辺表示を切り替える
ViewObject.prototype.setEdgeView():
  if(self.edge!=None):
    self.edge.visible=self.showEdge
  }
  if(self.showEdge):
    elemMat.opacity=0.8
  }
  else:
    elemMat.opacity=1
  }
}

# 表示オブジェクトを削除する
ViewObject.prototype.remove():
  i,child
  if(self.mesh!=None):
    viewModel.removeObject(self.mesh)
    self.mesh.geometry.dispose()
    self.mesh.material.dispose()
    self.mesh=None
  }
  if(self.edge!=None):
    viewModel.removeObject(self.edge)
    self.edge.geometry.dispose()
    self.edge.material.dispose()
    self.edge=None
  }
  if(self.bar!=None):
    self.bar.removeObject()
    self.bar=None
  }
  if(self.rest!=None):
    for(i=self.rest.children.length-1i>=0i--):
      self.rest.children[i].removeChildren()
      self.rest.remove(self.rest.children[i])
    }
    self.rest=None
  }
  if(self.load!=None):
    self.removeAllows(self.load)
    self.load=None
  }
  if(self.press!=None):
    self.removeAllows(self.press)
    self.press=None
  }
  if(self.htc!=None):
    self.removeAllows(self.htc)
    self.htc=None
  }
  if(self.temp!=None):
    viewModel.removeObject(self.temp)
    for(i=self.temp.children.length-1i>=0i--):
      child=self.temp.children[i]
      child.geometry.dispose()
      child.material.dispose()
      self.temp.remove(child)
    }
    self.temp=None
  }
}

# 矢印集合の表示オブジェクトを削除する
# grp - 矢印集合
ViewObject.prototype.removeAllows(grp):
  viewModel.removeObject(grp)
  for(i=grp.children.length-1i>=0i--):
    child=grp.children[i]
    child.line.geometry.dispose()
    child.line.material.dispose()
    child.cone.geometry.dispose()
    child.cone.material.dispose()
    grp.remove(child)
  }
}

# 変位を設定する
# geometry - 座標を設定する形状データ
# disp - 変位
# coef - 表示係数
function setGeomDisplacement(geometry,disp,coef):
  label=geometry.nodes,nodes=model.mesh.nodes,angle=geometry.angle
  pos=geometry.attributes.position.array
  for(i=0i<label.lengthi++):
    i3=3*i,p=nodes[label[i]],dx=disp[label[i]].x
    pos[i3]=p.x+coef*dx[0]
    pos[i3+1]=p.y+coef*dx[1]
    pos[i3+2]=p.z+coef*dx[2]
    angle[i3]=coef*dx[3]
    angle[i3+1]=coef*dx[4]
    angle[i3+2]=coef*dx[5]
  }
  geometry.attributes.position.needsUpdate=True
}

# 形状データのコンター図を設定する
# geometry - 対象となる形状データ
# value - コンター図データ
# minValue - コンター図データ最小値
# coef - データ変換係数
# type - データ保持形態
function setGeomContour(geometry,value,minValue,coef,type):
  label,colors=geometry.attributes.color.array
  if(type==ELEMENT_DATA):
    label=geometry.elements
  }
  else:
    label=geometry.nodes
  }
  for(i=0i<label.lengthi++):
    i3=3*i,d=coef*(value[label[i]]-minValue)
    cls=contourColor(d)
    colors[i3]=cls[0]
    colors[i3+1]=cls[1]
    colors[i3+2]=cls[2]
  }
  geometry.attributes.color.needsUpdate=True
}

# 形状データの結果表示を消去する
# geometry - 対象となる形状データ
function removeGeomResult(geometry):
  label=geometry.nodes,nodes=model.mesh.nodes,angle=geometry.angle
  pos=geometry.attributes.position.array
  colors=geometry.attributes.color.array
  for(i=0i<label.lengthi++):
    i3=3*i,p=nodes[label[i]]
    pos[i3]=p.x
    pos[i3+1]=p.y
    pos[i3+2]=p.z
    colors[i3]=meshColors[0]
    colors[i3+1]=meshColors[1]
    colors[i3+2]=meshColors[2]
    angle[i3]=0
    angle[i3+1]=0
    angle[i3+2]=0
  }
  geometry.attributes.position.needsUpdate=True
  geometry.attributes.color.needsUpdate=True
}

# 形状データのコンター図を消去する
# geometry - 対象となる形状データ
function clearGeomContour(geometry):
  colors=geometry.attributes.color.array
  for(i=0i<colors.lengthi+=3):
    colors[i]=meshColors[0]
    colors[i+1]=meshColors[1]
    colors[i+2]=meshColors[2]
  }
  geometry.attributes.color.needsUpdate=True
}

#--------------------------------------------------------------------#
# 拘束条件表示オブジェクト
# rest - 拘束条件
# size - 表示サイズ
RestraintHelper(rest,size):
  THREE.Group.call(this)
  geom
  if(rest.rest[0]):
    geom=THREE.CylinderBufferGeometry(0,0.5*size,size,5,1)
    geom.translate(0,-0.5*size,0)
    geom.rotateZ(0.5*math.pi)
    self.add(THREE.Mesh(geom,REST_MAT))
  }
  if(rest.rest[1]):
    geom=THREE.CylinderBufferGeometry(0,0.5*size,size,5,1)
    geom.translate(0,-0.5*size,0)
    geom.rotateX(math.pi)
    self.add(THREE.Mesh(geom,REST_MAT))
  }
  if(rest.rest[2]):
    geom=THREE.CylinderBufferGeometry(0,0.5*size,size,5,1)
    geom.translate(0,-0.5*size,0)
    geom.rotateX(-0.5*math.pi)
    self.add(THREE.Mesh(geom,REST_MAT))
  }
  if(rest.rest[3]):
    geom=THREE.CylinderBufferGeometry(0,0.3*size,2*size,5,1)
    geom.translate(0,size,0)
    geom.rotateZ(-0.5*math.pi)
    self.add(THREE.Mesh(geom,REST_MAT))
  }
  if(rest.rest[4]):
    geom=THREE.CylinderBufferGeometry(0,0.3*size,2*size,5,1)
    geom.translate(0,size,0)
    self.add(THREE.Mesh(geom,REST_MAT))
  }
  if(rest.rest[5]):
    geom=THREE.CylinderBufferGeometry(0,0.3*size,2*size,5,1)
    geom.translate(0,size,0)
    geom.rotateX(0.5*math.pi)
    self.add(THREE.Mesh(geom,REST_MAT))
  }
  if(rest.coords):
    e=rest.coords.c.elements
    e2=[e[0],e[1],e[2],0,e[3],e[4],e[5],0,e[6],e[7],e[8],0,0,0,0,1]
    self.applyMatrix(THREE.Matrix4().fromArray(e2))
  }
}

# 表示オブジェクトを削除する
RestraintHelper.prototype.removeChildren():
  for(i=self.children.length-1i>=0i--):
    child=self.children[i]
    child.geometry.dispose()
    child.material.dispose()
    self.remove(child)
  }
}

#--------------------------------------------------------------------#
# 梁要素表示オブジェクト
# geometry - 要素中心線の形状データ
BarObject(geometry):
  THREE.Group.call(this)
  self.center=THREE.LineSegments(geometry,BAR_MAT)
  self.add(self.center)
  param=geometry.param
  count=2*param.length	# １要素に断面２つ（内側と外側）
  for(i=0i<counti++):
    geom=THREE.BufferGeometry()
    points=3*param[int(i/2)].vertexCount()+3
    pos=new Float32Array(points)
    colors=new Float32Array(points)
    for(j=0j<pointsj++):
      pos[j]=0
      colors[j]=meshColors[j%3]
    }
    geom.addAttribute('position',THREE.BufferAttribute(pos,3))
    geom.addAttribute('color',THREE.BufferAttribute(colors,3))
    self.add(THREE.Line(geom,BAR_MAT))
  }
  self.setSection()
}

# 断面の位置を設定する
BarObject.prototype.setSection():
  param=self.center.geometry.param
  dir=self.center.geometry.dir,angle=self.center.geometry.angle
  v=self.center.geometry.attributes.position.array
  v1=THREE.Vector3(),v2=THREE.Vector3()
  axis=THREE.Vector3()
  for(i=0i<param.lengthi++):
    i6=6*i
    geom1=self.children[2*i+1].geometry
    geom2=self.children[2*i+2].geometry
    pos1=geom1.attributes.position.array
    pos2=geom2.attributes.position.array
    cx=0.5*(v[i6]+v[i6+3])
    cy=0.5*(v[i6+1]+v[i6+4])
    cz=0.5*(v[i6+2]+v[i6+5])
    wx=0.5*(angle[i6]+angle[i6+3])
    wy=0.5*(angle[i6+1]+angle[i6+4])
    wz=0.5*(angle[i6+2]+angle[i6+5])
    v1.set(v[i6+3]-v[i6],v[i6+4]-v[i6+1],v[i6+5]-v[i6+2]).normalize()
    v2.copy(dir[i][1])
    ww=math.sqrt(wx*wx+wy*wy+wz*wz)
    if(ww>0):
      v2.applyAxisAngle(axis.set(wx/ww,wy/ww,wz/ww),ww)
    }
    param[i].setCoords(pos1,pos2,cx,cy,cz,v1,v2)
    geom1.attributes.position.needsUpdate=True
    geom2.attributes.position.needsUpdate=True
  }
}

# 変位を設定する
# disp - 変位
# coef - 表示係数
BarObject.prototype.setDisplacement(disp,coef):
  setGeomDisplacement(self.center.geometry,disp,coef)
  self.setSection()
}

# コンター図を設定する
# value - コンター図データ
# minValue - コンター図データ最小値
# coef - データ変換係数
# type - データ保持形態
BarObject.prototype.setContour(value,minValue,coef,type):
  setGeomContour(self.center.geometry,value,minValue,coef,type)
}

# 結果表示を消去する
BarObject.prototype.removeResult():
  removeGeomResult(self.center.geometry)
  self.setSection()
}

# コンター図を消去する
BarObject.prototype.clearContour():
  clearGeomContour(self.center.geometry)
}

# 表示オブジェクトを削除する
BarObject.prototype.removeObject():
  viewModel.removeObject(this)
  for(i=self.children.length-1i>=0i--):
    child=self.children[i]
    child.geometry.dispose()
    child.material.dispose()
    self.remove(child)
  }
}

#--------------------------------------------------------------------#
# モデル表示設定
ViewConfig():
  canvas=document.getElementById('el-color')	# 要素表示色設定用
  canvas.width=canvas.clientWidth
  canvas.height=canvas.clientHeight
  self.cpicker=new ColorPicker(canvas)			# カラーピッカー
  self.showEdge=document.getElementById('showedge')	# 要素辺表示
  self.lightx=document.getElementById('lightx')	# 光源位置設定用
  self.lighty=document.getElementById('lighty')
  self.lightz=document.getElementById('lightz')
}

# コンフィグウィンドウを表示する
ViewConfig.prototype.show():
  self.cpicker.setColor(meshColors)
  lp=viewModel.directionalLight.position
  self.lightx.value=numString(lp.x)
  self.lighty.value=numString(lp.y)
  self.lightz.value=numString(lp.z)
}

# コンフィグを設定する
ViewConfig.prototype.set():
  meshColors=self.cpicker.getColor()
  if(int(resultView.contour.value)<0):
    viewObj.clearContour()
  }
  viewObj.setShowEdge(self.showEdge.checked)
  lp=viewModel.directionalLight.position
  lp.set(float(self.lightx.value),float(self.lighty.value),
      	 float(self.lightz.value))
}

# コンフィグウィンドウを表示する
function showConfig():
  showModalWindow(CONFIG_WINDOW)
  viewConfig.show()
}

# コンフィグを設定する
function setConfig():
  hideModalWindow(CONFIG_WINDOW)
  viewConfig.set()
}

# コンフィグを取り消す
function cancelConfig():
  hideModalWindow(CONFIG_WINDOW)
}

inherits(RestraintHelper,THREE.Group)
inherits(BarObject,THREE.Group)
