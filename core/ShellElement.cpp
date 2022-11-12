// �O�p�`1���v�f�̐ߓ_�̃�,�ō��W
var TRI1_NODE = [[0, 0], [1, 0], [0, 1]];
// �O�p�`1���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
var TRI1_INT = [[C1_3, C1_3, 0.5]];
// �O�p�`2���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
var TRI2_INT = [[GTRI2[0], GTRI2[0], C1_6], [GTRI2[1], GTRI2[0], C1_6],
[GTRI2[0], GTRI2[1], C1_6]];
// �l�p�`1���v�f�̐ߓ_�̃�,�ō��W
var QUAD1_NODE = [[-1, -1], [1, -1], [1, 1], [-1, 1]];
// �l�p�`1���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
var QUAD1_INT = [[GX2[0], GX2[0], 1], [GX2[1], GX2[0], 1], [GX2[0], GX2[1], 1],
[GX2[1], GX2[1], 1]];
// �O�p�`1���v�f�̎��ʃ}�g���b�N�X�W��
var TRI1_MASS1 = [[1, 0.5, 0.5], [0.5, 1, 0.5], [0.5, 0.5, 1]];

//--------------------------------------------------------------------//
// �V�F���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - �V�F���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// nodeP - �ߓ_�̃�,�ō��W
// intP - �ϕ��_�̃�,�ō��W,�d�݌W��
var ShellElement = function(label, material, param, nodes, nodeP, intP) {
    FElement.call(this, label, material, nodes);
    this.param = param;
    this.isShell = true;
    this.nodeP = nodeP;
    this.intP = intP;
};

// �v�f���E����Ԃ�
ShellElement.prototype.borderCount = function() {
    return 2;
};

// �v�f�ߓ_�̊p�x��Ԃ�
// p - �v�f�ߓ_
ShellElement.prototype.angle = function(p) {
    var th = [], count = this.nodeCount();
    for (var i = 0; i < count; i++) {
        th[i] = planeAngle(p[i], p[(i + 1) % count], p[(i + count - 1) % count]);
    }
    return th;
};

// ���R�r�s���Ԃ�
// p - �v�f�ߓ_
// sf - �`��֐��s��
// n - �@���x�N�g��
// t - �v�f����
ShellElement.prototype.jacobianMatrix = function(p, sf, n, t) {
    var count = this.nodeCount(), jac = [0, 0, 0, 0, 0, 0, 0, 0, 0];
    for (var i = 0; i < count; i++) {
        var sfi = sf[i], ppi = p[i], pix = ppi.x, piy = ppi.y, piz = ppi.z;
        for (var j = 0; j < 2; j++) {
            var sfij = sfi[j + 1];
            jac[j] += sfij * pix;
            jac[j + 3] += sfij * piy;
            jac[j + 6] += sfij * piz;
        }
    }
    jac[2] = 0.5 * t * n.x;
    jac[5] = 0.5 * t * n.y;
    jac[8] = 0.5 * t * n.z;
    return new THREE.Matrix3().fromArray(jac);
};

// �t���R�r�s���Ԃ�
// ja - ���R�r�s��
// d - �����]���}�g���b�N�X
ShellElement.prototype.jacobInv = function(ja, d) {
    var e1 = ja.elements;
    var jd = new THREE.Matrix3().set
    (e1[0] * d[0][0] + e1[3] * d[1][0] + e1[6] * d[2][0],
        e1[0] * d[0][1] + e1[3] * d[1][1] + e1[6] * d[2][1],
        e1[0] * d[0][2] + e1[3] * d[1][2] + e1[6] * d[2][2],
        e1[1] * d[0][0] + e1[4] * d[1][0] + e1[7] * d[2][0],
        e1[1] * d[0][1] + e1[4] * d[1][1] + e1[7] * d[2][1],
        e1[1] * d[0][2] + e1[4] * d[1][2] + e1[7] * d[2][2],
        0, 0, e1[2] * d[0][2] + e1[5] * d[1][2] + e1[8] * d[2][2]);
    return new THREE.Matrix3().getInverse(jd, true);
};

