
//--------------------------------------------------------------------//
// 四面体2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var TetraElement2 = function(label, material, nodes) {
    SolidElement.call(this, label, material, nodes, TETRA2_NODE, TETRA2_INT);
};

// 要素名称を返す
TetraElement2.prototype.getName = function() {
    return 'TetraElement2';
};

// 節点数を返す
TetraElement2.prototype.nodeCount = function() {
    return 10;
};

// 要素境界数を返す
TetraElement2.prototype.borderCount = function() {
    return 4;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
TetraElement2.prototype.border = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new TriangleBorder2
        (element, [p[0], p[2], p[1], p[6], p[5], p[4]]);
    case 1:
        return new TriangleBorder2
        (element, [p[0], p[1], p[3], p[4], p[8], p[7]]);
    case 2:
        return new TriangleBorder2
        (element, [p[1], p[2], p[3], p[5], p[9], p[8]]);
    case 3:
        return new TriangleBorder2
        (element, [p[2], p[0], p[3], p[6], p[7], p[9]]);
    }
};

// 要素を鏡像反転する
TetraElement2.prototype.mirror = function() {
    swap(this.nodes, 1, 2);
    swap(this.nodes, 4, 6);
    swap(this.nodes, 8, 9);
};

// 要素節点の角度を返す
// p - 要素節点
TetraElement2.prototype.angle = function(p) {
    return[solidAngle(p[0], p[4], p[6], p[7]),
        solidAngle(p[1], p[5], p[4], p[8]),
        solidAngle(p[2], p[6], p[5], p[9]),
        solidAngle(p[3], p[7], p[8], p[9]),
        planeAngle(p[4], p[2], p[3]), planeAngle(p[5], p[0], p[3]),
        planeAngle(p[6], p[1], p[3]), planeAngle(p[7], p[1], p[2]),
        planeAngle(p[8], p[2], p[0]), planeAngle(p[9], p[0], p[1])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
TetraElement2.prototype.shapeFunction = function(xsi, eta, zeta) {
    var xez = 1 - xsi - eta - zeta;
    return [[xez * (2 * xez - 1), 1 - 4 * xez, 1 - 4 * xez, 1 - 4 * xez],
        [xsi * (2 * xsi - 1), 4 * xsi - 1, 0, 0],
        [eta * (2 * eta - 1), 0, 4 * eta - 1, 0],
        [zeta * (2 * zeta - 1), 0, 0, 4 * zeta - 1],
        [4 * xez * xsi, 4 * (xez - xsi), -4 * xsi, -4 * xsi],
        [4 * xsi * eta, 4 * eta, 4 * xsi, 0],
        [4 * xez * eta, -4 * eta, 4 * (xez - eta), -4 * eta],
        [4 * xez * zeta, -4 * zeta, -4 * zeta, 4 * (xez - zeta)],
        [4 * xsi * zeta, 4 * zeta, 0, 4 * xsi],
        [4 * eta * zeta, 0, 4 * zeta, 4 * eta]];
};
