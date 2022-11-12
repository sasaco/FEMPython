
//--------------------------------------------------------------------//
// 六面体セレンディピティ族2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var HexaElement2 = function(label, material, nodes) {
    SolidElement.call(this, label, material, nodes, HEXA2_NODE, HEXA2_INT);
};

// 要素名称を返す
HexaElement2.prototype.getName = function() {
    return 'HexaElement2';
};

// 節点数を返す
HexaElement2.prototype.nodeCount = function() {
    return 20;
};

// 要素境界数を返す
HexaElement2.prototype.borderCount = function() {
    return 6;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
HexaElement2.prototype.border = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new QuadangleBorder2
        (element, [p[0], p[3], p[2], p[1], p[11], p[10], p[9], p[8]]);
    case 1:
        return new QuadangleBorder2
        (element, [p[4], p[5], p[6], p[7], p[12], p[13], p[14], p[15]]);
    case 2:
        return new QuadangleBorder2
        (element, [p[0], p[1], p[5], p[4], p[8], p[17], p[12], p[16]]);
    case 3:
        return new QuadangleBorder2
        (element, [p[1], p[2], p[6], p[5], p[9], p[18], p[13], p[17]]);
    case 4:
        return new QuadangleBorder2
        (element, [p[2], p[3], p[7], p[6], p[10], p[19], p[14], p[18]]);
    case 5:
        return new QuadangleBorder2
        (element, [p[3], p[0], p[4], p[7], p[11], p[16], p[15], p[19]]);
    }
};

// 要素を鏡像反転する
HexaElement2.prototype.mirror = function() {
    swap(this.nodes, 1, 3);
    swap(this.nodes, 5, 7);
    swap(this.nodes, 8, 11);
    swap(this.nodes, 9, 10);
    swap(this.nodes, 12, 15);
    swap(this.nodes, 13, 14);
    swap(this.nodes, 17, 19);
};