// �`��֐��̌��z [ dNi/dx dNi/dy ] ��Ԃ�
// p - �v�f�ߓ_
// ja - ���R�r�s��
// sf - �`��֐��s��
// d - �����]���}�g���b�N�X
// t - �v�f����
ShellElement.prototype.grad = function(p, ja, sf, d, t) {
    var count = this.nodeCount(), gr = [];
    var ji = this.jacobInv(ja, d).elements;
    for (var i = 0; i < count; i++) {
        var sfi = sf[i], dndxsi = sfi[1], dndeta = sfi[2];
        gr[i] = [ji[0] * dndxsi + ji[3] * dndeta, ji[1] * dndxsi + ji[4] * dndeta,
            ji[2] * dndxsi + ji[5] * dndeta];
    }
    return gr;
};

// �c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// ja - ���R�r�s��
// sf - �`��֐��s��
// d - �����]���}�g���b�N�X
ShellElement.prototype.strainMatrix1 = function(ja, sf, d) {
    var count = this.nodeCount(), m = numeric.rep([count, 4], 0);
    var ji = this.jacobInv(ja, d).elements;
    for (var i = 0; i < count; i++) {
        var mi = m[i], sfi = sf[i];
        for (var j = 0; j < 3; j++) {
            mi[j] = ji[j] * sfi[1] + ji[j + 3] * sfi[2];
        }
        mi[3] = ji[8] * sfi[0];
    }
    return m;
};

// �c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// �������c�͗v�f�ʍ��W�A�ψʂ͑S�̍��W
// ja - ���R�r�s��
// sf - �`��֐��s��
// d - �����]���}�g���b�N�X
// zeta - �ߓ_�̃č��W
// t - �v�f����
ShellElement.prototype.strainMatrix = function(ja, sf, d, zeta, t) {
    var b = this.strainMatrix1(ja, sf, d), z = 0.5 * t * zeta;
    var count = this.nodeCount(), m1 = numeric.rep([5, 6], 0);
    var matrix = numeric.rep([6 * count, 5], 0);
    for (var i = 0; i < count; i++) {
        var bi = b[i];
        m1[0][0] = bi[0];
        m1[0][4] = z * bi[0];
        m1[1][1] = bi[1];
        m1[1][3] = -z * bi[1];
        m1[2][0] = bi[1];
        m1[2][1] = bi[0];
        m1[2][3] = -z * bi[0];
        m1[2][4] = z * bi[1];
        m1[3][1] = bi[2];
        m1[3][2] = bi[1];
        m1[3][3] = -0.5 * t * bi[3] - z * bi[2];
        m1[4][0] = bi[2];
        m1[4][2] = bi[0];
        m1[4][4] = 0.5 * t * bi[3] + z * bi[2];
        var ib = 6 * i;
        for (var i1 = 0; i1 < 5; i1++) {
            var m1i = m1[i1];
            for (var j1 = 0; j1 < 3; j1++) {
                var dj = d[j1], s1 = 0, s2 = 0;
                for (var k1 = 0; k1 < 3; k1++) {
                    s1 += m1i[k1] * dj[k1];
                    s2 += m1i[k1 + 3] * dj[k1];
                }
                matrix[ib + j1][i1] += s1;
                matrix[ib + 3 + j1][i1] += s2;
            }
        }
    }
    return matrix;
};

// �ϕ��_�̌`��֐��}�g���b�N�X [ NiNj ] ��Ԃ�
// p - �v�f�ߓ_
// x - ��,��,�č��W
// w - �d�݌W��
// t - �v�f����
ShellElement.prototype.shapePart = function(p, x, w, t) {
    var sf = this.shapeFunction(x[0], x[1]);
    var ja = this.jacobianMatrix(p, sf, normalVector(p), t);
    var count = this.nodeCount(), matrix = [];
    var coef = 2 * w * Math.abs(ja.determinant());
    for (var i = 0; i < count; i++) {
        var matr = [], cf2 = coef * sf[i][0];
        for (var j = 0; j < count; j++) {
            matr[j] = cf2 * sf[j][0];
        }
        matrix[i] = matr;
    }
    return matrix;
};

