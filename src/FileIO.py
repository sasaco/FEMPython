from typing import List, Tuple, Dict, Set

import numpy as np
import math

from FemDataModel import FemDataModel, FENode
from Material import Material, ShellParameter, BarParameter
from BarElement import BEBarElement, TBarElement
from ShellElement import TriElement1, QuadElement1
from SolidElement import TetraElement1, WedgeElement1, HexaElement1, TetraElement2, WedgeElement2, HexaElement2
from AdvancedElement import HexaElement1WT
from BoundaryCondition import Pressure, Temperature, HeatTransferBound, Restraint, Load
from Coordinates import Coordinates
from Result import ELEMENT_DATA, NODE_DATA
# from Vector3 import Vector3
#--------------------------------------------------------------------#
# ファイル読み込み・書き込み

# サーバー上のFEMデータファイルを読み込む
# fileName - データファイル名
def readServerFemFile(fileName, model):
	f = open(fileName, encoding="utf-8")
	fstr = f.read()  # ファイル終端まで全て読んだデータを返す
	f.close()
	readFemModel(fstr, model)


# FEMデータを読み込む
# s - データ文字列のリスト
def readFemModel(s: List[str], model: FemDataModel):
	model.clear()
	mesh=model.mesh
	bc=model.bc
	# res=[]
	for i in range(len(s)):
		ss: List[str] = s[i].strip().split(' ')
		if len(ss)>0:
			keyWord = ss[0].lower()
		# 材料データ
		if keyWord=='material' and len(ss)>7:
			model.materials.append(
				Material(int(ss[1]),float(ss[2]),
					float(ss[3]),float(ss[5]),
					float(ss[6]),float(ss[7])))
		# シェルパラメータ
		elif keyWord=='shellparameter' and len(ss)>2:
			model.shellParams.append(
				ShellParameter(int(ss[1]),float(ss[2])))
		# 梁パラメータ
		elif keyWord=='barparameter' and len(ss)>4:
			model.barParams.append(BarParameter(
				int(ss[1]),ss[2],ss[3:len(ss)]))
		# 局所座標系
		elif keyWord=='coordinates' and len(ss)>10:
			model.coordinates.append(readCoordinates(ss))

		# 節点
		elif keyWord=='node' and len(ss)>4:
			mesh.nodes.append(FENode(int(ss[1]),float(ss[2]),
									float(ss[3]),
									float(ss[4])))
		# 要素
		elif keyWord=='bebarelement' and len(ss)>5:
			if(len(ss)<8):
				mesh.elements.append(BEBarElement
					(int(ss[1]),int(ss[2]),int(ss[3]),
					readVertex(ss,4,2)))
			else:
				mesh.elements.append(BEBarElement
					(int(ss[1]),int(ss[2]),int(ss[3]),
					readVertex(ss,4,2),
					np.array([float(ss[6]),float(ss[7]),float(ss[8])])))

		elif keyWord=='tbarelement' and len(ss)>5:
			if(len(ss)<8):
				mesh.elements.append(TBarElement
					(int(ss[1]),int(ss[2]),int(ss[3]),
					readVertex(ss,4,2)))
			else:
				mesh.elements.append(TBarElement(
					int(ss[1]), int(ss[2]), int(ss[3]),
					readVertex(ss, 4, 2),
					np.array([float(ss[6]),float(ss[7]),float(ss[8])])
					))

		elif keyWord=='trielement1' and len(ss)>6:
			mesh.elements.append(TriElement1
			(int(ss[1]),int(ss[2]),int(ss[3]),
			readVertex(ss,4,3)))

		elif keyWord=='quadelement1' and len(ss)>7:
			mesh.elements.append(QuadElement1
			(int(ss[1]),int(ss[2]),int(ss[3]),
			readVertex(ss,4,4)))

		elif keyWord=='tetraelement1' and len(ss)>6:
			mesh.elements.append(TetraElement1
			(int(ss[1]),int(ss[2]),readVertex(ss,3,4)))

		elif keyWord=='wedgeelement1' and len(ss)>8:
			mesh.elements.append(WedgeElement1
			(int(ss[1]),int(ss[2]),readVertex(ss,3,6)))

		elif keyWord=='hexaelement1' and len(ss)>10:
			mesh.elements.append(HexaElement1
			(int(ss[1]),int(ss[2]),readVertex(ss,3,8)))

		elif keyWord=='hexaelement1wt' and len(ss)>10:
			mesh.elements.append(HexaElement1WT
			(int(ss[1]),int(ss[2]),readVertex(ss,3,8)))

		elif keyWord=='tetraelement2' and len(ss)>12:
			mesh.elements.append(TetraElement2
			(int(ss[1]),int(ss[2]),readVertex(ss,3,10)))

		elif keyWord=='wedgeelement2' and len(ss)>17:
			mesh.elements.append(WedgeElement2
			(int(ss[1]),int(ss[2]),readVertex(ss,3,15)))

		elif keyWord=='hexaelement2' and len(ss)>22:
			mesh.elements.append(HexaElement2
			(int(ss[1]),int(ss[2]),readVertex(ss,3,20)))

		# 境界条件
		elif keyWord=='restraint' and len(ss)>7:
			rest=readRestraint(ss)
			if rest!=None:
				bc.restraints.append(rest)

		elif keyWord=='load' and len(ss)>4:
			bc.loads.append(readLoad(ss))

		elif keyWord=='pressure' and len(ss)>3:
			bc.pressures.append(
				Pressure(int(ss[1]),ss[2].upper(), float(ss[3])))

		elif keyWord=='temperature' and len(ss)>2:
			bc.temperature.append(
				Temperature(int(ss[1]),float(ss[2])))

		elif keyWord=='htc' and len(ss)>4:
			bc.htcs.append(
				HeatTransferBound(int(ss[1]),ss[2].upper(),
									float(ss[3]),float(ss[4])))

		# 計算結果
		elif keyWord=='resulttype' and len(ss)>1:
			if(ss[1].lower()=='element'):
				model.result.type = ELEMENT_DATA
			else:
				model.result.type = NODE_DATA

		# elif (keyWord=='eigenvalue' and len(ss)>2) or \
		# 		(keyWord=='displacement' and len(ss)>7) or \
		# 		(keyWord=='strain1' and len(ss)>7) or \
		# 		(keyWord=='stress1' and len(ss)>7) or \
		# 		(keyWord=='strenergy1' and len(ss)>2) or \
		# 		(keyWord=='strain2' and len(ss)>7) or \
		# 		(keyWord=='stress2' and len(ss)>7) or \
		# 		(keyWord=='strenergy2' and len(ss)>2) or \
		# 		(keyWord=='temp' and len(ss)>2):
		# 	res.append(ss)

	model.init()

	# if len(res)>0:
	# 	readFemResult(res)


