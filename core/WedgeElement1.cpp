
class WedgeElement1 {

private:


public:


};

//--------------------------------------------------------------------//
// ���`1���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
var WedgeElement1 = function(label, material, nodes) {
    SolidElement.call(this, label, material, nodes, WEDGE1_NODE, WEDGE1_INT);
};

// �v�f���̂�Ԃ�
WedgeElement1.prototype.getName = function() {
    return 'WedgeElement1';
};

// �ߓ_����Ԃ�
WedgeElement1.prototype.nodeCount = function() {
    return 6;
};

// �v�f���E����Ԃ�
WedgeElement1.prototype.borderCount = function() {
    return 5;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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

// �v�f���������]����
WedgeElement1.prototype.mirror = function() {
    swap(this.nodes, 1, 2);
    swap(this.nodes, 4, 5);
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
WedgeElement1.prototype.angle = function(p) {
    var th = [];
    for (var i = 0; i < 3; i++) {
        th[i] = solidAngle(p[i], p[(i + 1) % 3], p[(i + 2) % 3], p[i + 3]);
        th[i + 3] = solidAngle(p[i + 3], p[(i + 1) % 3 + 3], p[(i + 2) % 3 + 3], p[i]);
    }
    return th;
};

// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
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

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
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