// �ϕ��_�̊g�U�}�g���b�N�X [ ��Ni�E��Nj ] ��Ԃ�
// p - �v�f�ߓ_
// x - ��,��,�č��W
// w - �d�݌W��
// t - �v�f����
ShellElement.prototype.gradPart = function(p, x, w, t) {
    var sf = this.shapeFunction(x[0], x[1]);
    var ja = this.jacobianMatrix(p, sf, normalVector(p), t);
    var gr = this.grad(p, ja, sf, dirMatrix(p), t);
    var count = this.nodeCount(), matrix = [];
    var coef = 2 * w * Math.abs(ja.determinant());
    for (var i = 0; i < count; i++) {
        var matr = [], gri = gr[i];
        var c1 = coef * gri[0], c2 = coef * gri[1], c3 = coef * gri[2];
        for (var j = 0; j < count; j++) {
            var grj = gr[j];
            matr[j] = c1 * grj[0] + c2 * grj[1] + c3 * grj[2];
        }
        matrix[i] = matr;
    }
    return matrix;
};

// �`��֐��}�g���b�N�X [ ��NiNjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
// sp - �V�F���p�����[�^
ShellElement.prototype.shapeFunctionMatrix = function(p, coef, sp) {
    var count = this.nodeCount(), s = numeric.rep([count, count], 0);
    var t = sp.thickness;
    for (var i = 0; i < this.intP.length; i++) {
        addMatrix(s, this.shapePart(p, this.intP[i], coef * this.intP[i][2], t));
    }
    return s;
};

// �g�U�}�g���b�N�X [ ���Ni�E��NjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
// sp - �V�F���p�����[�^
ShellElement.prototype.gradMatrix = function(p, coef, sp) {
    var count = this.nodeCount(), g = numeric.rep([count, count], 0);
    var t = sp.thickness;
    for (var i = 0; i < this.intP.length; i++) {
        addMatrix(g, this.gradPart(p, this.intP[i], coef * this.intP[i][2], t));
    }
    return g;
};

// �􉽍����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
// sp - �V�F���p�����[�^
ShellElement.prototype.geomStiffnessMatrix = function(p, u, d1, sp) {
    var count = this.nodeCount(), kk = numeric.rep([6 * count, 6 * count], 0);
    var d = dirMatrix(p), n = normalVector(p);
    var v = this.toArray(u, 6), t = sp.thickness;
    for (var i = 0; i < this.intP.length; i++) {
        var ip = this.intP[i];
        var sf = this.shapeFunction(ip[0], ip[1]);
        var ja = this.jacobianMatrix(p, sf, n, t);
        var gr = this.grad(p, ja, sf, d, t);
        var sm = this.strainMatrix(ja, sf, d, 0, t);
        var str = this.toStress(numeric.dotMV(d1, numeric.dotVM(v, sm)));
        var w = 2 * ip[2] * Math.abs(ja.determinant());
        for (var i1 = 0; i1 < count; i1++) {
            var i6 = 6 * i1, gri = gr[i1];
            for (var j1 = 0; j1 < count; j1++) {
                var j6 = 6 * j1, grj = gr[j1];
                var s = w * (gri[0] * (str.xx * grj[0] + str.xy * grj[1] + str.zx * grj[2]) +
                    gri[1] * (str.xy * grj[0] + str.yy * grj[1] + str.yz * grj[2]) +
                    gri[2] * (str.zx * grj[0] + str.yz * grj[1] + str.zz * grj[2]));
                kk[i6][j6] += s;
                kk[i6 + 1][j6 + 1] += s;
                kk[i6 + 2][j6 + 2] += s;
            }
        }
    }
    toDir3(d, kk);
    return kk;
};

// �ߓ_�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
// sp - �V�F���p�����[�^
ShellElement.prototype.strainStress = function(p, u, d1, sp) {
    var count = this.nodeCount(), d = dirMatrix(p), n = normalVector(p);
    var v = this.toArray(u, 6), t = sp.thickness;
    var strain1 = [], stress1 = [], energy1 = [], strain2 = [], stress2 = [], energy2 = [];
    for (var i = 0; i < count; i++) {
        var np = this.nodeP[i];
        var eps1 = this.strainPart(p, v, n, d, np[0], np[1], 1, t);
        var eps2 = this.strainPart(p, v, n, d, np[0], np[1], -1, t);
        strain1[i] = this.toStrain(eps1);
        stress1[i] = this.toStress(numeric.dotMV(d1, eps1));
        strain2[i] = this.toStrain(eps2);
        stress2[i] = this.toStress(numeric.dotMV(d1, eps2));
        strain1[i].rotate(d);
        stress1[i].rotate(d);
        strain2[i].rotate(d);
        stress2[i].rotate(d);
        energy1[i] = 0.5 * strain1[i].innerProduct(stress1[i]);
        energy2[i] = 0.5 * strain2[i].innerProduct(stress2[i]);
    }
    return[strain1, stress1, energy1, strain2, stress2, energy2];
};

