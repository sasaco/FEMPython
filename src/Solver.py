import numpy as np
#--------------------------------------------------------------------#

PRECISION=1e-10	# マトリックス精度
LU_METHOD=0	# LU分解法
ILUCG_METHOD=1	# 不完全LU分解共役勾配法


# 質量マトリックスを作成する
# dof - モデル自由度
def massMatrix(dof):
  mesh=model.mesh
  elements=mesh.elements
  matrix=[]
  ,i,j,mm,mmax=0
  for(i=0i<dofi++) matrix[i]=[]
  for(i=0i<elements.lengthi++):
    elem=elements[i]
    material=model.materials[elem.material],dens=material.dens
    if(elem.isShell):
      sp=model.shellParams[elem.param]
      mm=elem.massMatrix(mesh.getNodes(elem),dens,sp.thickness)
      mmax=setElementMatrix(elem,6,matrix,mm,mmax)
    }
    elif(elem.isBar):
      sect=model.barParams[elem.param].section
      mm=elem.massMatrix(mesh.getNodes(elem),dens,sect)
      mmax=setElementMatrix(elem,6,matrix,mm,mmax)
    }
    else:
      mm=elem.massMatrix(mesh.getNodes(elem),dens)
      mmax=setElementMatrix(elem,3,matrix,mm,mmax)
    }
  }
# 座標変換
  rests=model.bc.restraints
  index=model.bc.nodeIndex,bcdof=model.bc.dof
  for(i=0i<rests.lengthi++):
    ri=rests[i]
    if(ri.coords):
      ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i])
    }
  }
# 絶対値が小さい成分を除去する
  eps=PRECISION*mmax
  for(i=0i<dofi++):
    mrow=matrix[i]
    for(j in mrow):
      if(mrow.hasOwnProperty(j)):
      	j=int(j)
      	if(abs(mrow[j])<eps):
      	  delete mrow[j]
      	}
      }
    }
  }
  return matrix
}

# 剛性マトリックスを作成する
# dof - モデル自由度
function stiffnessMatrix(dof):
  mesh=model.mesh,elements=mesh.elements,matrix=[],i,j,km,kmax=0
  for(i=0i<dofi++) matrix[i]=[]
  for(i=0i<elements.lengthi++):
    elem=elements[i]
    material=model.materials[elem.material],mat=material.matrix
    if(elem.isShell):
      sp=model.shellParams[elem.param]
      if(elem.getName()=='TriElement1'):
      	km=elem.stiffnessMatrix(mesh.getNodes(elem),mat.m2d,sp)
      }
      else:
      	km=elem.stiffnessMatrix(mesh.getNodes(elem),mat.msh,sp)
      }
      kmax=setElementMatrix(elem,6,matrix,km,kmax)
    }
    elif(elem.isBar):
      sect=model.barParams[elem.param].section
      km=elem.stiffnessMatrix(mesh.getNodes(elem),material,sect)
      kmax=setElementMatrix(elem,6,matrix,km,kmax)
    }
    else:
      km=elem.stiffnessMatrix(mesh.getNodes(elem),mat.m3d)
      kmax=setElementMatrix(elem,3,matrix,km,kmax)
    }
  }
# 座標変換
  rests=model.bc.restraints
  index=model.bc.nodeIndex,bcdof=model.bc.dof
  for(i=0i<rests.lengthi++):
    ri=rests[i]
    if(ri.coords):
      ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i])
    }
  }
# 絶対値が小さい成分を除去する
  eps=PRECISION*kmax
  for(i=0i<dofi++):
    mrow=matrix[i]
    for(j in mrow):
      if(mrow.hasOwnProperty(j)):
      	j=int(j)
      	if(abs(mrow[j])<eps):
      	  delete mrow[j]
      	}
      }
    }
  }
  return matrix
}

# 幾何剛性マトリックスを作成する
# dof - モデル自由度
function geomStiffnessMatrix(dof):
  mesh=model.mesh,elements=mesh.elements,nodes=mesh.nodes
  disp=model.result.displacement
  matrix=[],i,j,km,kmax=0,p=[],v=[]
  for(i=0i<dofi++) matrix[i]=[]
  for(i=0i<elements.lengthi++):
    elem=elements[i],en=elem.nodes
    p.length=0
    v.length=0
    for(j=0j<en.lengthj++):
      p[j]=nodes[en[j]]
      v[j]=disp[en[j]]
    }
    material=model.materials[elem.material],mat=material.matrix
    if(elem.isShell):
      sp=model.shellParams[elem.param]
      if(elem.getName()=='TriElement1'):
      	km=elem.geomStiffnessMatrix(p,v,mat.m2d,sp)
      }
      else:
      	km=elem.geomStiffnessMatrix(p,v,mat.msh,sp)
      }
      kmax=setElementMatrix(elem,6,matrix,km,kmax)
    }
    elif(elem.isBar):
      sect=model.barParams[elem.param].section
      km=elem.geomStiffnessMatrix(p,v,material,sect)
      kmax=setElementMatrix(elem,6,matrix,km,kmax)
    }
    else:
      km=elem.geomStiffnessMatrix(p,v,mat.m3d)
      kmax=setElementMatrix(elem,3,matrix,km,kmax)
    }
  }
