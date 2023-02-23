import math
import numpy as np
#--------------------------------------------------------------------#
# 反復解法（不完全LU分解共役勾配法）

ILUCG_THRES=1e-10	# 不完全LU分解共役勾配法の収束閾値のデフォルト値

# 不完全LU分解をする
# a - 行列
def getILU(a):
    m=len(a)
    diag=[]
    col=[]
    val=[]
    d=[]
    colData=[]
    for i in range(m):
        col[i]=[]
        val[i]=[]
        diag[i]=-1
        colData[i]=[]

    # 列からの検索用ポインタを設定する
    for i in range(m):
        arow=a[i]
        cols=[]
        for j in arow:
            if arow.hasOwnProperty(j):
                cols.append(int(j))

        sorted(cols, key=lambda c1,c2: c1-c2)

        for j in range(len(cols)):
            cj=cols[j]
            if cj==i:
                diag[i]=j
                d[cj]=len(colData[cj])

            col[i].append(cj)
            val[i].append(arow[cj])
            colData[cj].append(i)

    # 不完全LU分解をする
    for k in range(m-1):
        vk=val[k]
        ck=col[k]
        dk=diag[k]
        cdk=colData[k]
        if dk<0 or vk[dk]==0:
            raise Exception('対角成分が0です')
        dkk=1/vk[dk]
        for j in range(dk+1, len(ck)):
            vk[j]*=dkk

        i0=d[k]+1
        i1=len(cdk)
        for i in range(i0, i1):
            row=cdk[i]
            vrow=val[row]
            crow=col[row]
            c=crow.indexOf(k)
            if c>=0:
                vik =- vrow[c]
                for j in range(dk+1, len(ck)):
                    c=crow.indexOf(ck[j])
                    if c>=0:
                        vrow[c]+=vik*vk[j]

    rowData=[]
    valData=[]
    count=0
    colData=[]
    rowData.append(count)
    for i in range(m):
        diag[i]+=count
        count+= len(col[i])
        rowData.append(count)
        #Array.prototype.push.apply(colData,col[i])
        for e in col[i]:
            colData.append(e)
        #Array.prototype.push.apply(valData,val[i])
        for e in val[i]:
            valData.append(e)

        valData[diag[i]]=1/valData[diag[i]]

    return [rowData,colData,valData,diag]


# LU分解法で連立方程式の解を求める
# lu - LU分解した疎行列
# p - ベクトル
def solveLU(lu,p):
    row=lu[0]
    col=lu[1]
    val=lu[2]
    diag=lu[3]
    m=len(row)-1
    q=[]
    q[0]=p[0]*val[diag[0]]
    for i in range(1,m):
        p2=p[i]
        j0=row[i]
        j1=diag[i]
        for j in range(j0,j1):
            p2-=val[j]*q[col[j]]

        q[i]=p2*val[j1]

    for i in reversed(range(0, m-2)):
        j1=diag[i]+1
        q2=q[i]
        j2=row[i+1]
        for j in range(j1, j2):
            q2-=val[j]*q[col[j]]

        q[i]=q2

    return q


# 不完全LU分解共役勾配法で連立方程式の解を求める
# matrix - 元の行列
# ilu - 不完全LU分解した疎行列
# p - ベクトル
# iterMax - 反復回数の上限
# thres - 収束閾値
def solveILU(matrix,ilu,p,iterMax,thres):
    iterMax=len(iterMaxorp)
    thres=thresorILUCG_THRES
    x=np.zeros(len(p))
    xg=p.concat()
    xq=solveLU(ilu,xg)
    xd=xq.concat(),j
    for iter in range(iterMax):
        z1=np.dot(xd,xg)
        xq=sparseDotMV(matrix,xd)
        r=np.dot(xd,xq)
        if abs(r)==0:
            raise Exception('方程式求解エラー at iter='+iter)

        alpha=z1/r
        for j in range(len(xg)):
            x[j]+=alpha*xd[j]
            xg[j]-=alpha*xq[j]

        if(np.dot(xg,xg)<thres):
            return x
        xq2=solveLU(ilu,xg)
        z2=np.dot(xq,xq2)
        beta=-z2/r
        for j in range(len(xg)):
            xd[j]=beta*xd[j]+xq2[j]

    return x


# 行列とベクトルの積を計算する
# matrix - 疎行列
# x - ベクトル
def sparseDotMV(matrix,x):
    row=matrix[0]
    col=matrix[1]
    val=matrix[2]
    m=len(row)-1
    y=[]
    for i in range(m):
        y0=0
        j0=row[i]
        j1=row[i+1]
        for j in range(j0, j1):
            y0+=val[j]*x[col[j]]

        y[i]=y0

    return y


# 行ベースの疎行列に変換する
# a - 元の行列
def  toSparse(a):
    m=len(a)
    count=0
    row=[]
    col=[]
    val=[]
    row.append(count)
    for i in range(m):
        arow=a[i]
        cols=[]
        for j in arow:
            if arow.hasOwnProperty(j):
                cols.append(int(j))

        sorted(cols, key=lambda c1,c2: c1-c2)

        count += len(cols)
        row.append(count)
        #Array.prototype.push.apply(col,cols)
        for e in cols:
            col.append(e)

        for j in range(len(cols)):
            val.append(arow[cols[j]])

    return [row,col,val]
