#include "SymmetricTensor3.h"
//--------------------------------------------------------------------//
// ３次元対称テンソル
// s - 成分
SymmetricTensor3::SymmetricTensor3(VectorXd s) {
    xx = s(0);
    yy = s(1);
    zz = s(2);
    xy = s(3);
    yz = s(4);
    zx = s(5);
};

// テンソルをベクトルとして返す
VectorXd SymmetricTensor3::vector() {
    VectorXd s(6);
    s(0) = xx;
    s(1) = yy;
    s(2) = zz;
    s(3) = xy;
    s(4) = yz;
    s(5) = zx;
    return s;
};

// テンソルを加える
// t - 加えるテンソル
void SymmetricTensor3::add(SymmetricTensor3 t) {
    xx += t.xx;
    yy += t.yy;
    zz += t.zz;
    xy += t.xy;
    yz += t.yz;
    zx += t.zx;
};

// 成分にスカラーを掛ける
// a - 掛けるスカラー
void SymmetricTensor3::mul(double a) {
    xx *= a;
    yy *= a;
    zz *= a;
    xy *= a;
    yz *= a;
    zx *= a;
};

// テンソルの内積を計算する
// t - 相手のテンソル
double SymmetricTensor3::innerProduct(SymmetricTensor3 t) {
    double result = xx * t.xx + yy * t.yy + zz * t.zz +
        2 * (xy * t.xy + yz * t.yz + zx * t.zx);

    return result;
}


// テンソルを回転させる
// d - 方向余弦マトリックス
void SymmetricTensor3::rotate(MatrixXd d) {

    MatrixXd mat(3, 3);
    mat(0, 0) = xx;
    mat(0, 1) = xy;
    mat(0, 2) = zx;
    
    mat(1, 0) = xy;
    mat(1, 1) = yy;
    mat(1, 2) = yz;

    mat(2, 0) = zx;
    mat(2, 1) = yz;
    mat(2, 2) = zz;

    VectorXd s = VectorXd::Zero(6);

    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            double mij = mat(i, j);
            for (int k = 0; k < 3; k++) {
                s(k) += d(k, i) * d(k, j) * mij;
                s(k + 3) += d(k, i) * d((k + 1) % 3, j) * mij;
            }
        }
    }
    xx = s(0);
    yy = s(1);
    zz = s(2);
    xy = s(3);
    yz = s(4);
    zx = s(5);
}


/*
// 固有値を返す
SymmetricTensor3.prototype.principal = function() {
    return eigenvalue(this, 100).lambda;
};



// Jacobie法で対称テンソルの固有値を求める
// Numeric.jsでは対角要素が0（例えばせん断のみの条件）だと求められない
// st - 対称テンソル
// iterMax - 反復回数の最大値
function eigenvalue(st, iterMax) {
    var m = [[st.xx, st.xy, st.zx], [st.xy, st.yy, st.yz],
        [st.zx, st.yz, st.zz]];
    return eigenByJacob(m, iterMax);
}


// Jacobie法で対称テンソルの固有値を求める
// m - 対称行列
// iterMax - 反復回数の最大値
function eigenByJacob(m, iterMax) {
    var size = m.length, abs = Math.abs, i, j, iter, dataMax = 0;
    var ev = numeric.identity(size);
    for (i = 0; i < size; i++) {
        for (j = i; j < size; j++) {
            dataMax = Math.max(dataMax, abs(m[i][j]));
        }
    }
    var tolerance = EIG_EPS * dataMax;
    // 値が0の場合
    if (dataMax == = 0) return { lambda:numeric.getDiag(m),ev : ev };
    for (iter = 0; iter < iterMax; iter++) {
        var im = 0, jm = 0, ndMax = 0;
        for (i = 0; i < 2; i++) {
            for (j = i + 1; j < 3; j++) {
                var absm = abs(m[i][j]);
                if (absm > ndMax) {
                    ndMax = absm;
                    im = i;
                    jm = j;
                }
            }
        }
        if (ndMax < tolerance) break;
        var mim = m[im], mjm = m[jm];
        var alpha = 0.5 * (mim[im] - mjm[jm]);
        var beta = 0.5 / Math.sqrt(alpha * alpha + ndMax * ndMax);
        var cc2 = 0.5 + abs(alpha) * beta, cs = -beta * mim[jm];
        if (alpha < 0) cs = -cs;
        var cc = Math.sqrt(cc2), ss = cs / cc;
        var aij = 2 * (alpha * cc2 - mim[jm] * cs), aii = mjm[jm] + aij, ajj = mim[im] - aij;
        for (i = 0; i < 3; i++) {
            var mi = m[i], evi = ev[i];
            var a1 = mi[im] * cc - mi[jm] * ss;
            var a2 = mi[im] * ss + mi[jm] * cc;
            mi[im] = a1;
            mi[jm] = a2;
            mim[i] = a1;
            mjm[i] = a2;
            a1 = evi[im] * cc - evi[jm] * ss;
            a2 = evi[im] * ss + evi[jm] * cc;
            evi[im] = a1;
            evi[jm] = a2;
        }
        mim[im] = aii;
        mim[jm] = 0;
        mjm[im] = 0;
        mjm[jm] = ajj;
    }
    m = numeric.getDiag(m);
    // 固有値を大きい順に入れ替える
    var eig = [];
    ev = numeric.transpose(ev);
    for (i = 0; i < size; i++) eig.push([m[i], ev[i]]);
    eig.sort(function(v1, v2) { return v2[0] - v1[0]; });
    for (i = 0; i < size; i++) {
        m[i] = eig[i][0];
        ev[i] = eig[i][1];
    }
    return { lambda:m,ev : numeric.transpose(ev) };
}

*/