# 座標変換
  rests=model.bc.restraints
  index=model.bc.nodeIndex,bcdof=model.bc.dof
  for(i=0i<rests.lengthi++):
    ri=rests[i]
    if(ri.coords):
      ri.coords.transMatrix(matrix,dof,index[ri.node],bcdof[i])
    }
  }
# 絶対値が小さい成分を除去・符号反転
  eps=PRECISION*kmax
  for(i=0i<dofi++):
    mrow=matrix[i]
    for(j in mrow):
      if(mrow.hasOwnProperty(j)):
      	j=int(j)
      	if(abs(mrow[j])<eps):
      	  delete mrow[j]
      	}
      	else:
      	  mrow[j]=-mrow[j]
      	}
      }
    }
  }
  return matrix
}

# 要素のマトリックスを設定する
# element - 要素
# dof - 自由度
# matrix - 全体剛性マトリックス
# km - 要素の剛性マトリックス
# kmax - 成分の絶対値の最大値
function setElementMatrix(element,dof,matrix,km,kmax):
  nodeCount=element.nodeCount()
  index=model.bc.nodeIndex,nodes=element.nodes
  for(i=0i<nodeCounti++):
    row0=index[nodes[i]],i0=dof*i
    for(j=0j<nodeCountj++):
      column0=index[nodes[j]],j0=dof*j
      for(i1=0i1<dofi1++):
      	mrow=matrix[row0+i1],krow=km[i0+i1]
      	for(j1=0j1<dofj1++):
      	  cj1=column0+j1
      	  if(cj1 in mrow):
      	    mrow[cj1]+=krow[j0+j1]
      	    kmax=max(kmax,abs(mrow[cj1]))
      	  }
      	  else:
      	    mrow[cj1]=krow[j0+j1]
      	    kmax=max(kmax,abs(mrow[cj1]))
      	  }
      	}
      }
    }
  }
  return kmax
}

# 荷重ベクトルを作成する
# dof - モデル自由度
function loadVector(dof):
  loads=model.bc.loads,press=model.bc.pressures
  vector=numeric.rep([dof],0)
  i,j,index0,index=model.bc.nodeIndex,bcdof=model.bc.dof
  for(i=0i<loads.lengthi++):
    ld=loads[i],nd=ld.node,ldx=ld.globalX,ldof=bcdof[nd]
    index0=index[nd]
    for(j=0j<ldofj++):
      vector[index0+j]=ldx[j]
    }
  }
  for(i=0i<press.lengthi++):
    border=press[i].getBorder
      (model.mesh.elements[press[i].element])
    p=model.mesh.getNodes(border)
    ps=border.shapeFunctionVector(p,press[i].press)
    norm=normalVector(p)
    count=border.nodeCount()
    for(j=0j<countj++):
      index0=index[border.nodes[j]]
      vector[index0]-=ps[j]*norm.x
      vector[index0+1]-=ps[j]*norm.y
      vector[index0+2]-=ps[j]*norm.z
    }
  }
  rests=model.bc.restraints
  for(i=0i<rests.lengthi++):
    ri=rests[i]
    if(ri.coords):
      ri.coords.transVector(vector,dof,index[ri.node],bcdof[i])
    }
  }
  return vector
}

# 伝熱マトリックスを作成する
function heatMatrix():
  elements=model.mesh.elements,mesh=model.mesh
  dof=model.mesh.nodes.length,matrix=[],i
  for(i=0i<dofi++) matrix[i]=[]
  for(i=0i<elements.lengthi++):
    elem=elements[i],count=elem.nodeCount()
    h=model.materials[elem.material].hCon,ls
    if(elem.isShell):
      sp=model.shellParams[elem.param]
      ls=elem.gradMatrix(mesh.getNodes(elem),h,sp)
    }
    elif(elem.isBar):
      sect=model.barParams[elem.param].section
      ls=elem.gradMatrix(mesh.getNodes(elem),h,sect)
    }
    else:
      ls=elem.gradMatrix(mesh.getNodes(elem),h)
    }
    for(i1=0i1<counti1++):
      mrow=matrix[elem.nodes[i1]],lrow=ls[i1]
      for(j1=0j1<countj1++):
      	if(elem.nodes[j1] in mrow):
      	  mrow[elem.nodes[j1]]+=lrow[j1]
      	}
      	else:
      	  mrow[elem.nodes[j1]]=lrow[j1]
      	}
      }
    }
  }
  return matrix
}

