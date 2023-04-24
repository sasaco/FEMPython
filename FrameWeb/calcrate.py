# ======================
# 3D Frame Analysis
# ======================
import numpy as np
from collections import OrderedDict

from scipy.sparse.linalg import spsolve
from scipy.sparse import csr_matrix


from matrixBar import barMatrix
from matrixShell import shellMatrix
from matrixG import gMatrix

from dataInput import dataInput
from dataManager import dataManager


class FrameCalc:

    def __init__(self, _inp):
        self.inp = dataManager(_inp)

        # 全ての荷重ケースの解析を開始する
    def calcrate(self):

        result = dict() # 結果を格納するdictionary

        # calcration
        for id in self.inp.loadList:
            error, res = self.calcLinear3D(id)
            if error != None:
                return error, id
            result[id] = res
        return None, result


    # ターゲットケースの情報をセットする
    def calcLinear3D(self, caseID: str, debug=False):

        # ターゲットケースの基本情報をセットする
        error = self.inp.setTargetData(caseID)
        if error != None:
            return error, None


        ### core ########################################################
        
        from main import FEMPython

        calc = FEMPython()

        #materials = []
        #nodes = [] 
        #elements = []
        #shellParams = []
        #barParams = []
        #coordinates = []
        #pressures = []
        #temperature = []
        #htcs = []
        #restraints = []
        #loads = []

        ## 材料データ
        #mat = calc.core.Material(int(columns[1]), float(columns[2]),
        #    float(columns[3]), float(columns[5]),
        #    float(columns[6]), float(columns[7]))
        #materials.append(mat)

        ## シェルパラメータ
        #shell = calc.core.ShellParameter(int(columns[1]), float(columns[2]))
        #shellParams.append(shell)

        ## 梁パラメータ
        #bar = calc.core.BarParameter(int(columns[1]), columns[2], columns[3:])
        #barParams.append(bar)

        ## 局所座標系
        #cord = calc.core.readCoordinates(columns)
        #coordinates.append(cord)

        # 節点
        for ID in self.inp.node:
            node = self.inp.node[ID]
            calc.addNode(ID, node['x'], node['y'], node['z'])
        
        ## 要素
        # Bar部材の登録
        for ID in self.inp.member:
            member = self.inp.member[ID]
            joint_i = 100*int(member['xi']) + 10*int(member['yi']) + int(member['zi'])
            joint_j = 100*int(member['xj']) + 10*int(member['yj']) + int(member['zj'])
            calc.addElement("barelement", ID, member['ni'],  member['nj'], member['e'], member['cg'], joint_i, joint_j)


        # Shell部材の登録


        #elements.append(calc.core.ElementManager("bebarelement", columns))
        
        ## 境界条件
        #rest = calc.core.Restraint(columns)
        #restraints.append(rest)

        ## 荷重
        #load = calc.core.Load(columns)
        #loads.append(load)

        ## 温度
        #temp = calc.core.Temperature(int(columns[1]), float(columns[2]))
        #temperature.append(temp)


        calc.init()

        #result = calc.calculate()
        
        #################################################################




        # 節点に剛性マトリックスのインデックスを割り当てる
        self.nodeIndex = self.reNumbering()

        # 全体剛性マトリックス
        gmat = gMatrix(self.inp, self.nodeIndex)

        # X, LU = torch.solve(fp, gk)
        d = spsolve(gmat.gk, gmat.fp, use_umfpack=True)
        # result = np.linalg.solve(gmat.gk, fmat.fp)

        # 解析結果を集計する
        disg, reac, fsec, shell_fsec = self.getResultData(gmat, d)

        return None, self.inp.getResultData(gmat, disg, reac, fsec, shell_fsec)


    # 節点に剛性マトリックスのインデックスを割り当てる
    def reNumbering(self):

        # 節点に剛性マトリックスのインデックスを割り当てる
        nodeIndex = list(self.inp.node.keys())
        nd = dict(zip(nodeIndex, list(range(len(nodeIndex)))))

        members = self.inp.member.values()

        # i端節点No ＞ ｊ端節点No の場合は i端 と j端をひっくり返す・・・この処理重いいらねんじゃね？
        flg = True
        while flg:
            flg = False
            for m in members:
                i = nd[m['ni']]
                j = nd[m['nj']]
                if i > j:
                    nodeIndex[j] = m['ni']
                    nodeIndex[i] = m['nj']
                    nd[m['ni']] = j
                    nd[m['nj']] = i
                    flg = True
                    break
        return nodeIndex


    def getResultData(self, gmat: gMatrix, disg):

        reac = np.zeros_like(disg)  # Section force vector
        fsec = dict()  # Section force vector
        shell_fsec = dict()  # Section force vector

        # calculation of section force
        for k in gmat.kmat:
            if k.label == 'Shell':
                list_fsec, fseg = k.getFsec(disg, gmat.Gindex) # Shell部材の剛性マトリックス
                for ID in list_fsec:
                    shell_fsec[ID] = list_fsec[ID]
                nodes = k.nodes

            elif k.label == 'Bar':
                fsec[k.ID], fseg = k.getFsec(disg, gmat.Gindex) # Bar部材の剛性マトリックス
                nodes = [k.IDi, k.IDj]

            # 支点の反力
            i = 0
            for nd in nodes:
                if nd in gmat.mpfix:
                    fix = gmat.mpfix[nd]
                    for j in range(k.nfree):
                        if fix[j] != 0:
                            # 固定支点がある場合
                            iz = gmat.Gindex[nd] + j
                            reac[iz] -= fseg[j + i]
                i += k.nfree



        return disg, reac, fsec, shell_fsec