// 要素節点の角度を返す
// p - 要素節点
HexaElement2.prototype.angle = function(p) {
    return[solidAngle(p[0], p[8], p[11], p[16]),
        solidAngle(p[1], p[9], p[8], p[17]),
        solidAngle(p[2], p[10], p[9], p[18]),
        solidAngle(p[3], p[11], p[10], p[19]),
        solidAngle(p[4], p[12], p[15], p[16]),
        solidAngle(p[5], p[13], p[12], p[17]),
        solidAngle(p[6], p[14], p[13], p[18]),
        solidAngle(p[7], p[15], p[14], p[19]),
        planeAngle(p[8], p[12], p[10]), planeAngle(p[9], p[13], p[11]),
        planeAngle(p[10], p[14], p[8]), planeAngle(p[11], p[15], p[9]),
        planeAngle(p[12], p[8], p[14]), planeAngle(p[13], p[9], p[15]),
        planeAngle(p[14], p[10], p[12]), planeAngle(p[15], p[11], p[13]),
        planeAngle(p[16], p[17], p[19]), planeAngle(p[17], p[18], p[16]),
        planeAngle(p[18], p[19], p[17]), planeAngle(p[19], p[16], p[18])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
HexaElement2.prototype.shapeFunction = function(xsi, eta, zeta) {
    return [[0.125 * (1 - xsi) * (1 - eta) * (1 - zeta) * (-xsi - eta - zeta - 2),
        0.125 * (1 - eta) * (1 - zeta) * (2 * xsi + eta + zeta + 1),
        0.125 * (1 - xsi) * (1 - zeta) * (xsi + 2 * eta + zeta + 1),
        0.125 * (1 - xsi) * (1 - eta) * (xsi + eta + 2 * zeta + 1)],
        [0.125 * (1 + xsi) * (1 - eta) * (1 - zeta) * (xsi - eta - zeta - 2),
        0.125 * (1 - eta) * (1 - zeta) * (2 * xsi - eta - zeta - 1),
        0.125 * (1 + xsi) * (1 - zeta) * (-xsi + 2 * eta + zeta + 1),
        0.125 * (1 + xsi) * (1 - eta) * (-xsi + eta + 2 * zeta + 1)],
        [0.125 * (1 + xsi) * (1 + eta) * (1 - zeta) * (xsi + eta - zeta - 2),
        0.125 * (1 + eta) * (1 - zeta) * (2 * xsi + eta - zeta - 1),
        0.125 * (1 + xsi) * (1 - zeta) * (xsi + 2 * eta - zeta - 1),
        0.125 * (1 + xsi) * (1 + eta) * (-xsi - eta + 2 * zeta + 1)],
        [0.125 * (1 - xsi) * (1 + eta) * (1 - zeta) * (-xsi + eta - zeta - 2),
        0.125 * (1 + eta) * (1 - zeta) * (2 * xsi - eta + zeta + 1),
        0.125 * (1 - xsi) * (1 - zeta) * (-xsi + 2 * eta - zeta - 1),
        0.125 * (1 - xsi) * (1 + eta) * (xsi - eta + 2 * zeta + 1)],
        [0.125 * (1 - xsi) * (1 - eta) * (1 + zeta) * (-xsi - eta + zeta - 2),
        0.125 * (1 - eta) * (1 + zeta) * (2 * xsi + eta - zeta + 1),
        0.125 * (1 - xsi) * (1 + zeta) * (xsi + 2 * eta - zeta + 1),
        0.125 * (1 - xsi) * (1 - eta) * (-xsi - eta + 2 * zeta - 1)],
        [0.125 * (1 + xsi) * (1 - eta) * (1 + zeta) * (xsi - eta + zeta - 2),
        0.125 * (1 - eta) * (1 + zeta) * (2 * xsi - eta + zeta - 1),
        0.125 * (1 + xsi) * (1 + zeta) * (-xsi + 2 * eta - zeta + 1),
        0.125 * (1 + xsi) * (1 - eta) * (xsi - eta + 2 * zeta - 1)],
        [0.125 * (1 + xsi) * (1 + eta) * (1 + zeta) * (xsi + eta + zeta - 2),
        0.125 * (1 + eta) * (1 + zeta) * (2 * xsi + eta + zeta - 1),
        0.125 * (1 + xsi) * (1 + zeta) * (xsi + 2 * eta + zeta - 1),
        0.125 * (1 + xsi) * (1 + eta) * (xsi + eta + 2 * zeta - 1)],
        [0.125 * (1 - xsi) * (1 + eta) * (1 + zeta) * (-xsi + eta + zeta - 2),
        0.125 * (1 + eta) * (1 + zeta) * (2 * xsi - eta - zeta + 1),
        0.125 * (1 - xsi) * (1 + zeta) * (-xsi + 2 * eta + zeta - 1),
        0.125 * (1 - xsi) * (1 + eta) * (-xsi + eta + 2 * zeta - 1)],
        [0.25 * (1 + xsi) * (1 - xsi) * (1 - eta) * (1 - zeta),
        -0.5 * xsi * (1 - eta) * (1 - zeta),
        -0.25 * (1 + xsi) * (1 - xsi) * (1 - zeta),
        -0.25 * (1 + xsi) * (1 - xsi) * (1 - eta)],
        [0.25 * (1 + xsi) * (1 + eta) * (1 - eta) * (1 - zeta),
        0.25 * (1 + eta) * (1 - eta) * (1 - zeta),
        -0.5 * (1 + xsi) * eta * (1 - zeta),
        -0.25 * (1 + xsi) * (1 + eta) * (1 - eta)],
        [0.25 * (1 + xsi) * (1 - xsi) * (1 + eta) * (1 - zeta),
        -0.5 * xsi * (1 + eta) * (1 - zeta),
        0.25 * (1 + xsi) * (1 - xsi) * (1 - zeta),
        -0.25 * (1 + xsi) * (1 - xsi) * (1 + eta)],
        [0.25 * (1 - xsi) * (1 + eta) * (1 - eta) * (1 - zeta),
        -0.25 * (1 + eta) * (1 - eta) * (1 - zeta),
        -0.5 * (1 - xsi) * eta * (1 - zeta),
        -0.25 * (1 - xsi) * (1 + eta) * (1 - eta)],
        [0.25 * (1 + xsi) * (1 - xsi) * (1 - eta) * (1 + zeta),
        -0.5 * xsi * (1 - eta) * (1 + zeta),
        -0.25 * (1 + xsi) * (1 - xsi) * (1 + zeta),
        0.25 * (1 + xsi) * (1 - xsi) * (1 - eta)],
        [0.25 * (1 + xsi) * (1 + eta) * (1 - eta) * (1 + zeta),
        0.25 * (1 + eta) * (1 - eta) * (1 + zeta),
        -0.5 * (1 + xsi) * eta * (1 + zeta),
        0.25 * (1 + xsi) * (1 + eta) * (1 - eta)],
        [0.25 * (1 + xsi) * (1 - xsi) * (1 + eta) * (1 + zeta),
        -0.5 * xsi * (1 + eta) * (1 + zeta),
        0.25 * (1 + xsi) * (1 - xsi) * (1 + zeta),
        0.25 * (1 + xsi) * (1 - xsi) * (1 + eta)],
        [0.25 * (1 - xsi) * (1 + eta) * (1 - eta) * (1 + zeta),
        -0.25 * (1 + eta) * (1 - eta) * (1 + zeta),
        -0.5 * (1 - xsi) * eta * (1 + zeta),
        0.25 * (1 - xsi) * (1 + eta) * (1 - eta)],
        [0.25 * (1 - xsi) * (1 - eta) * (1 + zeta) * (1 - zeta),
        -0.25 * (1 - eta) * (1 + zeta) * (1 - zeta),
        -0.25 * (1 - xsi) * (1 + zeta) * (1 - zeta),
        -0.5 * (1 - xsi) * (1 - eta) * zeta],
        [0.25 * (1 + xsi) * (1 - eta) * (1 + zeta) * (1 - zeta),
        0.25 * (1 - eta) * (1 + zeta) * (1 - zeta),
        -0.25 * (1 + xsi) * (1 + zeta) * (1 - zeta),
        -0.5 * (1 + xsi) * (1 - eta) * zeta],
        [0.25 * (1 + xsi) * (1 + eta) * (1 + zeta) * (1 - zeta),
        0.25 * (1 + eta) * (1 + zeta) * (1 - zeta),
        0.25 * (1 + xsi) * (1 + zeta) * (1 - zeta),
        -0.5 * (1 + xsi) * (1 + eta) * zeta],
        [0.25 * (1 - xsi) * (1 + eta) * (1 + zeta) * (1 - zeta),
        -0.25 * (1 + eta) * (1 + zeta) * (1 - zeta),
        0.25 * (1 - xsi) * (1 + zeta) * (1 - zeta),
        -0.5 * (1 - xsi) * (1 + eta) * zeta]];
};