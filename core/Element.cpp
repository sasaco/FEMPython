#include <string>
using namespace std;         //  ���O��Ԏw��


import "Material.h";
import "Nodes.h";

//--------------------------------------------------------------------//
// �v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
class FElement : public Nodes {

private:
    const double C1_3 = 1 / 3;
    const double C1_6 = 1 / 6;
    const double C1_12 = 1 / 12;
    const double C1_24 = 1 / 24;

    // �O�p�`2���v�f�̃K�E�X�ϕ��̐ϕ��_���W
    const double GTRI2[2] = { 1 / 6, 2 / 3 };
    // �l�ʑ�2���v�f�̃K�E�X�ϕ��̐ϕ��_���W
    const double GTETRA2[2] = { 0.25 - 0.05 * sqrt(5), 0.25 + 0.15 * sqrt(5) };
    // �l�p�`1���v�f�̃K�E�X�ϕ��̐ϕ��_���W
    const double GX2[2] = { -1 / sqrt(3), 1 / sqrt(3) };
    // �l�p�`2���v�f�̃K�E�X�ϕ��̐ϕ��_���W
    const double GX3[3] = { -sqrt(0.6), 0, sqrt(0.6)};
    // �K�E�X�ϕ��̏d�݌W��
    const double GW3[3] = { 5 / 9, 8 / 9, 5 / 9 };

public:


};

FElement::FElement(int label, Material material, int nodes) {
    Nodes.call(this, nodes);
    this.label = label;
    this.material = material;
    this.isShell = false;		// �V�F���v�f�ł͂Ȃ�
    this.isBar = false;		// ���v�f�ł͂Ȃ�
};

// �v�f���E��Ԃ�
// element - �v�f���x��
FElement.prototype.borders = function(element) {
    var count = this.borderCount(), borders = [];
    for (var i = 0; i < count; i++) borders[i] = this.border(element, i);
    return borders;
};

// �ϕ��_�̍����}�g���b�N�X��Ԃ�
// d - ����-�c�}�g���b�N�X
// b - �c-�ψʃ}�g���b�N�X�̓]�u�s��
// coef - �W��
FElement.prototype.stiffPart = function(d, b, coef) {
    var size1 = b.length, size2 = d.length, a = [], k = [], j;
    for (var i = 0; i < size1; i++) {
        a.length = 0;
        var bi = b[i];
        for (j = 0; j < size2; j++) {
            a[j] = coef * numeric.dotVV(bi, d[j]);
        }
        var ki = [];
        for (j = 0; j < size1; j++) {
            ki[j] = numeric.dotVV(a, b[j]);
        }
        k[i] = ki;
    }
    return k;
};

// �ߓ_�ψʂ�1�����z��ɕϊ�����
// u - �ߓ_�ψ�
// dof - �ߓ_���R�x
FElement.prototype.toArray = function(u, dof) {
    var count = this.nodeCount(), v = [];
    for (var i = 0; i < count; i++) {
        var ux = u[i].x;
        for (var j = 0; j < dof; j++) {
            v.push(ux[j]);
        }
    }
    return v;
};

// �ߓ_�ψʂ��Ǐ����W�n�E1�����z��ɕϊ�����
// u - �ߓ_�ψ�
// d - �����]���}�g���b�N�X
FElement.prototype.toLocalArray = function(u, d) {
    var v = [], j;
    for (var i = 0; i < 2; i++) {
        var ux = u[i].x;
        for (j = 0; j < 3; j++) {
            v.push(d[0][j] * ux[0] + d[1][j] * ux[1] + d[2][j] * ux[2]);
        }
        for (j = 0; j < 3; j++) {
            v.push(d[0][j] * ux[3] + d[1][j] * ux[4] + d[2][j] * ux[5]);
        }
    }
    return v;
};

// �ߓ_�����ւ���
// i1,i2 - �ߓ_�C���f�b�N�X
function swap(nodes, i1, i2) {
    var t = nodes[i1];
    nodes[i1] = nodes[i2];
    nodes[i2] = t;
}

// ���ʏ�̊p�x�����߂�
// p0 - ��_
// p1,p2 - ���_
function planeAngle(p0, p1, p2) {
    var v1 = p1.clone().sub(p0).normalize();
    var v2 = p2.clone().sub(p0).normalize();
    return Math.acos(Math.min(Math.max(v1.dot(v2), 0), 1));
}

