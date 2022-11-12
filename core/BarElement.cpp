var I_YMZ = [1, 5, 7, 11];	// y������,z�����萬���̃C���f�b�N�X
var I_ZMY = [2, 4, 8, 10];	// z������,y�����萬���̃C���f�b�N�X

//--------------------------------------------------------------------//
// ���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - ���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// axis - �f�ʊ�����x�N�g��
var BarElement = function(label, material, param, nodes, axis) {
    FElement.call(this, label, material, nodes);
    this.param = param;
    this.isBar = true;
    if ((axis != = null) && (axis != = undefined)) axis.normalize();
    this.axis = axis;
};

// �ߓ_����Ԃ�
BarElement.prototype.nodeCount = function() {
    return 2;
};

// �v�f���E����Ԃ�
BarElement.prototype.borderCount = function() {
    return 1;
};

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
BarElement.prototype.border = function(element, index) {
    var p = this.nodes;
    switch (index) {
    default:
        return null;
    case 0:
        return new EdgeBorder1(element, [p[0], p[1]]);
    }
};

// �v�f���E�ӂ�Ԃ� ���v�f�ł͗v�f���E�Ɠ���
// element - �v�f���x��
// index - �v�f���E�ӂ̃C���f�b�N�X
BarElement.prototype.borderEdge = function(element, index) {
    return this.border(element, index);
};

// �v�f�ߓ_�̊p�x��Ԃ� ���v�f�ł͂P
// p - �v�f�ߓ_
BarElement.prototype.angle = function(p) {
    return[1, 1];
};

// �����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// material - �ޗ�
// sect - ���f�ʃp�����[�^
BarElement.prototype.stiffnessMatrix = function(p, material, sect) {
    var kk = numeric.rep([12, 12], 0), l = p[0].distanceTo(p[1]);
    var d = dirMatrix(p, this.axis);
    var kx = material.ee * sect.area / l, kt = material.gg * sect.ip / l;
    kk[0][0] = kx;
    kk[0][6] = -kx;
    kk[6][0] = -kx;
    kk[6][6] = kx;
    kk[3][3] = kt;
    kk[3][9] = -kt;
    kk[9][3] = -kt;
    kk[9][9] = kt;
    var km = this.stiffBend(l, material, sect);
    for (var i = 0; i < 4; i++) {
        var kk1 = kk[I_YMZ[i]], kk2 = kk[I_ZMY[i]], kmi1 = km[i], kmi2 = km[i + 4];
        for (var j = 0; j < 4; j++) {
            kk1[I_YMZ[j]] = kmi1[j];
            kk2[I_ZMY[j]] = kmi2[j];
        }
    }
    toDir3(d, kk);
    return kk;
};

// �g�U�}�g���b�N�X [ ���Ni�E��NjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
// sect - ���f�ʃp�����[�^
BarElement.prototype.gradMatrix = function(p, coef, sect) {
    var c = coef * sect.area / p[0].distanceTo(p[1]);
    return [[c, -c], [-c, c]];
};

// �􉽍����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// material - �ޗ�
// sect - ���f�ʃp�����[�^
BarElement.prototype.geomStiffnessMatrix = function(p, u, material, sect) {
    var l2 = p[0].distanceToSquared(p[1]), d = dirMatrix(p, this.axis);
    var v = this.toLocalArray(u, d), kk = numeric.rep([12, 12], 0);
    var sx = material.ee * sect.area * (v[6] - v[0]) / l2;
    for (var i = 0; i < 3; i++) {
        kk[i][i] = sx;
        kk[i + 6][i] = -sx;
        kk[i][i + 6] = -sx;
        kk[i + 6][i + 6] = sx;
    }
    toDir3(d, kk);
    return kk;
};

// �ߓ_�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// material - �ޗ�
// sect - ���f�ʃp�����[�^
BarElement.prototype.strainStress = function(p, u, material, sect) {
    var l = p[0].distanceTo(p[1]), d = dirMatrix(p, this.axis);
    var v = this.toLocalArray(u, d);
    var strain1 = [], stress1 = [], energy1 = [], strain2 = [], stress2 = [], energy2 = [];
    var ex = (v[6] - v[0]) / l, thd = (v[9] - v[3]) / l;
    var ks = this.bendCurveShare(v, l, material, sect);
    var kpy = ks[0], kpz = ks[1], sy = ks[2], sz = ks[3];
    for (var i = 0; i < 2; i++) {
        var str = sect.strainStress(material, ex, thd, kpy[i], kpz[i], sy, sz);
        strain1[i] = new Strain(str[0]);
        stress1[i] = new Stress(str[1]);
        strain2[i] = new Strain(str[2]);
        stress2[i] = new Stress(str[3]);
        strain1[i].rotate(d);
        stress1[i].rotate(d);
        strain2[i].rotate(d);
        stress2[i].rotate(d);
        energy1[i] = 0.5 * strain1[i].innerProduct(stress1[i]);
        energy2[i] = 0.5 * strain2[i].innerProduct(stress2[i]);
    }
    return[strain1, stress1, energy1, strain2, stress2, energy2];
};

// �v�f�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// material - �ޗ�
// sect - ���f�ʃp�����[�^
BarElement.prototype.elementStrainStress = function(p, u, material, sect) {
    var l = p[0].distanceTo(p[1]), d = dirMatrix(p, this.axis);
    var v = this.toLocalArray(u, d), i;
    var str = [];
    var ex = (v[6] - v[0]) / l, thd = (v[9] - v[3]) / l;
    var ks = this.bendCurveShare(v, l, material, sect);
    var kpy = ks[0], kpz = ks[1], sy = ks[2], sz = ks[3];
    for (i = 0; i < 2; i++) {
        str[i] = sect.strainStress(material, ex, thd, kpy[i], kpz[i], sy, sz);
    }
    var strain1 = new Strain
    (numeric.mul(0.5, numeric.add(str[0][0], str[1][0])));
    var stress1 = new Stress
    (numeric.mul(0.5, numeric.add(str[0][1], str[1][1])));
    var strain2 = new Strain
    (numeric.mul(0.5, numeric.add(str[0][2], str[1][2])));
    var stress2 = new Stress
    (numeric.mul(0.5, numeric.add(str[0][3], str[1][3])));
    strain1.rotate(d);
    stress1.rotate(d);
    strain2.rotate(d);
    stress2.rotate(d);
    var energy1 = 0.5 * strain1.innerProduct(stress1);
    var energy2 = 0.5 * strain2.innerProduct(stress2);
    return[strain1, stress1, energy1, strain2, stress2, energy2];
};

// �v�f��\���������Ԃ�
// materials - �ޗ�
// params - ���p�����[�^
// p - �ߓ_
BarElement.prototype.toString = function(materials, params, p) {
    var s = this.getName() + '\t' + this.label.toString(10) + '\t' +
        materials[this.material].label.toString(10) + '\t' +
        params[this.param].label.toString(10);
    for (var i = 0; i < this.nodes.length; i++) {
        s += '\t' + p[this.nodes[i]].label.toString(10);
    }
    return s;
};
