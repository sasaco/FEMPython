
//--------------------------------------------------------------------//
// 六面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var HexaElement1 = function(label, material, nodes) {
    SolidElement.call(this, label, material, nodes, HEXA1_NODE, HEXA1_INT);
};

// 要素名称を返す
HexaElement1.prototype.getName = function() {
    return 'HexaElement1';
};

// 節点数を返す
HexaElement1.prototype.nodeCount = function() {
    return 8;
};

// 要素境界数を返す
HexaElement1.prototype.borderCount = function() {
    return 6;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
HexaElement1.prototype.border = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new QuadangleBorder1(element, [p[0], p[3], p[2], p[1]]);
    case 1:
        return new QuadangleBorder1(element, [p[4], p[5], p[6], p[7]]);
    case 2:
        return new QuadangleBorder1(element, [p[0], p[1], p[5], p[4]]);
    case 3:
        return new QuadangleBorder1(element, [p[1], p[2], p[6], p[5]]);
    case 4:
        return new QuadangleBorder1(element, [p[2], p[3], p[7], p[6]]);
    case 5:
        return new QuadangleBorder1(element, [p[3], p[0], p[4], p[7]]);
    }
};

// 要素を鏡像反転する
HexaElement1.prototype.mirror = function() {
    swap(this.nodes, 1, 3);
    swap(this.nodes, 5, 7);
};

// 要素節点の角度を返す
// p - 要素節点
HexaElement1.prototype.angle = function(p) {
    var th = [];
    for (var i = 0; i < 4; i++) {
        th[i] = solidAngle(p[i], p[(i + 1) % 4], p[(i + 3) % 4], p[i + 4]);
        th[i + 4] = solidAngle(p[i + 4], p[(i + 1) % 4 + 4], p[(i + 3) % 4 + 4], p[i]);
    }
    return th;
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
HexaElement1.prototype.shapeFunction = function(xsi, eta, zeta) {
    return [[0.125 * (1 - xsi) * (1 - eta) * (1 - zeta), -0.125 * (1 - eta) * (1 - zeta),
        -0.125 * (1 - xsi) * (1 - zeta), -0.125 * (1 - xsi) * (1 - eta)],
        [0.125 * (1 + xsi) * (1 - eta) * (1 - zeta), 0.125 * (1 - eta) * (1 - zeta),
        -0.125 * (1 + xsi) * (1 - zeta), -0.125 * (1 + xsi) * (1 - eta)],
        [0.125 * (1 + xsi) * (1 + eta) * (1 - zeta), 0.125 * (1 + eta) * (1 - zeta),
        0.125 * (1 + xsi) * (1 - zeta), -0.125 * (1 + xsi) * (1 + eta)],
        [0.125 * (1 - xsi) * (1 + eta) * (1 - zeta), -0.125 * (1 + eta) * (1 - zeta),
        0.125 * (1 - xsi) * (1 - zeta), -0.125 * (1 - xsi) * (1 + eta)],
        [0.125 * (1 - xsi) * (1 - eta) * (1 + zeta), -0.125 * (1 - eta) * (1 + zeta),
        -0.125 * (1 - xsi) * (1 + zeta), 0.125 * (1 - xsi) * (1 - eta)],
        [0.125 * (1 + xsi) * (1 - eta) * (1 + zeta), 0.125 * (1 - eta) * (1 + zeta),
        -0.125 * (1 + xsi) * (1 + zeta), 0.125 * (1 + xsi) * (1 - eta)],
        [0.125 * (1 + xsi) * (1 + eta) * (1 + zeta), 0.125 * (1 + eta) * (1 + zeta),
        0.125 * (1 + xsi) * (1 + zeta), 0.125 * (1 + xsi) * (1 + eta)],
        [0.125 * (1 - xsi) * (1 + eta) * (1 + zeta), -0.125 * (1 + eta) * (1 + zeta),
        0.125 * (1 - xsi) * (1 + zeta), 0.125 * (1 - xsi) * (1 + eta)]];
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
HexaElement1.prototype.massMatrix = function(p, dens) {
    var ja = 0, i;
    for (i = 0; i < 8; i++) {
        var sf = this.shapeFunction(HEXA1_INT[i][0], HEXA1_INT[i][1],
            HEXA1_INT[i][2]);
        ja += Math.abs(this.jacobianMatrix(p, sf).determinant());
    }
    var coef = dens * ja, m = numeric.rep([24, 24], 0);
    for (i = 0; i < 8; i++) {
        var i3 = 3 * i;
        for (var j = 0; j < 8; j++) {
            var value = coef * HEXA1_MASS_BASE[i][j], j3 = 3 * j;
            m[i3][j3] += value;
            m[i3 + 1][j3 + 1] += value;
            m[i3 + 2][j3 + 2] += value;
        }
    }
    return m;
};
