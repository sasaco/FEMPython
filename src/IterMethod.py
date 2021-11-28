#--------------------------------------------------------------------#
# 反復解法（不完全LU分解共役勾配法）

ILUCG_THRES=1e-10	# 不完全LU分解共役勾配法の収束閾値のデフォルト値

# 不完全LU分解をする
# a - 行列
function getILU(a):
  m=a.length,i,j,diag=[],col=[],val=[],d=[],colData=[]
  for(i=0i<mi++):
    col[i]=[]
    val[i]=[]
    diag[i]=-1
    colData[i]=[]
  }
# 列からの検索用ポインタを設定する
  for(i=0i<mi++):
    arow=a[i]
    cols=[]
    for(j in arow):
      if(arow.hasOwnProperty(j)):
      	cols.append(int(j))
      }
    }
    cols.sort(function(c1,c2):return c1-c2})
    for(j=0j<cols.lengthj++):
      cj=cols[j]
      if(cj==i):
      	diag[i]=j
      	d[cj]=colData[cj].length
      }
      col[i].append(cj)
      val[i].append(arow[cj])
      colData[cj].append(i)
    }
  }
# 不完全LU分解をする
  for(k=0k<m-1k++):
    vk=val[k],ck=col[k],dk=diag[k],cdk=colData[k]
    if((dk<0) or (vk[dk]==0)) raise Exception('対角成分が0です')
    dkk=1/vk[dk]
    for(j=dk+1j<ck.lengthj++):
      vk[j]*=dkk
    }
    i0=d[k]+1,i1=cdk.length
    for(i=i0i<i1i++):
      row=cdk[i],vrow=val[row],crow=col[row]
      c=crow.indexOf(k)
      if(c>=0):
      	vik=-vrow[c]
      	for(j=dk+1j<ck.lengthj++):
      	  c=crow.indexOf(ck[j])
      	  if(c>=0):
      	    vrow[c]+=vik*vk[j]
      	  }
      	}
      }
    }
  }
  rowData=[],valData=[],count=0
  colData.length=0
  rowData.append(count)
  for(i=0i<mi++):
    diag[i]+=count
    count+=col[i].length
    rowData.append(count)
    Array.prototype.push.apply(colData,col[i])
    Array.prototype.push.apply(valData,val[i])
    valData[diag[i]]=1/valData[diag[i]]
  }
  return [rowData,colData,valData,diag]
}

# LU分解法で連立方程式の解を求める
# lu - LU分解した疎行列
# p - ベクトル
function solveLU(lu,p):
  row=lu[0],col=lu[1],val=lu[2],diag=lu[3],m=row.length-1
  q=[],i,j,j1
  q[0]=p[0]*val[diag[0]]
  for(i=1i<mi++):
    p2=p[i],j0=row[i]
    j1=diag[i]
    for(j=j0j<j1j++):
      p2-=val[j]*q[col[j]]
    }
    q[i]=p2*val[j1]
  }
  for(i=m-2i>=0i--):
    j1=diag[i]+1
    q2=q[i],j2=row[i+1]
    for(j=j1j<j2j++):
      q2-=val[j]*q[col[j]]
    }
    q[i]=q2
  }
  return q
}

# 不完全LU分解共役勾配法で連立方程式の解を求める
# matrix - 元の行列
# ilu - 不完全LU分解した疎行列
# p - ベクトル
# iterMax - 反復回数の上限
# thres - 収束閾値
function solveILU(matrix,ilu,p,iterMax,thres):
  iterMax=iterMaxorp.length
  thres=thresorILUCG_THRES
  x=numeric.rep([p.length],0),xg=p.concat()
  xq=solveLU(ilu,xg)
  xd=xq.concat(),j
  for(iter=0iter<iterMaxiter++):
    z1=numeric.dotVV(xd,xg)
    xq=sparseDotMV(matrix,xd)
    r=numeric.dotVV(xd,xq)
    if(abs(r)==0):
      raise Exception('方程式求解エラー at iter='+iter)
    }
    alpha=z1/r
    for(j=0j<xg.lengthj++):
      x[j]+=alpha*xd[j]
      xg[j]-=alpha*xq[j]
    }
    if(numeric.dotVV(xg,xg)<thres) return x
    xq2=solveLU(ilu,xg)
    z2=numeric.dotVV(xq,xq2)
    beta=-z2/r
    for(j=0j<xg.lengthj++):
      xd[j]=beta*xd[j]+xq2[j]
    }
  }
  return x
}

# 行列とベクトルの積を計算する
# matrix - 疎行列
# x - ベクトル
function sparseDotMV(matrix,x):
  row=matrix[0],col=matrix[1],val=matrix[2],m=row.length-1,y=[]
  for(i=0i<mi++):
    y0=0,j0=row[i],j1=row[i+1]
    for(j=j0j<j1j++):
      y0+=val[j]*x[col[j]]
    }
    y[i]=y0
  }
  return y
}

# 行ベースの疎行列に変換する
# a - 元の行列
function toSparse(a):
  m=a.length,count=0,row=[],col=[],val=[],j
  row.append(count)
  for(i=0i<mi++):
    arow=a[i]
    cols=[]
    for(j in arow):
      if(arow.hasOwnProperty(j)):
      	cols.append(int(j))
      }
    }
    cols.sort(function(c1,c2):return c1-c2})
    count+=cols.length
    row.append(count)
    Array.prototype.push.apply(col,cols)
    for(j=0j<cols.lengthj++):
      val.append(arow[cols[j]])
    }
  }
  return [row,col,val]
}