# 熱境界条件ベクトルを作成する
# matrix - 伝熱マトリックス
function tempVector(matrix):
  htcs=model.bc.htcs,i
  vector=numeric.rep([model.mesh.nodes.length],0)
  for(i=0i<htcs.lengthi++):
    elem=model.mesh.elements[htcs[i].element]
    border=htcs[i].getBorder(elem)
    p=model.mesh.getNodes(border)
    h=htcs[i].htc
    if(border.isEdge):
      sp=model.shellParams[elem.param]
      h*=sp.thickness
    }
    hm=border.shapeFunctionMatrix(p,h)
    hv=border.shapeFunctionVector(p,h*htcs[i].outTemp)
    count=border.nodeCount()
    for(i1=0i1<counti1++):
      mrow=matrix[border.nodes[i1]],hrow=hm[i1]
      for(j1=0j1<countj1++):
      	if(border.nodes[j1] in mrow):
      	  mrow[border.nodes[j1]]+=hrow[j1]
      	}
      	else:
      	  mrow[border.nodes[j1]]=hrow[j1]
      	}
      }
      vector[border.nodes[i1]]+=hv[i1]
    }
  }
  return vector
}

# 行列の行から一部を抽出する
# mrow - 元のマトリックスの行データ
# list - 抽出部分のリスト
function extructRow(mrow,list):
  exrow=[],col=[],i1=0,j1=0
  for(j in mrow):
    if(mrow.hasOwnProperty(j)):
      col.append(int(j))
    }
  }
  col.sort(function(j1,j2):return j1-j2})
  while((i1<col.length) and (j1<list.length)):
    if(col[i1]==list[j1]):
      exrow[j1]=mrow[col[i1]]
      i1++
      j1++
    }
    elif(col[i1]<list[j1]):
      i1++
    }
    else:
      j1++
    }
  }
  return exrow
}

# 計算を開始する
function calcStart():
  info.textContent='計算中・・・'
  elems=document.getElementsByName('method')
  if(elems[0].checked):
    model.solver.method=LU_METHOD
  }
  elif(elems[1].checked):
    model.solver.method=ILUCG_METHOD
  }
  elems=document.getElementsByName('restype')
  if(elems[0].checked):
    model.result.type=NODE_DATA
  }
  elif(elems[1].checked):
    model.result.type=ELEMENT_DATA
  }
  hideModalWindow(CALC_WINDOW)
  elems=document.getElementsByName('calctype')
  if(elems[0].checked):
    setTimeout(statCalcStart,10)
  }
  elif(elems[1].checked):
    setTimeout(vibCalcStart,10)
  }
  else:
    setTimeout(buckCalcStart,10)
  }
}

# 静解析の計算を開始する
function statCalcStart():
  try:
    model.calculate()
    resultView.setInitStatic()
    showInfo()
  }
  catch(ex):
    alert(ex)
  }
}

# 固有振動解析の計算を開始する
function vibCalcStart():
  try:
    count=int(document.getElementById('eigennumber').value)
    model.charVib(count)
    resultView.setInitEigen()
  }
  catch(ex):
    alert(ex)
  }
}

# 線形座屈解析の計算を開始する
function buckCalcStart():
#  try:
    count=int(document.getElementById('eigennumber').value)
    model.calcBuckling(count)
    resultView.setInitEigen()
/*  }
  catch(ex):
    alert(ex)
  }*/
}

# 計算設定ウィンドウを表示する
function showCalc():
  showModalWindow(CALC_WINDOW)
  elems=document.getElementsByName('method')
  elems[model.solver.method].checked=True
}

# 計算設定を取り消す
function calcCancel():
  hideModalWindow(CALC_WINDOW)
}



#--------------------------------------------------------------------#
# 連立方程式求解オブジェクト
class Solver():
  def __init__(self):
    self.matrix=[]		# 行列
    self.matrix2=[]		# 第２行列
    self.vector=[]		# ベクトル
    self.dof=0			# モデル自由度
    self.method=LU_METHOD	# 方程式解法

