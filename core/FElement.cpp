#include "FElement.h";

#include <numeric>


//--------------------------------------------------------------------//
// �v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
FElement::FElement(int _label, int _material, vector<int> _nodes)
    : Nodes(_nodes) {

    label = label;
    material = _material;
    isShell = false;        // �V�F���v�f�ł͂Ȃ�
    isBar = false;		    // ���v�f�ł͂Ȃ�
};

// �ϕ��_�̍����}�g���b�N�X��Ԃ�
// d - ����-�c�}�g���b�N�X
// b - �c-�ψʃ}�g���b�N�X�̓]�u�s��
// coef - �W��
void FElement::stiffPart(vector<vector<double>> d, vector<vector<double>> b, double coef, vector<vector<double>> out) {

    int size1 = b.size();
    int size2 = d.size();
    
    vector<double> a;

    for (int i = 0; i < size1; i++) {

        vector<double> bi = b[i];

        for (int j = 0; j < size2; j++) {
            a.push_back(
                coef * inner_product(bi.begin(), bi.end(), d[j].begin(), 0)
            );
        }

        vector<double> ki;

        for (int j = 0; j < size1; j++) {
            ki.push_back(
                inner_product(a.begin(), a.end(), b[j].begin(), 0)
            );
        }
        out.push_back(ki);
    }
}

// �ߓ_�ψʂ�1�����z��ɕϊ�����
// u - �ߓ_�ψ�
// dof - �ߓ_���R�x
void FElement::toArray(vector<BoundaryCondition> u, int dof, vector<double> out) {

    int count = nodeCount();
    out.clear();

    for (int i = 0; i < count; i++) {
        vector<double> ux = u[i].x;
        for (int j = 0; j < dof; j++) {
            out.push_back(ux[j]);
        }
    }
};

// �ߓ_�ψʂ��Ǐ����W�n�E1�����z��ɕϊ�����
// u - �ߓ_�ψ�
// d - �����]���}�g���b�N�X
void FElement::toLocalArray(vector<BoundaryCondition> u, vector<vector<double>> d, vector<double> v) {

    v.clear();

    for (int i = 0; i < 2; i++) {
        vector<double> ux = u[i].x;
        for (int j = 0; j < 3; j++) {
            v.push_back(d[0][j] * ux[0] + d[1][j] * ux[1] + d[2][j] * ux[2]);
        }
        for (int j = 0; j < 3; j++) {
            v.push_back(d[0][j] * ux[3] + d[1][j] * ux[4] + d[2][j] * ux[5]);
        }
    }
}

// 3x3�̍s�񎮂�Ԃ�
double FElement::determinant(double ja[9]) {

    double det = 0;
    // 3x3�̍s������
    double a[3][3];
    for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 3; ++j) {
            a[i][j] = ja[i + j];
        }
    }
    // �s�񎮂̌v�Z
    for (int i = 0; i < 3; ++i) {
        det = det + a[0][i] * (a[1][(i + 1) % 3] * a[2][(i + 2) % 3] - a[1][(i + 2) % 3] * a[2][(i + 1) % 3]);
    }

    return det;
}


// �����]���}�g���b�N�X��Ԃ�
// p - ���_���W
// axis - �f�ʊ�����x�N�g��
void FElement::dirMatrix(vector<double> p, double axis[3], vector<vector<double>> out) {

    vector<double> v;
    dirVectors(p, axis, v);

    return [[v[0].x, v[1].x, v[2].x], [v[0].y, v[1].y, v[2].y],
        [v[0].z, v[1].z, v[2].z]];
}


// �����]���}�g���b�N�X��Ԃ�
// p - ���_���W
// axis - �f�ʊ�����x�N�g��
void FElement::dirVectors(vector<double> p, double axis[3], vector<double> out[3]) {

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


void FElement::normalVector(vector<double> p, double out[3]) {
    if (p.size() < 3) {
        return null;
    }
    else if ((p.size() == 3) || (p.size() == 6)) {
        return new THREE.Vector3().subVectors(p[1], p[0]).cross
        (new THREE.Vector3().subVectors(p[2], p[0])).normalize();
    }
    else if ((p.size() == 4) || (p.size() == 8)) {
        return new THREE.Vector3().subVectors(p[2], p[0]).cross
        (new THREE.Vector3().subVectors(p[3], p[1])).normalize();
    }
    else {
        var vx = 0, vy = 0, vz = 0;
        for (var i = 0; i < p.size(); i++) {
            var p1 = p[(i + 1) % p.size()], p2 = p[(i + 2) % p.size()];
            var norm = new THREE.Vector3().subVectors(p1, p[i]).cross
            (new THREE.Vector3().subVectors(p2, p[i]));
            vx += norm.x;
            vy += norm.y;
            vz += norm.z;
        }
        return new THREE.Vector3(vx, vy, vz).normalize();
    }
}


