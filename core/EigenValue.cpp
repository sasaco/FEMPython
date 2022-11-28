#include "EigenValue.h"

//--------------------------------------------------------------------//
// 固有値
// value - 固有値・固有振動数
// type - 解析種類
EigenValue::EigenValue(double _value, int _type) {
    value = _value;
    type = _type;
    /*
    this.displacement = [];		// 変位
    this.sEnergy1 = [];		// 節点歪エネルギー密度
    this.sEnergy2 = [];
    this.dispMax = 0;
    this.angleMax = 0;
    */
};

/*

// 変位を設定する
// bc - 境界条件
// disp - 変位を表す固有ベクトル
// nodeCount - 節点数
EigenValue.prototype.setDisplacement = function(bc, disp, nodeCount) {
    this.displacement.length = 0;
    this.dispMax = 0;
    this.angleMax = 0;
    var rests = bc.restraints, ii = 0;
    for (var i = 0; i < nodeCount; i++) {
        var v = new Vector3R(), i0 = bc.nodeIndex[i], bcDof = bc.dof[i], r = -1, x = v.x;
        for (var j = 0; j < bcDof; j++) {
            var bcl = bc.bcList[i0 + j];
            if (bcl < 0) {
                x[j] = disp[ii];
                ii++;
            }
            else {
                r = parseInt(bcl / 6);
            }
        }
        if ((r >= 0) && rests[r].coords) {
            v.x = rests[r].coords.toGlobal(x);
        }
        this.dispMax = Math.max(this.dispMax, v.magnitude());
        this.angleMax = Math.max(this.angleMax, v.magnitudeR());
        this.displacement.push(v);
    }
};

// データを取り出す
// param - データの種類
// component - データの成分
// index - 節点のインデックス
EigenValue.prototype.getData = function(param, component, index) {
    switch (param) {
    case DISPLACEMENT:
        switch (component) {
        case X:
        case Y:
        case Z:
        case RX:
        case RY:
        case RZ:
            return this.displacement[index].x[component];
        case MAGNITUDE:
            return this.displacement[index].magnitude();
        }
        break;
    case S_ENERGY:
        if (component == = 0) {
            return this.sEnergy1[index];
        }
        else {
            return this.sEnergy2[index];
        }
        break;
    }
    return 0;
};

// 節点歪・応力を初期化する
// count - 節点数
EigenValue.prototype.initStrainEnergy = function(count) {
    this.sEnergy1.length = 0;
    this.sEnergy2.length = 0;
    for (var i = 0; i < count; i++) {
        this.sEnergy1[i] = 0;
        this.sEnergy2[i] = 0;
    }
};

// データ文字列を返す
// nodes - 節点
// tuple - 節点or要素
EigenValue.prototype.toStrings = function(nodes, tuple) {
    var s = [], i;
    s.push('EigenValue\t' + this.type + '\t' + this.value);
    for (i = 0; i < this.displacement.length; i++) {
        s.push('Displacement\t' + nodes[i].label.toString(10) + '\t' +
            this.displacement[i].x.join('\t'));
    }
    for (i = 0; i < this.sEnergy1.length; i++) {
        s.push('StrEnergy1\t' + tuple[i].label.toString(10) + '\t' +
            this.sEnergy1[i]);
    }
    for (i = 0; i < this.sEnergy2.length; i++) {
        s.push('StrEnergy2\t' + tuple[i].label.toString(10) + '\t' +
            this.sEnergy2[i]);
    }
    return s;
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
