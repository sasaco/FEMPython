from IterMethod import toSparse
import math
#--------------------------------------------------------------------#
# ランチョス法による固有値計算

EIG_SHIFT=-0.1	# シフト量δのデフォルト値

# ランチョス法で一般固有値問題Ax=λBxの３重対角化をする
# a,b - 行列A,B
# n - ３重対角化行列の大きさ
def eigByLanczos(a,b,n):
    size=len(a)
    u=[]
    alp=[]
    bet=[]
    n=min(n, size)
    blup=numeric.ccsLUP(numeric.ccsSparse(b))
    u[0]=[]
    for i in range(size):
        u[0][i]=a[i][i]
    a=toSparse(a)
    b=toSparse(b)
    ubu=sparseDotVMV(b,u[0])
    u[0]=numeric.mul(1/math.sqrt(abs(ubu)),u[0])
    for i in range(n-1):
        al=sparseDotVMV(a,u[i])
        alp.append(al)
        bau=numeric.ccsLUPSolve(blup,sparseDotMV(a,u[i]))
        v=numeric.sub(bau,numeric.mul(al,u[i]))
        if i>0:
            v=numeric.sub(v,numeric.mul(bt,u[i-1]))
        for j in range(i+1):
            uv=numeric.dot(u[j],sparseDotMV(b,v))
            v=numeric.sub(v,numeric.mul(uv,u[j]))

        ubu=sparseDotVMV(b,v)
        bt=math.sqrt(abs(ubu))
        bet.append(bt)
        u[i+1]=numeric.mul(1/bt,v)

    al=sparseDotVMV(a,u[n-1])
    alp.append(al)
    eig=tdmEig(alp,bet)
    return {
        "lambda":eig[0],
        "ut":numeric.dot(eig[1],u)
    }


# ランチョス法で一般固有値問題Ax=λBxの３重対角化をする
# 小さい固有値を優先させるため B(A+δB)^-1Bx=θBx に変換する
# 逆行列はLU分解法
# a,b - 行列A,B
# n - ３重対角化行列の大きさ
# delta - シフト量δ
def eigByLanczosLUP(a,b,n,delta):
    size=len(a)
    u=[]
    alp=[]
    bet=[]
    n=min(n,size)
    if delta==None:
        delta=EIG_SHIFT
    ablup=numeric.ccsLUP(numeric.ccsSparse(shiftMatrix(a,b,delta)))
    b=toSparse(b)
    u[0]=[]
    for i in range(size):
        u[0][i]=a[i][i]
    ubu=sparseDotVMV(b,u[0])
    u[0]=numeric.mul(1/math.sqrt(abs(ubu)),u[0])
    for i in range(n):
        bu=sparseDotMV(b,u[i])
        abu=numeric.ccsLUPSolve(ablup,bu)
        al=numeric.dot(bu,abu)
        alp.append(al)
        if i < n-1:
            v=numeric.sub(abu,numeric.mul(al,u[i]))
            if i>0:
                v=numeric.sub(v,numeric.mul(bt,u[i-1]))
            for j in range(i+1):
                uv=numeric.dot(u[j],sparseDotMV(b,v))
                v=numeric.sub(v,numeric.mul(uv,u[j]))

            ubu=sparseDotVMV(b,v)
            bt=math.sqrt(abs(ubu))
            bet.append(bt)
            u[i+1]=numeric.mul(1/bt,v)

    eig=tdmEig(alp,bet)
    e1=eig[0]
    e2=[]
    for i in range(len(e1)):
        e2[i]=1/e1[i]+delta

    return {
        "lambda":e2,
        "ut":numeric.dot(eig[1],u)
    }


# ランチョス法で一般固有値問題Ax=λBxの３重対角化をする
# 小さい固有値を優先させるため B(A+δB)^-1Bx=θBx に変換する
# 逆行列はILUCG法
# a,b - 行列A,B
# n - ３重対角化行列の大きさ
# delta - シフト量δ
def eigByLanczosILUCG(a,b,n,delta):
    size=len(a)
    u=[]
    alp=[]
    bet=[]
    n=min(n,size)
    if delta==None:
        delta=EIG_SHIFT
    adb=shiftMatrix(a,b,delta)
    ablu=getILU(adb)
    adb=toSparse(adb)
    b=toSparse(b)
    u[0]=[]
    for i in range(size):
        u[0][i]=a[i][i]
    ubu=sparseDotVMV(b,u[0])
    u[0]=numeric.mul(1/math.sqrt(abs(ubu)),u[0])
    for i in range(n):
        bu=sparseDotMV(b,u[i])
        abu=solveILU(adb,ablu,bu)
        al=numeric.dot(bu,abu)
        alp.append(al)
        if i<n-1:
            v=numeric.sub(abu,numeric.mul(al,u[i]))
            if i>0:
                v=numeric.sub(v,numeric.mul(bt,u[i-1]))
            for j in range(i+1):
                uv=numeric.dot(u[j],sparseDotMV(b,v))
                v=numeric.sub(v,numeric.mul(uv,u[j]))

            ubu=sparseDotVMV(b,v)
            bt=math.sqrt(abs(ubu))
            bet.append(bt)
            u[i+1]=numeric.mul(1/bt,v)

    eig=tdmEig(alp,bet)
    e1=eig[0]
    e2=[]
    for i in range(len(e1)):
        e2[i]=1/e1[i]+delta

    return {
        "lambda":e2,
        "ut":numeric.dot(eig[1],u)
    }


