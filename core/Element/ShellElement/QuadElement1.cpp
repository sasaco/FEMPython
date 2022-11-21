
//--------------------------------------------------------------------//
// 四角形1次要素 (MITC4)
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
var QuadElement1 = function(label, material, param, nodes) {
    ShellElement.call(this, label, material, param, nodes,
        QUAD1_NODE, QUAD1_INT);
};

// 要素名称を返す
QuadElement1.prototype.getName = function() {
    return 'QuadElement1';
};

// 節点数を返す
QuadElement1.prototype.nodeCount = function() {
    return 4;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
QuadElement1.prototype.border = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new QuadangleBorder1(element, [p[0], p[1], p[2], p[3]]);
    case 1:
        return new QuadangleBorder1(element, [p[0], p[3], p[2], p[1]]);
    }
};

// 要素境界辺を返す
// element - 要素ラベル
// index - 要素境界辺のインデックス
QuadElement1.prototype.borderEdge = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new EdgeBorder1(element, [p[0], p[1]]);
    case 1:
        return new EdgeBorder1(element, [p[1], p[2]]);
    case 2:
        return new EdgeBorder1(element, [p[2], p[3]]);
    case 3:
        return new EdgeBorder1(element, [p[3], p[0]]);
    }
};

// 要素を鏡像反転する
QuadElement1.prototype.mirror = function() {
    swap(this.nodes, 1, 3);
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
QuadElement1.prototype.shapeFunction = function(xsi, eta) {
    return [[0.25 * (1 - xsi) * (1 - eta), -0.25 * (1 - eta), -0.25 * (1 - xsi)],
        [0.25 * (1 + xsi) * (1 - eta), 0.25 * (1 - eta), -0.25 * (1 + xsi)],
        [0.25 * (1 + xsi) * (1 + eta), 0.25 * (1 + eta), 0.25 * (1 + xsi)],
        [0.25 * (1 - xsi) * (1 + eta), -0.25 * (1 + eta), 0.25 * (1 - xsi)]];
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
// t - 要素厚さ
QuadElement1.prototype.massMatrix = function(p, dens, t) {
    var count = this.nodeCount(), m = numeric.rep([6 * count, 6 * count], 0);
    var d = dirMatrix(p), n = normalVector(p), tt = C1_12 * t * t;
    for (var i = 0; i < this.intP.length; i++) {
        var ipi = this.intP[i], sf = this.shapeFunction(ipi[0], ipi[1]);
        var nn = [sf[0][0], sf[1][0], sf[2][0], sf[3][0]];
        var jac = Math.abs(this.jacobianMatrix(p, sf, n, t).determinant());
        jac *= 2 * ipi[2];
        for (var i1 = 0; i1 < count; i1++) {
            var i6 = 6 * i1, nja = nn[i1] * jac;
            for (var j1 = 0; j1 < count; j1++) {
                var j6 = 6 * j1, nnja = nja * nn[j1], dm = dens * nnja, dii = tt * dm;
                m[i6][j6] += dm;
                m[i6 + 1][j6 + 1] += dm;
                m[i6 + 2][j6 + 2] += dm;
                m[i6 + 3][j6 + 3] += dii;
                m[i6 + 4][j6 + 4] += dii;
                m[i6 + 5][j6 + 5] += 1e-4 * dii;
            }
        }
    }
    toDir3(d, m);
    return m;
};

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
QuadElement1.prototype.stiffnessMatrix = function(p, d1, sp) {
    var size = 6 * this.nodeCount(), kk = numeric.rep([size, size], 0);
    var n = normalVector(p), t = sp.thickness;
    for (var i = 0; i < this.intP.length; i++) {
        var ks = this.stiffPart(p, d1, n, this.intP[i][0], this.intP[i][1], t);
        addMatrix(kk, ks);
    }
    return kk;
};

// 積分点の剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// n - 法線ベクトル
// xsi,eta - 要素内部ξ,η座標
// t - 要素厚さ
QuadElement1.prototype.stiffPart = function(p, d1, n, xsi, eta, t) {
    var d = dirMatrix(p);
    var sf = this.shapeFunction(xsi, eta);
    var ja = this.jacobianMatrix(p, sf, n, t);
    var bc0 = this.strainMatrix1(ja, sf, d);
    var sf1 = this.shapeFunction(xsi, 0);
    var ja1 = this.jacobianMatrix(p, sf1, n, t);
    var sf2 = this.shapeFunction(0, eta);
    var ja2 = this.jacobianMatrix(p, sf2, n, t);
    var bc = [this.strainMatrix1(ja1, sf1, d), this.strainMatrix1(ja2, sf2, d)];
    var count = this.nodeCount();
    var kk = numeric.rep([6 * count, 6 * count], 0);
    var jacob = Math.abs(ja.determinant());

    var tt6 = t * t / 6.0, ce1 = 1e-3 * t * t * d1[3][3], ce2 = -ce1 / (count - 1), j1, j2;
    var k1 = [[0, 0, 0], [0, 0, 0], [0, 0, 0]];
    var k2 = [[0, 0, 0], [0, 0, 0], [0, 0, 0]];
    var k3 = [[0, 0, 0], [0, 0, 0], [0, 0, 0]];
    var k4 = [[0, 0, 0], [0, 0, 0], [0, 0, 0]];
    for (var i = 0; i < count; i++) {
        for (var j = 0; j < count; j++) {
            for (j1 = 0; j1 < 3; j1++) {
                for (j2 = 0; j2 < 3; j2++) {
                    k1[j1][j2] = 0;
                    k2[j1][j2] = 0;
                    k3[j1][j2] = 0;
                    k4[j1][j2] = 0;
                }
            }
            for (j1 = 0; j1 < 2; j1++) {
                for (j2 = 0; j2 < 2; j2++) {
                    k1[j1][j2] = bc0[i][j1] * d1[j1][j2] * bc0[j][j2] +
                        bc0[i][1 - j1] * d1[2][2] * bc0[j][1 - j2];
                }
                var dd = d1[4 - j1][4 - j1];
                k1[j1][j1] += bc[1 - j1][i][2] * dd * bc[1 - j1][j][2];
                k1[j1][2] = bc[1 - j1][i][2] * dd * bc[j1][j][j1];
                k1[2][j1] = bc[j1][i][j1] * dd * bc[1 - j1][j][2];
                k2[j1][j1] = bc[1 - j1][i][2] * dd * bc[1 - j1][j][3];
                k2[2][j1] = bc[1 - j1][i][j1] * dd * bc[1 - j1][j][3];
                k3[j1][j1] = bc[1 - j1][i][3] * dd * bc[1 - j1][j][2];
                k3[j1][2] = bc[1 - j1][i][3] * dd * bc[1 - j1][j][j1];
            }
            k1[2][2] = bc[0][i][1] * d1[3][3] * bc[0][j][1] +
                bc[1][i][0] * d1[4][4] * bc[1][j][0];
            k4[0][0] = k1[1][1] + 3 * bc[0][i][3] * d1[3][3] * bc[0][j][3];
            k4[0][1] = -k1[1][0];
            k4[1][0] = -k1[0][1];
            k4[1][1] = k1[0][0] + 3 * bc[1][i][3] * d1[4][4] * bc[1][j][3];
            for (j1 = 0; j1 < 3; j1++) {
                var kt = k2[j1][0];
                k2[j1][0] = -k2[j1][1];
                k2[j1][1] = kt;
                kt = k3[0][j1];
                k3[0][j1] = -k3[1][j1];
                k3[1][j1] = kt;
            }

            if (i == j) k4[2][2] = ce1;
            else     k4[2][2] = ce2;
            toDir(d, k1);
            toDir(d, k2);
            toDir(d, k3);
            toDir(d, k4);
            var i0 = 6 * i;
            var j0 = 6 * j;
            for (j1 = 0; j1 < 3; j1++) {
                for (j2 = 0; j2 < 3; j2++) {
                    kk[i0 + j1][j0 + j2] = 2 * jacob * k1[j1][j2];
                    kk[i0 + j1][j0 + 3 + j2] = t * jacob * k2[j1][j2];
                    kk[i0 + 3 + j1][j0 + j2] = t * jacob * k3[j1][j2];
                    kk[i0 + 3 + j1][j0 + 3 + j2] = tt6 * jacob * k4[j1][j2];
                }
            }
        }
    }
    return kk;
};
