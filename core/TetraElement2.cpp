
//--------------------------------------------------------------------//
// �l�ʑ�2���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var TetraElement2 = function(label, material, nodes) {
    SolidElement.call(this, label, material, nodes, TETRA2_NODE, TETRA2_INT);
};

// �v�f���̂�Ԃ�
TetraElement2.prototype.getName = function() {
    return 'TetraElement2';
};

// �ߓ_����Ԃ�
TetraElement2.prototype.nodeCount = function() {
    return 10;
};

// �v�f���E����Ԃ�
TetraElement2.prototype.borderCount = function() {
    return 4;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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

// �v�f���������]����
TetraElement2.prototype.mirror = function() {
    swap(this.nodes, 1, 2);
    swap(this.nodes, 4, 6);
    swap(this.nodes, 8, 9);
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
TetraElement2.prototype.angle = function(p) {
    return[solidAngle(p[0], p[4], p[6], p[7]),
        solidAngle(p[1], p[5], p[4], p[8]),
        solidAngle(p[2], p[6], p[5], p[9]),
        solidAngle(p[3], p[7], p[8], p[9]),
        planeAngle(p[4], p[2], p[3]), planeAngle(p[5], p[0], p[3]),
        planeAngle(p[6], p[1], p[3]), planeAngle(p[7], p[1], p[2]),
        planeAngle(p[8], p[2], p[0]), planeAngle(p[9], p[0], p[1])];
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
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
