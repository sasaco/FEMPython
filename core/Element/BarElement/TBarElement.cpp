
//--------------------------------------------------------------------//
// Timoshenko���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - ���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// axis - �f�ʊ�����x�N�g��
var TBarElement = function(label, material, param, nodes, axis) {
    BarElement.call(this, label, material, param, nodes, axis);
};

// �v�f���E���̂�Ԃ�
TBarElement.prototype.getName = function() {
    return 'TBarElement';
};

// �����}�g���b�N�X�̗��Ȃ�������Ԃ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
TBarElement.prototype.stiffBend = function(l, material, sect) {
    var kb = material.ee / l, kby = kb * sect.iy, kbz = kb * sect.iz;
    var ksc1 = sect.shearCoef() * material.gg * sect.area / l;
    var ksc2y = 12 * kby / l, kscy = ksc1 * ksc2y / (ksc1 + ksc2y);	// MacNeal�̕␳
    var ksc2z = 12 * kbz / l, kscz = ksc1 * ksc2z / (ksc1 + ksc2z);
    var lh = 0.5 * l, ksyl = kscy * lh, ksyl2 = ksyl * lh, kszl = kscz * lh, kszl2 = kszl * lh;
    return [[kscy, ksyl, -kscy, ksyl], [ksyl, kby + ksyl2, -ksyl, -kby + ksyl2],
        [-kscy, -ksyl, kscy, -ksyl], [ksyl, -kby + ksyl2, -ksyl, kby + ksyl2],
        [kscz, -kszl, -kscz, -kszl], [-kszl, kbz + kszl2, kszl, -kbz + kszl2],
        [-kscz, kszl, kscz, kszl], [-kszl, -kbz + kszl2, kszl, kbz + kszl2]];
};

// �Ȃ��ȗ��E����f�c��Ԃ�
// v - �Ǐ����W�n�̕ψ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
TBarElement.prototype.bendCurveShare = function(v, l, material, sect) {
    var cs1 = sect.shearCoef() / l, cs2 = 0.5 * sect.shearCoef();
    var ckap1 = cs2 * material.gg * sect.area * l / material.ee;
    var ckap1y = ckap1 / sect.iy, ckap1z = ckap1 / sect.iz;
    var ckap2y = ckap1y * 0.5 * l, ckap2z = ckap1z * 0.5 * l, ckap3 = 1 / l;
    var sy = cs1 * (v[7] - v[1]) - cs2 * (v[5] + v[11]);
    var sz = cs1 * (v[8] - v[2]) + cs2 * (v[4] + v[10]);
    var kpy = [ckap1y * (v[1] - v[7]) + (ckap2y + ckap3) * v[5] +
        (ckap2y - ckap3) * v[11],
        ckap1y * (v[1] - v[7]) + (ckap2y - ckap3) * v[5] +
        (ckap2y + ckap3) * v[11]];
    var kpz = [-ckap1z * (v[2] - v[8]) + (ckap2z + ckap3) * v[4] +
        (ckap2z - ckap3) * v[10],
        -ckap1z * (v[2] - v[8]) + (ckap2z - ckap3) * v[4] +
        (ckap2z + ckap3) * v[10]];
    return[kpy, kpz, sy, sz];
};

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
// sect - ���f�ʃp�����[�^
TBarElement.prototype.massMatrix = function(p, dens, sect) {
    var l = p[0].distanceTo(p[1]), d = dirMatrix(p, this.axis);
    var mi = sect.massInertia(dens, l), dm = C1_3 * mi[0];
    var di = [C1_3 * mi[1], C1_3 * mi[2], C1_3 * mi[3]];
    var m = numeric.diag([dm, dm, dm, di[0], di[1], di[2],
        dm, dm, dm, di[0], di[1], di[2]]);
    for (var i = 0; i < 3; i++) {
        m[i][i + 6] = 0.5 * dm;
        m[i + 6][i] = 0.5 * dm;
        m[i + 3][i + 9] = 0.5 * di[i];
        m[i + 9][i + 3] = 0.5 * di[i];
    }
    toDir3(d, m);
    return m;
};