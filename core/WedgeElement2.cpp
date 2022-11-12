
//--------------------------------------------------------------------//
// ���`2���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var WedgeElement2 = function(label, material, nodes) {
    SolidElement.call(this, label, material, nodes, WEDGE2_NODE, WEDGE2_INT);
};

// �v�f���̂�Ԃ�
WedgeElement2.prototype.getName = function() {
    return 'WedgeElement2';
};

// �ߓ_����Ԃ�
WedgeElement2.prototype.nodeCount = function() {
    return 15;
};

// �v�f���E����Ԃ�
WedgeElement2.prototype.borderCount = function() {
    return 5;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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

// �v�f���������]����
WedgeElement2.prototype.mirror = function() {
    swap(this.nodes, 1, 2);
    swap(this.nodes, 4, 5);
    swap(this.nodes, 6, 8);
    swap(this.nodes, 9, 11);
    swap(this.nodes, 13, 14);
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
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

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
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