# アーノルディ法で一般固有値問題Ax=λBxの３重対角化をする
# 小さい固有値を優先させるため (A+δB)^-1Bx=θx に変換する
# 逆行列はLU分解法
# a,b - 行列A,B
# n - ３重対角化行列の大きさ
# delta - シフト量δ
def eigByArnoldiLUP(a,b,n,delta):
    size=len(a)
    u=[]
    n=min(n,size)
    h=numeric.rep([n,n],0)
    if delta==None:
        delta=EIG_SHIFT
    ablup=numeric.ccsLUP(numeric.ccsSparse(shiftMatrix(a,b,delta)))
    b=toSparse(b)
    u[0]=[]
    for i in range(size):
        u[0][i]=a[i][i]
    uu=numeric.dotVV(u[0],u[0])
    u[0]=numeric.mul(1/math.sqrt(uu),u[0])
    for i in range(n):
        bu=sparseDotMV(b,u[i])
        abu=numeric.ccsLUPSolve(ablup,bu)
        v=abu
        for j in range(i+1):
            h1=numeric.dot(u[j],abu)
            h[j][i]=h1
            v=numeric.sub(v,numeric.mul(h1,u[j]))

        if i<n-1:
            for j in range(i+1):
                uv=numeric.dot(u[j],v)
                v=numeric.sub(v,numeric.mul(uv,u[j]))

            uu=numeric.dotVV(v,v)
            h1=math.sqrt(uu)
            h[i+1][i]=h1
            u[i+1]=numeric.mul(1/h1,v)

    eig=eigen(h)
    e1=eig[0]
    e2=[]
    for i in range(len(e1)):
        e2[i]=1/e1[i]+delta
    return {
        "lambda":e2,
        "ut":numeric.dot(eig[1],u)
    }

# アーノルディ法で一般固有値問題Ax=λBxの３重対角化をする
# 小さい固有値を優先させるため (A+δB)^-1Bx=θx に変換する
# 逆行列はILUCG法
# a,b - 行列A,B
# n - ３重対角化行列の大きさ
# delta - シフト量δ
def eigByArnoldiILUCG(a,b,n,delta):
    size=len(a)
    u=[]
    n=min(n,size)
    h=numeric.rep([n,n],0)
    if delta==None:
        delta=EIG_SHIFT
    adb=shiftMatrix(a,b,delta)
    ablu=getILU(adb)
    adb=toSparse(adb)
    b=toSparse(b)
    u[0]=[]
    for i in range(size):
        u[0][i]=a[i][i]
    uu=numeric.dotVV(u[0],u[0])
    u[0]=numeric.mul(1/math.sqrt(uu),u[0])
    for i in range(n):
        bu=sparseDotMV(b,u[i])
        abu=solveILU(adb,ablu,bu)
        v=abu
        for j in range(i+1):
            h1=numeric.dot(u[j],abu)
            h[j][i]=h1
            v=numeric.sub(v,numeric.mul(h1,u[j]))

        if i<n-1:
            for j in range(i+1):
                uv=numeric.dot(u[j],v)
                v=numeric.sub(v,numeric.mul(uv,u[j]))

            uu=numeric.dotVV(v,v)
            h1=math.sqrt(uu)
            h[i+1][i]=h1
            u[i+1]=numeric.mul(1/h1,v)

    eig=eigen(h)
    e1=eig[0]
    e2=[]
    for i in range(len(e1)):
        e2[i]=1/e1[i]+delta
    return {
        "lambda":e2,
        "ut":numeric.dot(eig[1],u)
    }


# ３重対角化行列の固有値・固有ベクトルを求める
# alp,bet - ３重対角化行列の対角成分,その隣の成分
def tdmEig(alp,bet):
    size=len(alp)
    t=numeric.diag(alp)
    for i in range(size-1):
        t[i][i+1]=bet[i]
        t[i+1][i]=bet[i]

    return eigen(t)


# 行列の固有値・固有ベクトルを求める
# m - 行列
def eigen(m):
    e=[]
    ev=[]
    size=len(m)
    eig=numeric.eig(m)
    q=numeric.transpose(eig.E.x)
    for i in range(size):
        ev.append([eig['lambda'].x[i],q[i]])
    ev.sort(function(v1,v2):return abs(v2[0])-abs(v1[0])})
    for i in range(size):
        e[i]=ev[i][0]
        q[i]=ev[i][1]

    return [e,q]


# シフト行列A-δBを返す
# a,b - 行列A,B
# delta - シフト量δ
def shiftMatrix(a,b,delta):
    size=len(a)
    adb=[]
    for i in range(size):
        ai=a[i]
        bi=b[i]
        s=[]
        for j in ai:
            if ai.hasOwnProperty(j):
                j=int(j)
                s[j]=ai[j]

        if delta != 0:
            for j in bi:
                if bi.hasOwnProperty(j):
                    j=int(j)
                    if j in s:
                        s[j]-=delta*bi[j]

                    else:
                        s[j]=-delta*bi[j]

        adb[i]=s

    return adb


# 行列とベクトルの積を計算する
# matrix - 疎行列
# x - ベクトル
def sparseDotVMV(matrix,x):
    row=matrix[0]
    col=matrix[1]
    val=matrix[2]
    m=len(row)-1
    s=0
    for i in range(m):
        j0=row[i]
        j1=row[i+1]
        xi=x[i]
        for j in range(j0, j1):
            s+=xi*val[j]*x[col[j]]

    return s

