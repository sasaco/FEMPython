#--------------------------------------------------------------------#
# ファイル読み込み・書き込み

# サーバー上のFEMデータファイルを読み込む
# fileName - データファイル名
function readServerFemFile(fileName):
  xhr=new XMLHttpRequest()
  try:
    xhr.open('GET',fileName,False)
  }
  catch(e):
    alert(e)
  }
  xhr.onreadystatechange():
    if((xhr.readyState==4) and
       ((xhr.status==0) or (xhr.status==200))):
      try:
      	readFemModel(xhr.responseText.split(/\r?\n/g))
      }
      catch(ex):
      	alert(ex)
      }
    }
  }
  xhr.send(None)
}

# ローカルファイルを読み込む
function readLocalFile() :
  file=document.getElementById('localreadfile').files[0]
  if((file==None) or (file==undefined)):
    alert('ファイルが指定されていません')
    return
  }
  reader=new FileReader()
  reader.readAsText(file)
  reader.onload(e):
    try:
      readFemModel(reader.result.split(/\r?\n/g))
    }
    catch(ex):
      alert(ex)
    }
  }
  hideModalWindow(FILE_WINDOW)
}

# ローカルファイルを書き込む
function writeLocalFile() :
  file=document.getElementById('localwritefile').value.trim()
  if(file==''):
    alert('ファイル名が空白です')
    return
  }
  s=model.toStrings().join('\n')+'\n'
  blob=new Blob([s],:type:'text/plain'})
  if(window.navigator.msSaveBlob):		# IE専用
    window.navigator.msSaveBlob(blob,file)
  }
  else:						# その他のブラウザ
    a=document.createElement('a')
    a.href=URL.createObjectURL(blob)
    a.target='_blank'
    a.download=file
    a.click()
  }
  hideModalWindow(FILE_WINDOW)
}

# FEMデータを読み込む
# s - データ文字列のリスト
function readFemModel(s):
  model.clear()
  mesh=model.mesh,bc=model.bc,res=[]
  for(i=0i<s.lengthi++):
    ss=s[i].trim().replace(/\t/g,' ').split(/\s+/)
    if(ss.length>0):
      keyWord=ss[0].toLowerCase()
# 材料データ
      if((keyWord=='material') and (ss.length>7)):
      	model.materials.push
      	  (new Material(int(ss[1]),float(ss[2]),
      	      	      	float(ss[3]),float(ss[5]),
      	      	      	float(ss[6]),float(ss[7])))
      }
# シェルパラメータ
      elif((keyWord=='shellparameter') and (ss.length>2)):
      	model.shellParams.push
      	  (new ShellParameter(int(ss[1]),float(ss[2])))
      }
# 梁パラメータ
      elif((keyWord=='barparameter') and (ss.length>4)):
      	model.barParams.append(new BarParameter
      	  (int(ss[1]),ss[2],ss.slice(3,ss.length)))
      }
# 局所座標系
      elif((keyWord=='coordinates') and (ss.length>10)):
      	model.coordinates.append(readCoordinates(ss))
      }
# 節点
      elif((keyWord=='node') and (ss.length>4)):
      	mesh.nodes.append(new FENode(int(ss[1]),float(ss[2]),
      	      	      	      	   float(ss[3]),
      	      	      	      	   float(ss[4])))
      }