// �O�p�`�̗��̊p�����ʉߏ肩�狁�߂�
// p0 - ��_
// p1,p2,p3 - ���_
function solidAngle(p0, p1, p2, p3) {
    var v1 = p1.clone().sub(p0);
    var v2 = p2.clone().sub(p0);
    var v3 = p3.clone().sub(p0);
    var v12 = v1.clone().cross(v2).normalize();
    var v23 = v2.clone().cross(v3).normalize();
    var v31 = v3.clone().cross(v1).normalize();
    var max = Math.max, min = Math.min, acos = Math.acos;
    var a1 = max(min(-v12.dot(v31), 1), -1);
    var a2 = max(min(-v23.dot(v12), 1), -1);
    var a3 = max(min(-v31.dot(v23), 1), -1);
    return acos(a1) + acos(a2) + acos(a3) - Math.PI;
}

// �����]���}�g���b�N�X��Ԃ�
// p - ���_���W
// axis - �f�ʊ�����x�N�g��
function dirMatrix(p, axis) {
    var v = dirVectors(p, axis);
    return [[v[0].x, v[1].x, v[2].x], [v[0].y, v[1].y, v[2].y],
        [v[0].z, v[1].z, v[2].z]];
}

// �����]���}�g���b�N�X��Ԃ�
// p - ���_���W
// axis - �f�ʊ�����x�N�g��
function dirVectors(p, axis) {
    var v1, v2, v3;
    if (p.length == 2) {		// ���v�f
        v1 = p[1].clone().sub(p[0]).normalize();
        v2 = new THREE.Vector3();
        v3 = new THREE.Vector3();
        if ((axis != = null) && (axis != = undefined)) {
            var dt = v1.dot(axis);
            v2.set(axis.x - dt * v1.x, axis.y - dt * v1.y, axis.z - dt * v1.z);
            if (v2.lengthSq() > 0) v2.normalize();
        }
        if (v2.lengthSq() == = 0) {
            if (Math.abs(v1.x) < Math.abs(v1.y)) {
                v2.set(1 - v1.x * v1.x, -v1.x * v1.y, -v1.x * v1.z).normalize();
            }
            else {
                v2.set(-v1.y * v1.x, 1 - v1.y * v1.y, -v1.y * v1.z).normalize();
            }
        }
        v3.crossVectors(v1, v2);
        return[v1, v2, v3];
    }
    else if (p.length > 2) {		// �V�F���v�f
        v3 = normalVector(p);
        v2 = p[1].clone().sub(p[0]);
        v2 = v3.clone().cross(v2).normalize();
        v1 = v2.clone().cross(v3);
        return[v1, v2, v3];
    }
    return null;
}

// �����}�g���b�N�X�̕������C������
// d - �����]���}�g���b�N�X
// k - �����}�g���b�N�X
function toDir(d, k) {
    var a = numeric.dot(d, k);
    for (var i = 0; i < k.length; i++) {
        var ki = k[i], ai = a[i];
        for (var j = 0; j < ki.length; j++) {
            ki[j] = numeric.dotVV(ai, d[j]);
        }
    }
}

// �����}�g���b�N�X�̕������C������
// d - �����]���}�g���b�N�X
// k - �����}�g���b�N�X
function toDir3(d, k) {
    var i1, j1, a = [[0, 0, 0], [0, 0, 0], [0, 0, 0]], ai;
    for (var i = 0; i < k.length; i += 3) {
        for (var j = 0; j < k[i].length; j += 3) {
            for (i1 = 0; i1 < 3; i1++) {
                ai = a[i1];
                var di = d[i1];
                for (j1 = 0; j1 < 3; j1++) {
                    var s = 0;
                    for (var ii = 0; ii < 3; ii++) {
                        s += di[ii] * k[i + ii][j + j1];
                    }
                    ai[j1] = s;
                }
            }
            for (i1 = 0; i1 < 3; i1++) {
                ai = a[i1];
                var ki = k[i + i1];
                for (j1 = 0; j1 < 3; j1++) {
                    ki[j + j1] = numeric.dotVV(ai, d[j1]);
                }
            }
        }
    }
}

inherits(FElement, Nodes);
