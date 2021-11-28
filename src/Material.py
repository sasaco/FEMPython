from Section import CircleSection, RectSection
#--------------------------------------------------------------------#
KS_RECT=5/6			# 矩形断面のせん断補正係数
KS_CIRCLE=6/7			# 円形断面のせん断補正係数

#--------------------------------------------------------------------#
# 材料
# label - 材料番号
# ee - ヤング率 (縦弾性係数) 
# nu - ポアソン比
# dens - 密度
# hCon - 熱伝導率
# sHeat - 比熱
class Material:
    def __init__(self,label,ee,nu,dens,hCon,sHeat):
        self.label=label
        self.ee=ee
        self.nu=nu
        self.dens=dens
        self.hCon=hCon
        self.sHeat=sHeat
        self.gg=0.5*ee/(1+nu)	# 横弾性係数
        self.cv=dens*sHeat		# 体積比熱
        self.matrix=None		# 応力 - 歪マトリックス


    # 平面応力問題の応力 - 歪マトリックスを作成する
    def matrix2Dstress(self):
        coef = self.ee / (1-self.nu*self.nu)
        return [[coef,coef*self.nu,0],[coef*self.nu,coef,0],[0,0,self.gg]]


    # 平面歪問題の応力 - 歪マトリックスを作成する
    def matrix2Dstrain(self):
        coef=self.ee/((1+self.nu)*(1-2*self.nu))
        return [[coef*(1-self.nu),coef*self.nu,0],
                        [coef*self.nu,coef*(1-self.nu),0],[0,0,self.gg]]


    # 軸対称問題の応力 - 歪マトリックスを作成する
    def matrixAxiSymetric(self):
        coef=self.ee/((1+self.nu)*(1-2*self.nu))
        s1=coef*(1-self.nu),s2=coef*self.nu
        return [[s1,s2,s2,0],[s2,s1,s2,0],[s2,s2,s1,0],[0,0,0,self.gg]]


    # 捩り問題の応力 - 歪マトリックスを作成する
    def matrixTorsion(self):
        return [[self.gg,0],[0,self.gg]]


    # 3次元問題の応力 - 歪マトリックスを作成する
    def matrix3D(self):
        coef=self.ee/((1+self.nu)*(1-2*self.nu))
        s1=coef*(1-self.nu),s2=coef*self.nu
        return [[s1,s2,s2,0,0,0],[s2,s1,s2,0,0,0],[s2,s2,s1,0,0,0],
                        [0,0,0,self.gg,0,0],[0,0,0,0,self.gg,0],[0,0,0,0,0,self.gg]]


    # シェル要素の応力 - 歪マトリックスを作成する
    def matrixShell(self):
        coef=self.ee/(1-self.nu*self.nu)
        s2=coef*self.nu
        return [[coef,s2,0,0,0],[s2,coef,0,0,0],[0,0,self.gg,0,0],
                        [0,0,0,KS_RECT*self.gg,0],[0,0,0,0,KS_RECT*self.gg]]


    # 材料を表す文字列を返す
    def toString(self):
        return 'Material\t'+self.label.toString(10)+'\t'+ \
                    self.ee+'\t'+self.nu+'\t'+self.gg+'\t'+self.dens+'\t'+ \
                    self.hCon+'\t'+self.sHeat


#--------------------------------------------------------------------#
# シェルパラメータ
# label - パラメータ番号
# thickness - 厚さ
class ShellParameter:
    def __init__(self,label,thickness):
        self.label=label
        self.thickness=thickness

    # シェルパラメータを表す文字列を返す
    def toString(self):
        return 'ShellParameter\t'+self.label.toString(10)+'\t'+self.thickness

#--------------------------------------------------------------------#
# 梁パラメータ（円形断面）
# label - パラメータ番号
# type - 断面種類
# ss - データ文字列
class BarParameter:
    def __init__(self,label,type,ss):
        self.label=label
        self.type=type
        tp=type.toLowerCase()
        if tp=='circle':
            self.section=CircleSection(ss)
        elif tp=='rectangle':
            self.section=RectSection(ss)

    # 梁パラメータを表す文字列を返す
    def toString(self):
        return 'BarParameter\t'+self.label.toString(10)+'\t'+self.type+'\t'+ \
                    self.section.toString()