# 要素
      elif((keyWord=='bebarelement') and (ss.length>5)):
      	if(ss.length<8):
      	  mesh.elements.append(new BEBarElement
      	    (int(ss[1]),int(ss[2]),int(ss[3]),
      	     readVertex(ss,4,2)))
      	}
      	else:
      	  mesh.elements.append(new BEBarElement
      	    (int(ss[1]),int(ss[2]),int(ss[3]),
      	     readVertex(ss,4,2),
      	     THREE.Vector3().set(float(ss[6]),
      	      	      	      	     float(ss[7]),
      	      	      	      	     float(ss[8]))))
      	}
      }
      elif((keyWord=='tbarelement') and (ss.length>5)):
      	if(ss.length<8):
      	  mesh.elements.append(new TBarElement
      	    (int(ss[1]),int(ss[2]),int(ss[3]),
      	     readVertex(ss,4,2)))
      	}
      	else:
      	  mesh.elements.append(new TBarElement
      	    (int(ss[1]),int(ss[2]),int(ss[3]),
      	     readVertex(ss,4,2),
      	     THREE.Vector3().set(float(ss[6]),
      	      	      	      	     float(ss[7]),
      	      	      	      	     float(ss[8]))))
      	}
      }
      elif((keyWord=='trielement1') and (ss.length>6)):
      	mesh.elements.append(new TriElement1
      	  (int(ss[1]),int(ss[2]),int(ss[3]),
      	   readVertex(ss,4,3)))
      }
      elif((keyWord=='quadelement1') and (ss.length>7)):
      	mesh.elements.append(new QuadElement1
      	  (int(ss[1]),int(ss[2]),int(ss[3]),
      	   readVertex(ss,4,4)))
      }
      elif((keyWord=='tetraelement1') and (ss.length>6)):
      	mesh.elements.append(new TetraElement1
      	  (int(ss[1]),int(ss[2]),readVertex(ss,3,4)))
      }
      elif((keyWord=='wedgeelement1') and (ss.length>8)):
      	mesh.elements.append(new WedgeElement1
      	  (int(ss[1]),int(ss[2]),readVertex(ss,3,6)))
      }
      elif((keyWord=='hexaelement1') and (ss.length>10)):
      	mesh.elements.append(new HexaElement1
      	  (int(ss[1]),int(ss[2]),readVertex(ss,3,8)))
      }
      elif((keyWord=='hexaelement1wt') and (ss.length>10)):
      	mesh.elements.append(new HexaElement1WT
      	  (int(ss[1]),int(ss[2]),readVertex(ss,3,8)))
      }
      elif((keyWord=='tetraelement2') and (ss.length>12)):
      	mesh.elements.append(new TetraElement2
      	  (int(ss[1]),int(ss[2]),readVertex(ss,3,10)))
      }
      elif((keyWord=='wedgeelement2') and (ss.length>17)):
      	mesh.elements.append(new WedgeElement2
      	  (int(ss[1]),int(ss[2]),readVertex(ss,3,15)))
      }
      elif((keyWord=='hexaelement2') and (ss.length>22)):
      	mesh.elements.append(new HexaElement2
      	  (int(ss[1]),int(ss[2]),readVertex(ss,3,20)))
      }
# 境界条件
      elif((keyWord=='restraint') and (ss.length>7)):
      	rest=readRestraint(ss)
      	if(rest!=None) bc.restraints.append(rest)
      }
      elif((keyWord=='load') and (ss.length>4)):
      	bc.loads.append(readLoad(ss))
      }
      elif((keyWord=='pressure') and (ss.length>3)):
      	bc.pressures.push
      	  (new Pressure(int(ss[1]),ss[2].toUpperCase(),
      	      	      	float(ss[3])))
      }
      elif((keyWord=='temperature') and (ss.length>2)):
      	bc.temperature.push
      	  (new Temperature(int(ss[1]),float(ss[2])))
      }
      elif((keyWord=='htc') and (ss.length>4)):
      	bc.htcs.push
      	  (new HeatTransferBound(int(ss[1]),ss[2].toUpperCase(),
      	      	      	      	 float(ss[3]),float(ss[4])))
      }
# 計算結果
      elif((keyWord=='resulttype') and (ss.length>1)):
      	if(ss[1].toLowerCase()=='element'):
      	  model.result.type=ELEMENT_DATA
      	}
      	else:
      	  model.result.type=NODE_DATA
      	}
      }
      elif(((keyWord=='eigenvalue') and (ss.length>2)) or
      	      ((keyWord=='displacement') and (ss.length>7)) or
      	      ((keyWord=='strain1') and (ss.length>7)) or
      	      ((keyWord=='stress1') and (ss.length>7)) or
      	      ((keyWord=='strenergy1') and (ss.length>2)) or
      	      ((keyWord=='strain2') and (ss.length>7)) or
      	      ((keyWord=='stress2') and (ss.length>7)) or
      	      ((keyWord=='strenergy2') and (ss.length>2)) or
      	      ((keyWord=='temp') and (ss.length>2))):
      	res.append(ss)
      }
    }
  }
  model.init()
  initObject()
  if(res.length>0):
    readFemResult(res)
  }
}