// �v�f���̘c�x�N�g����Ԃ�
// p - �v�f�ߓ_
// v - �ߓ_�ψʃx�N�g��
// n - �@���x�N�g��
// d - �����]���}�g���b�N�X
// xsi,eta,zeta - ��,��,�č��W
// t - �v�f����
ShellElement.prototype.strainPart = function(p, v, n, d, xsi, eta, zeta, t) {
    var sf = this.shapeFunction(xsi, eta);
    var ja = this.jacobianMatrix(p, sf, n, t);
    var sm = this.strainMatrix(ja, sf, d, zeta, t);
    return numeric.dotVM(v, sm);
};

// �v�f�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
// sp - �V�F���p�����[�^
ShellElement.prototype.elementStrainStress = function(p, u, d1, sp) {
    var d = dirMatrix(p), n = normalVector(p), v = this.toArray(u, 6);
    var t = sp.thickness, cf = 1 / this.intP.length;
    var strain1 = [0, 0, 0, 0, 0, 0], stress1 = [0, 0, 0, 0, 0, 0], energy1 = 0;
    var strain2 = [0, 0, 0, 0, 0, 0], stress2 = [0, 0, 0, 0, 0, 0], energy2 = 0;
    for (var i = 0; i < this.intP.length; i++) {
        var ip = this.intP[i];
        var eps1 = this.strainPart(p, v, n, d, ip[0], ip[1], 1, t);
        var eps2 = this.strainPart(p, v, n, d, ip[0], ip[1], -1, t);
        strain1 = numeric.add(strain1, eps1);
        strain2 = numeric.add(strain2, eps2);
        var str1 = numeric.dotMV(d1, eps1);
        var str2 = numeric.dotMV(d1, eps2);
        stress1 = numeric.add(stress1, str1);
        stress2 = numeric.add(stress2, str2);
        energy1 += numeric.dotVV(eps1, str1);
        energy2 += numeric.dotVV(eps2, str2);
    }
    strain1 = numeric.mul(strain1, cf);
    stress1 = numeric.mul(stress1, cf);
    energy1 *= 0.5 * cf;
    strain2 = numeric.mul(strain1, cf);
    stress2 = numeric.mul(stress1, cf);
    energy2 *= 0.5 * cf;
    return[this.toStrain(strain1), this.toStress(stress1), energy1,
        this.toStrain(strain2), this.toStress(stress2), energy2];
};

// �x�N�g����c�ɕϊ�����
// s - �c�x�N�g��
ShellElement.prototype.toStrain = function(s) {
    return new Strain([s[0], s[1], 0, s[2], s[3], s[4]]);
};

// �x�N�g����c�ɕϊ�����
// s - �c�x�N�g��
ShellElement.prototype.toStress = function(s) {
    return new Stress([s[0], s[1], 0, s[2], s[3], s[4]]);
};

// �v�f��\���������Ԃ�
// materials - �ޗ�
// params - �V�F���p�����[�^
// p - �ߓ_
ShellElement.prototype.toString = function(materials, params, p) {
    var s = this.getName() + '\t' + this.label.toString(10) + '\t' +
        materials[this.material].label.toString(10) + '\t' +
        params[this.param].label.toString(10);
    for (var i = 0; i < this.nodes.length; i++) {
        s += '\t' + p[this.nodes[i]].label.toString(10);
    }
    return s;
};



// �ߓ_���W���Ǐ����W�n�ɕϊ�����
// d - �����]���}�g���b�N�X
// p - �v�f�ߓ_
function toLocal(d, p) {
    var x = [];
    for (var i = 0; i < p.length; i++) {
        x[i] = new THREE.Vector3().set
        (d[0][0] * p[i].x + d[1][0] * p[i].y + d[2][0] * p[i].z,
            d[0][1] * p[i].x + d[1][1] * p[i].y + d[2][1] * p[i].z,
            d[0][2] * p[i].x + d[1][2] * p[i].y + d[2][2] * p[i].z);
    }
    return x;
}