# データを消去する
def clear():
  self.matrix.length=0
  self.matrix2.length=0
  self.vector.length=0
  self.dof=0
}

  # 剛性マトリックス・荷重ベクトルを作成する
  def createStiffnessMatrix():
    i,bc=model.bc,bcList=bc.bcList,reducedList=[]
    for(i=0i<bcList.lengthi++):
      if(bcList[i]<0):
        reducedList.append(i)
      }
    }

    # 剛性マトリックス・荷重ベクトルの作成
    matrix1=stiffnessMatrix(self.dof),vector1=loadVector(self.dof)

    # 拘束自由度を除去する
    for(i=0i<bcList.lengthi++):
      if(bcList[i]>=0):
        rx=bc.getRestDisp(bcList[i])
        for(j=0j<vector1.lengthj++):
          if(i in matrix1[j]):
            vector1[j]-=rx*matrix1[j][i]
          }
        }
      }
    }
    self.extruct(matrix1,vector1,reducedList)
  }

  # 剛性マトリックス・質量マトリックスを作成する
  def createStiffMassMatrix():
    i,bc=model.bc,bcList=bc.bcList,reducedList=[]
    for(i=0i<bcList.lengthi++):
      if(bcList[i]<0):
        reducedList.append(i)
      }
    }
    matrix1=stiffnessMatrix(self.dof),matrix2=massMatrix(self.dof)

    self.matrix.length=0
    self.matrix2.length=0
    for(i=0i<reducedList.lengthi++):
      self.matrix[i]=extructRow(matrix1[reducedList[i]],reducedList)
      self.matrix2[i]=extructRow(matrix2[reducedList[i]],reducedList)
    }
  }

  # 幾何剛性マトリックスを作成する
  def createGeomStiffMatrix():
    i,bc=model.bc,bcList=bc.bcList,reducedList=[]
    for(i=0i<bcList.lengthi++):
      if(bcList[i]<0):
        reducedList.append(i)
      }
    }
    matrix2=geomStiffnessMatrix(self.dof)

    self.matrix2.length=0
    for(i=0i<reducedList.lengthi++):
      self.matrix2[i]=extructRow(matrix2[reducedList[i]],reducedList)
    }
  }

  # 熱計算のマトリックス・ベクトルを計算する
  def createHeatMatrix():
    i,bcList=model.bc.bcList,reducedList=[]
    for(i=0i<bcList.lengthi++):
      if(bcList[i]<0):
        reducedList.append(i)
      }
    }

  # 伝熱マトリックス・熱境界条件ベクトルの作成
    matrix1=heatMatrix(),vector1=tempVector(matrix1)

  # 拘束自由度を除去する
    for(i=0i<bcList.lengthi++):
      if(bcList[i]>=0):
        t=model.bc.temperature[bcList[i]]
        for(j=0j<vector1.lengthj++):
          if(i in matrix1[j]):
            vector1[j]-=t.t*matrix1[j][i]
          }
        }
      }
    }
    self.extruct(matrix1,vector1,reducedList)
  }

  # 行列の一部を抽出する
  # matrix1,vector1 - 元のマトリックス,ベクトル
  # list - 抽出部分のリスト
  def extruct(matrix1,vector1,list):
    self.matrix.length=0
    self.vector.length=0
    for(i=0i<list.lengthi++):
      self.vector[i]=vector1[list[i]]
      self.matrix[i]=extructRow(matrix1[list[i]],list)
    }
  }

  # 連立方程式を解く
  def solve():
    switch(self.method):
      case LU_METHOD:
        a=numeric.ccsSparse(self.matrix)
        return numeric.ccsLUPSolve(numeric.ccsLUP(a),self.vector)
      case ILUCG_METHOD:
        return solveILU(toSparse(self.matrix),getILU(self.matrix),
                        self.vector)
    }
  }

  # ランチョス法で固有値・固有ベクトルを求める
  # n - ３重対角化行列の大きさ
  # delta - シフト量δ
  def eigenByLanczos(n,delta):
    switch(self.method):
      case LU_METHOD:
        return eigByLanczosLUP(self.matrix,self.matrix2,n,delta)
      case ILUCG_METHOD:
        return eigByLanczosILUCG(self.matrix,self.matrix2,n,delta)
    }
  }

  # アーノルディ法で固有値・固有ベクトルを求める
  # n - ３重対角化行列の大きさ
  # delta - シフト量δ
  def eigenByArnoldi(n,delta):
    switch(self.method):
      case LU_METHOD:
        return eigByArnoldiLUP(self.matrix,self.matrix2,n,delta)
      case ILUCG_METHOD:
        return eigByArnoldiILUCG(self.matrix,self.matrix2,n,delta)
    }
  }