# 拘束条件を読み込む
# ss - データ文字列配列
def readRestraint(ss: List[str]) -> Restraint:
	rx = int(ss[2]) != 0
	ry = int(ss[4]) != 0
	rz = int(ss[6]) != 0
	x = float(ss[3])
	y = float(ss[5])
	z = float(ss[7])
	coords = None
	if len(ss) < 14:
		if len(ss) > 8:
			coords = int(ss[8])
		if rx==False and ry==False and rz==False:
			return None
		return Restraint(int(ss[1]), coords, rx, ry, rz, x, y, z)

	else:
		if len(ss)>14:
			coords=int(ss[14])
		rrx= int(ss[8]) != 0
		rry= int(ss[10]) != 0
		rrz= int(ss[12]) != 0
		if rx==False and ry==False and rz==False and \
			rrx==False and rry==False and rrz==False:
			return None
		return Restraint(int(ss[1]),coords,rx,ry,rz,x,y,z,
							rrx,rry,rrz,float(ss[9]),
							float(ss[11]),float(ss[13]))

# 荷重条件を読み込む
# ss - データ文字列配列
def readLoad(ss) -> Load:
	coords=None
	if(len(ss)<8):
		if(len(ss)>5):
			coords=int(ss[5])
		return Load(int(ss[1]),coords,float(ss[2]),
					float(ss[3]),float(ss[4]))
	else:
		if(len(ss)>8):
			coords=int(ss[8])
		return Load(int(ss[1]),coords,
					float(ss[2]),float(ss[3]),
					float(ss[4]),float(ss[5]),
					float(ss[6]),float(ss[7]))

# 節点番号を読み取る
# ss - 文字列配列
# is - 開始インデックス
# count - 節点数
def readVertex(ss: List[str], i: int, count: int) -> List[int]:
	vertex=[]
	for j in range(count):
		vertex[j]=int(ss[i+j])
	return vertex

