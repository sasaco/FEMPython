
class WedgeElement1 {

private:


public:


};

//--------------------------------------------------------------------//
// 楔形1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var WedgeElement1 = function(label, material, nodes) {
    SolidElement.call(this, label, material, nodes, WEDGE1_NODE, WEDGE1_INT);
};

// 要素名称を返す
WedgeElement1.prototype.getName = function() {
    return 'WedgeElement1';
};

// 節点数を返す
WedgeElement1.prototype.nodeCount = function() {
    return 6;
};

// 要素境界数を返す
WedgeElement1.prototype.borderCount = function() {
    return 5;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
WedgeElement1.prototype.border = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new TriangleBorder1(element, [p[0], p[2], p[1]]);
    case 1:
        return new TriangleBorder1(element, [p[3], p[4], p[5]]);
    case 2:
        return new QuadangleBorder1(element, [p[0], p[1], p[4], p[3]]);
    case 3:
        return new QuadangleBorder1(element, [p[1], p[2], p[5], p[4]]);
    case 4:
        return new QuadangleBorder1(element, [p[2], p[0], p[3], p[5]]);
    }
};

// 要素を鏡像反転する
WedgeElement1.prototype.mirror = function() {
    swap(this.nodes, 1, 2);
    swap(this.nodes, 4, 5);
};

// 要素節点の角度を返す
// p - 要素節点
WedgeElement1.prototype.angle = function(p) {
    var th = [];
    for (var i = 0; i < 3; i++) {
        th[i] = solidAngle(p[i], p[(i + 1) % 3], p[(i + 2) % 3], p[i + 3]);
        th[i + 3] = solidAngle(p[i + 3], p[(i + 1) % 3 + 3], p[(i + 2) % 3 + 3], p[i]);
    }
    return th;
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
WedgeElement1.prototype.shapeFunction = function(xsi, eta, zeta) {
    return [[0.5 * (1 - xsi - eta) * (1 - zeta), -0.5 * (1 - zeta), -0.5 * (1 - zeta),
        -0.5 * (1 - xsi - eta)],
        [0.5 * xsi * (1 - zeta), 0.5 * (1 - zeta), 0, -0.5 * xsi],
        [0.5 * eta * (1 - zeta), 0, 0.5 * (1 - zeta), -0.5 * eta],
        [0.5 * (1 - xsi - eta) * (1 + zeta), -0.5 * (1 + zeta), -0.5 * (1 + zeta),
        0.5 * (1 - xsi - eta)],
        [0.5 * xsi * (1 + zeta), 0.5 * (1 + zeta), 0, 0.5 * xsi],
        [0.5 * eta * (1 + zeta), 0, 0.5 * (1 + zeta), 0.5 * eta]];
};

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
WedgeElement1.prototype.massMatrix = function(p, dens) {
    var ja = 0, i;
    for (i = 0; i < 2; i++) {
        var sf = this.shapeFunction(WEDGE1_INT[i][0], WEDGE1_INT[i][1],
            WEDGE1_INT[i][2]);
        ja += this.jacobianMatrix(p, sf).determinant();
    }
    var v0 = dens * ja / 36, m = numeric.rep([18, 18], 0);
    for (i = 0; i < 6; i++) {
        for (var j = 0; j < 6; j++) {
            var value = v0, i3 = 3 * i, j3 = 3 * j;
            if (i % 3 != j % 3) value *= 0.5;
            if (Math.floor(i / 3) != Math.floor(j / 3)) value *= 0.5;
            m[i3][j3] += value;
            m[i3 + 1][j3 + 1] += value;
            m[i3 + 2][j3 + 2] += value;
        }
    }
    return m;
};
