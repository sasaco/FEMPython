
//--------------------------------------------------------------------//
// 楔形2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
var WedgeElement2 = function(label, material, nodes) {
    SolidElement.call(this, label, material, nodes, WEDGE2_NODE, WEDGE2_INT);
};

// 要素名称を返す
WedgeElement2.prototype.getName = function() {
    return 'WedgeElement2';
};

// 節点数を返す
WedgeElement2.prototype.nodeCount = function() {
    return 15;
};

// 要素境界数を返す
WedgeElement2.prototype.borderCount = function() {
    return 5;
};

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
WedgeElement2.prototype.border = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new TriangleBorder2
        (element, [p[0], p[2], p[1], p[8], p[7], p[6]]);
    case 1:
        return new TriangleBorder2
        (element, [p[3], p[4], p[5], p[9], p[10], p[11]]);
    case 2:
        return new QuadangleBorder2
        (element, [p[0], p[1], p[4], p[3], p[6], p[13], p[9], p[12]]);
    case 3:
        return new QuadangleBorder2
        (element, [p[1], p[2], p[5], p[4], p[7], p[14], p[10], p[13]]);
    case 4:
        return new QuadangleBorder2
        (element, [p[2], p[0], p[3], p[5], p[8], p[12], p[11], p[14]]);
    }
};

// 要素を鏡像反転する
WedgeElement2.prototype.mirror = function() {
    swap(this.nodes, 1, 2);
    swap(this.nodes, 4, 5);
    swap(this.nodes, 6, 8);
    swap(this.nodes, 9, 11);
    swap(this.nodes, 13, 14);
};

// 要素節点の角度を返す
// p - 要素節点
WedgeElement2.prototype.angle = function(p) {
    return[solidAngle(p[0], p[6], p[8], p[12]),
        solidAngle(p[1], p[6], p[7], p[14]),
        solidAngle(p[2], p[7], p[8], p[14]),
        solidAngle(p[3], p[9], p[11], p[12]),
        solidAngle(p[4], p[10], p[9], p[13]),
        solidAngle(p[5], p[11], p[10], p[14]),
        planeAngle(p[6], p[2], p[9]), planeAngle(p[7], p[0], p[10]),
        planeAngle(p[8], p[1], p[11]), planeAngle(p[9], p[6], p[5]),
        planeAngle(p[10], p[7], p[3]), planeAngle(p[11], p[8], p[4]),
        planeAngle(p[12], p[13], p[14]), planeAngle(p[13], p[14], p[12]),
        planeAngle(p[14], p[12], p[13])];
};

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
WedgeElement2.prototype.shapeFunction = function(xsi, eta, zeta) {
    var xe = 1 - xsi - eta;
    return [[xe * (xe - 0.5 * zeta - 1) * (1 - zeta), -(2 * xe - 0.5 * zeta - 1) * (1 - zeta),
        -(2 * xe - 0.5 * zeta - 1) * (1 - zeta), xe * (zeta - xe + 0.5)],
        [xsi * (xsi - 0.5 * zeta - 1) * (1 - zeta), (2 * xsi - 0.5 * zeta - 1) * (1 - zeta),
        0, xsi * (zeta - xsi + 0.5)],
        [eta * (eta - 0.5 * zeta - 1) * (1 - zeta), 0,
        (2 * eta - 0.5 * zeta - 1) * (1 - zeta), eta * (zeta - eta + 0.5)],
        [xe * (xe + 0.5 * zeta - 1) * (1 + zeta), -(2 * xe + 0.5 * zeta - 1) * (1 + zeta),
        -(2 * xe + 0.5 * zeta - 1) * (1 + zeta), xe * (zeta + xe - 0.5)],
        [xsi * (xsi + 0.5 * zeta - 1) * (1 + zeta), (2 * xsi + 0.5 * zeta - 1) * (1 + zeta),
        0, xsi * (zeta + xsi - 0.5)],
        [eta * (eta + 0.5 * zeta - 1) * (1 + zeta), 0,
        (2 * eta + 0.5 * zeta - 1) * (1 + zeta), eta * (zeta + eta - 0.5)],
        [2 * xe * xsi * (1 - zeta), 2 * (xe - xsi) * (1 - zeta), -2 * xsi * (1 - zeta),
        -2 * xe * xsi],
        [2 * xsi * eta * (1 - zeta), 2 * eta * (1 - zeta), 2 * xsi * (1 - zeta),
        -2 * xsi * eta],
        [2 * eta * xe * (1 - zeta), -2 * eta * (1 - zeta), 2 * (xe - eta) * (1 - zeta),
        -2 * eta * xe],
        [2 * xe * xsi * (1 + zeta), 2 * (xe - xsi) * (1 + zeta), -2 * xsi * (1 + zeta),
        2 * xe * xsi],
        [2 * xsi * eta * (1 + zeta), 2 * eta * (1 + zeta), 2 * xsi * (1 + zeta),
        2 * xsi * eta],
        [2 * eta * xe * (1 + zeta), -2 * eta * (1 + zeta), 2 * (xe - eta) * (1 + zeta),
        2 * eta * xe],
        [xe * (1 + zeta) * (1 - zeta), -(1 + zeta) * (1 - zeta), -(1 + zeta) * (1 - zeta),
        -2 * xe * zeta],
        [xsi * (1 + zeta) * (1 - zeta), (1 + zeta) * (1 - zeta), 0, -2 * xsi * zeta],
        [eta * (1 + zeta) * (1 - zeta), 0, (1 + zeta) * (1 - zeta), -2 * eta * zeta]];
};