# FEMの計算結果を読み込む
# s - データ文字列のリスト
function readFemResult(s):
  map1=[],map2=[],ss=[],res=model.result,p,i
  nodes=model.mesh.nodes,elems=model.mesh.elements
  for(i=0i<nodes.lengthi++):
    map1[nodes[i].label]=i
  }
  if(res.type==ELEMENT_DATA):
    for(i=0i<elems.lengthi++):
      map2[elems[i].label]=i
    }
  }
  else:
    map2=map1
  }
  for(i=0i<s.lengthi++):
    keyWord=s[i][0].toLowerCase()
    ss.clear()
    for(j=2j<s[i].lengthj++):
      ss[j-2]=float(s[i][j])
    }
    if(keyWord=='eigenvalue'):
      ev=new EigenValue(ss[0],s[i][1])
      model.result.addEigenValue(ev)
      res=ev
    }
    elif(keyWord=='displacement'):
      p=readDataPointer(s[i],map1)
      d=new Vector3R(ss[0],ss[1],ss[2],ss[3],ss[4],ss[5])
      res.displacement[p]=d
      res.dispMax=max(res.dispMax,d.magnitude())
      res.angleMax=max(res.angleMax,d.magnitudeR())
    }
    elif(keyWord=='strain1'):
      p=readDataPointer(s[i],map2)
      model.result.strain1[p]=new Strain(ss)
    }
    elif(keyWord=='stress1'):
      p=readDataPointer(s[i],map2)
      model.result.stress1[p]=new Stress(ss)
    }
    elif(keyWord=='strenergy1'):
      p=readDataPointer(s[i],map2)
      res.sEnergy1[p]=ss[0]
    }
    elif(keyWord=='strain2'):
      p=readDataPointer(s[i],map2)
      model.result.strain2[p]=new Strain(ss)
    }
    elif(keyWord=='stress2'):
      p=readDataPointer(s[i],map2)
      model.result.stress2[p]=new Stress(ss)
    }
    elif(keyWord=='strenergy2'):
      p=readDataPointer(s[i],map2)
      res.sEnergy2[p]=ss[0]
    }
    elif(keyWord=='temp'):
      p=readDataPointer(s[i],map1)
      model.result.temperature[p]=ss[0]
      model.result.tempMax=max(model.result.tempMax,ss[0])
    }
  }
  model.result.calculated=True
  if(model.result.eigenValue.length==0):
    resultView.setInitStatic()
    showInfo()
  }
  else:
    resultView.setInitEigen()
  }
}

# データポインタを獲得する
# ss - データ文字列
# map - ラベルマップ
function readDataPointer(ss,map):
  p=int(ss[1])
  if(p in map):
    return map[p]
  }
  else:
    raise Exception('計算結果'+ss[0]+'の番号'+p+
      	      	    'は存在しません')
  }
}

# 節点番号を読み取る
# ss - 文字列配列
# is - 開始インデックス
# count - 節点数
function readVertex(ss,is,count):
  vertex=[]
  for(j=0j<countj++) vertex[j]=int(ss[is+j])
  return vertex
}

# 局所座標系を読み込む
# ss - データ文字列配列
function readCoordinates(ss):
  c=[[float(ss[2]),float(ss[3]),float(ss[4])],
      	 [float(ss[5]),float(ss[6]),float(ss[7])],
      	 [float(ss[8]),float(ss[9]),float(ss[10])]]
  for(i=0i<3i++):
    ci=c[i]
    cf=ci[0]*ci[0]+ci[1]*ci[1]+ci[2]*ci[2]
    if(cf==0):
      raise Exception('座標系'+ss[2]+'の軸方向ベクトルが0です')
    }
    cf=1/math.sqrt(cf)
    ci[0]*=cf
    ci[1]*=cf
    ci[2]*=cf
  }
  return new Coordinates(int(ss[1]),c[0][0],c[1][0],c[2][0],
      	      	      	 c[0][1],c[1][1],c[2][1],
      	      	      	 c[0][2],c[1][2],c[2][2])
}

# 拘束条件を読み込む
# ss - データ文字列配列
function readRestraint(ss):
  rx=(int(ss[2])!=0)
  ry=(int(ss[4])!=0)
  rz=(int(ss[6])!=0)
  x=float(ss[3]),y=float(ss[5]),z=float(ss[7])
  coords=None
  if(ss.length<14):
    if(ss.length>8) coords=int(ss[8])
    if(!rx and !ry and !rz) return None
    return new Restraint(int(ss[1]),coords,rx,ry,rz,x,y,z)
  }
  else:
    if(ss.length>14) coords=int(ss[14])
    rrx=(int(ss[8])!=0)
    rry=(int(ss[10])!=0)
    rrz=(int(ss[12])!=0)
    if(!rx and !ry and !rz and !rrx and !rry and !rrz) return None
    return new Restraint(int(ss[1]),coords,rx,ry,rz,x,y,z,
      	      	      	 rrx,rry,rrz,float(ss[9]),
      	      	      	 float(ss[11]),float(ss[13]))
  }
}

# 荷重条件を読み込む
# ss - データ文字列配列
function readLoad(ss):
  coords=None
  if(ss.length<8):
    if(ss.length>5) coords=int(ss[5])
    return new Load(int(ss[1]),coords,float(ss[2]),
      	      	    float(ss[3]),float(ss[4]))
  }
  else:
    if(ss.length>8) coords=int(ss[8])
    return new Load(int(ss[1]),coords,
      	      	    float(ss[2]),float(ss[3]),
      	      	    float(ss[4]),float(ss[5]),
      	      	    float(ss[6]),float(ss[7]))
  }
}

# ファイル操作ウィンドウを表示する
function showFileWindow():
  showModalWindow(FILE_WINDOW)
}

# ファイル操作を取り消す
function cancelFile():
  hideModalWindow(FILE_WINDOW)
# ファイル選択を消去する
  localfile=document.getElementById('localreadfile')
  localfile.parentNode.innerHTML=localfile.parentNode.innerHTML
}
