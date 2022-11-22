#include "BarElement.h";

class BEBarElement : public BarElement {

};
//--------------------------------------------------------------------//
// Bernoulli-Euler���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - ���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// axis - �f�ʊ�����x�N�g��
var BEBarElement = function(label, material, param, nodes, axis) {
    BarElement.call(this, label, material, param, nodes, axis);
};

// �v�f���E���̂�Ԃ�
BEBarElement.prototype.getName = function() {
    return 'BEBarElement';
};

// �����}�g���b�N�X�̗��Ȃ�������Ԃ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
BEBarElement.prototype.stiffBend = function(l, material, sect) {
    var kc = material.ee / (l * l * l), kcy = kc * sect.iy, kcz = kc * sect.iz;
    var kcy12 = 12 * kcy, kcy6l = 6 * kcy * l, kcyll = kcy * l * l;
    var kcz12 = 12 * kcz, kcz6l = 6 * kcz * l, kczll = kcz * l * l;
    return [[kcy12, kcy6l, -kcy12, kcy6l], [kcy6l, 4 * kcyll, -kcy6l, 2 * kcyll],
        [-kcy12, -kcy6l, kcy12, -kcy6l], [kcy6l, 2 * kcyll, -kcy6l, 4 * kcyll],
        [kcz12, -kcz6l, -kcz12, -kcz6l], [-kcz6l, 4 * kczll, kcz6l, 2 * kczll],
        [-kcz12, kcz6l, kcz12, kcz6l], [-kcz6l, 2 * kczll, kcz6l, 4 * kczll]];
};


// �Ȃ��ȗ��E����f�c��Ԃ�
// v - �Ǐ����W�n�̕ψ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
BEBarElement.prototype.bendCurveShare = function(v, l, material, sect) {
    var ckap1 = 6 / (l * l), ckap2 = 4 / l, ckap3 = 0.5 * ckap2;
    var kpy = [ckap1 * (v[1] - v[7]) + ckap2 * v[5] + ckap3 * v[11],
        ckap1 * (v[1] - v[7]) + ckap3 * v[5] + ckap2 * v[11]];
    var kpz = [-ckap1 * (v[2] - v[8]) + ckap2 * v[4] + ckap3 * v[10],
        -ckap1 * (v[2] - v[8]) + ckap3 * v[4] + ckap2 * v[10]];
    return[kpy, kpz, 0, 0];
};

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
// sect - ���f�ʃp�����[�^
BEBarElement.prototype.massMatrix = function(p, dens, sect) {
    var l = p[0].distanceTo(p[1]), d = dirMatrix(p, this.axis);
    var mi = sect.massInertia(dens, l), m0 = mi[0], dm = C1_3 * m0, dix = C1_3 * mi[1];
    var m = numeric.rep([12, 12], 0);
    m[0][0] = dm;
    m[0][6] = 0.5 * dm;
    m[6][0] = 0.5 * dm;
    m[6][6] = dm;
    m[3][3] = dix;
    m[3][9] = 0.5 * dix;
    m[9][3] = 0.5 * dix;
    m[9][9] = dix;
    var m1 = m0 * 13 / 35, m2 = m0 * 11 * l / 210, m3 = m0 * 9 / 70, m4 = m0 * 13 * l / 420;
    var m5 = m0 * l * l / 105, m6 = m0 * l * l / 140;
    var mm1 = [[m1, m2, m3, -m4], [m2, m5, m4, -m6],
        [m3, m4, m1, -m2], [-m4, -m6, -m2, m5]];
    var mm2 = [[m1, -m2, m3, m4], [-m2, m5, -m4, -m6],
        [m3, -m4, m1, m2], [m4, -m6, m2, m5]];
    for (var i = 0; i < 4; i++) {
        var mi1 = m[I_YMZ[i]], mi2 = m[I_ZMY[i]], mmi1 = mm1[i], mmi2 = mm2[i];
        for (var j = 0; j < 4; j++) {
            mi1[I_YMZ[j]] = mmi1[j];
            mi2[I_ZMY[j]] = mmi2[j];
        }
    }
    toDir3(d, m);
    return m;
};