# 局所座標系を読み込む
# ss - データ文字列配列
def readCoordinates(ss: List[str]) -> Coordinates:
	c=[[float(ss[2]),float(ss[3]),float(ss[4])],
			[float(ss[5]),float(ss[6]),float(ss[7])],
			[float(ss[8]),float(ss[9]),float(ss[10])]]
	for i in range(3):
		ci=c[i]
		cf=ci[0]*ci[0]+ci[1]*ci[1]+ci[2]*ci[2]
		if(cf==0):
			raise Exception('座標系'+ss[2]+'の軸方向ベクトルが0です')
		cf=1/math.sqrt(cf)
		ci[0]*=cf
		ci[1]*=cf
		ci[2]*=cf

	return Coordinates(int(ss[1]),c[0][0],c[1][0],c[2][0],
							c[0][1],c[1][1],c[2][1],
							c[0][2],c[1][2],c[2][2])


# # FEMの計算結果を読み込む
# # s - データ文字列のリスト
# def readFemResult(s):
#   map1=[],map2=[],ss=[],res=model.result,p,i
#   nodes=model.mesh.nodes,elems=model.mesh.elements
#   for(i=0i<nodes.lengthi++):
#     map1[nodes[i].label]=i
#   }
#   if(res.type==ELEMENT_DATA):
#     for(i=0i<elems.lengthi++):
#       map2[elems[i].label]=i
#     }
#   }
#   else:
#     map2=map1
#   }
#   for(i=0i<s.lengthi++):
#     keyWord=s[i][0].lower()
#     ss.clear()
#     for(j=2j<s[i].lengthj++):
#       ss[j-2]=float(s[i][j])
#     }
#     if(keyWord=='eigenvalue'):
#       ev=new EigenValue(ss[0],s[i][1])
#       model.result.addEigenValue(ev)
#       res=ev
#     }
#     elif(keyWord=='displacement'):
#       p=readDataPointer(s[i],map1)
#       d=new Vector3R(ss[0],ss[1],ss[2],ss[3],ss[4],ss[5])
#       res.displacement[p]=d
#       res.dispMax=max(res.dispMax,d.magnitude())
#       res.angleMax=max(res.angleMax,d.magnitudeR())
#     }
#     elif(keyWord=='strain1'):
#       p=readDataPointer(s[i],map2)
#       model.result.strain1[p]=new Strain(ss)
#     }
#     elif(keyWord=='stress1'):
#       p=readDataPointer(s[i],map2)
#       model.result.stress1[p]=new Stress(ss)
#     }
#     elif(keyWord=='strenergy1'):
#       p=readDataPointer(s[i],map2)
#       res.sEnergy1[p]=ss[0]
#     }
#     elif(keyWord=='strain2'):
#       p=readDataPointer(s[i],map2)
#       model.result.strain2[p]=new Strain(ss)
#     }
#     elif(keyWord=='stress2'):
#       p=readDataPointer(s[i],map2)
#       model.result.stress2[p]=new Stress(ss)
#     }
#     elif(keyWord=='strenergy2'):
#       p=readDataPointer(s[i],map2)
#       res.sEnergy2[p]=ss[0]
#     }
#     elif(keyWord=='temp'):
#       p=readDataPointer(s[i],map1)
#       model.result.temperature[p]=ss[0]
#       model.result.tempMax=max(model.result.tempMax,ss[0])
#     }
#   }
#   model.result.calculated=True
#   if(model.result.eigenValue.length==0):
#     resultView.setInitStatic()
#     showInfo()
#   }
#   else:
#     resultView.setInitEigen()
#   }
# }

# # データポインタを獲得する
# # ss - データ文字列
# # map - ラベルマップ
# def readDataPointer(ss,map):
#   p=int(ss[1])
#   if(p in map):
#     return map[p]
#   }
#   else:
#     raise Exception('計算結果'+ss[0]+'の番号'+p+
#       	      	    'は存在しません')
#   }
# }

# # ファイル操作ウィンドウを表示する
# def showFileWindow():
#   showModalWindow(FILE_WINDOW)
# }

# # ファイル操作を取り消す
# def cancelFile():
#   hideModalWindow(FILE_WINDOW)
# # ファイル選択を消去する
#   localfile=document.getElementById('localreadfile')
#   localfile.parentNode.innerHTML=localfile.parentNode.innerHTML
# }
