#include "BEBarElement.h";
//--------------------------------------------------------------------//
// Bernoulli-Euler���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// param - ���p�����[�^�̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// axis - �f�ʊ�����x�N�g��
BEBarElement::BEBarElement(int label, int material, int param, , vector<int> nodes, Vector3 axis) :
    BarElement(label, material, param, nodes, axis){
};


// �v�f���E���̂�Ԃ�
string BEBarElement::getName() {
    return "BEBarElement";
};


// �����}�g���b�N�X�̗��Ȃ�������Ԃ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
vector<vector<double>> BEBarElement::stiffBend(double l, Material material, Section sect) {

    double kc = material.ee / (l * l * l);
    double kcy = kc * sect.iy;
    double kcz = kc * sect.iz;

    double kcy12 = 12 * kcy;
    double kcy6l = 6 * kcy * l;
    double kcyll = kcy * l * l;
    double kcz12 = 12 * kcz;
    double kcz6l = 6 * kcz * l;
    double kczll = kcz * l * l;

    vector<vector<double>> result = {
        {kcy12, kcy6l, -kcy12, kcy6l}, 
        {kcy6l, 4 * kcyll, -kcy6l, 2 * kcyll},
        {-kcy12, -kcy6l, kcy12, -kcy6l}, 
        {kcy6l, 2 * kcyll, -kcy6l, 4 * kcyll},
        {kcz12, -kcz6l, -kcz12, -kcz6l}, 
        {-kcz6l, 4 * kczll, kcz6l, 2 * kczll},
        {-kcz12, kcz6l, kcz12, kcz6l}, 
        {-kcz6l, 2 * kczll, kcz6l, 4 * kczll}
    };

    return result;
};


// �Ȃ��ȗ��E����f�c��Ԃ�
// v - �Ǐ����W�n�̕ψ�
// l - �v�f����
// material - �ޗ�
// sect - ���f�ʃp�����[�^
vector<vector<double>> BEBarElement::bendCurveShare(vector<double> v, double l, Material material, Section sect) {

    double ckap1 = 6 / (l * l);
    double ckap2 = 4 / l;
    double ckap3 = 0.5 * ckap2;

    vector<double> kpy = {
        ckap1 * (v[1] - v[7]) + ckap2 * v[5] + ckap3 * v[11],
        ckap1 * (v[1] - v[7]) + ckap3 * v[5] + ckap2 * v[11]
    };

    vector<double> kpz = {
        -ckap1 * (v[2] - v[8]) + ckap2 * v[4] + ckap3 * v[10],
        -ckap1 * (v[2] - v[8]) + ckap3 * v[4] + ckap2 * v[10]
    };

    vector<vector<double>> result =
    {
        kpy, kpz, 0, 0
    };

    return result;
};

// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
// sect - ���f�ʃp�����[�^
vector<vector<double>> BEBarElement::massMatrix(vector<FENode> p, double dens, Section sect) {

    double l = p[0].distanceTo(p[1]);
    vector<vector<double>> d = dirMatrix(p, axis);

    double* mi = sect.massInertia(dens, l);
    double m0 = mi[0];
    double dm = C1_3 * m0;
    double dix = C1_3 * mi[1];

    vector<vector<double>> m = numeric::rep(12, 12);

    m[0][0] = dm;
    m[0][6] = 0.5 * dm;
    m[6][0] = 0.5 * dm;
    m[6][6] = dm;
    m[3][3] = dix;
    m[3][9] = 0.5 * dix;
    m[9][3] = 0.5 * dix;
    m[9][9] = dix;

    double m1 = m0 * 13 / 35;
    double m2 = m0 * 11 * l / 210;
    double m3 = m0 * 9 / 70;
    double m4 = m0 * 13 * l / 420;

    double m5 = m0 * l * l / 105;
    double m6 = m0 * l * l / 140;

    vector<vector<double>> mm1 = {
        {m1, m2, m3, -m4},
        {m2, m5, m4, -m6},
        {m3, m4, m1, -m2},
        {-m4, -m6, -m2, m5}
    };
    vector<vector<double>> mm2 = {
        {m1, -m2, m3, m4}, 
        {-m2, m5, -m4, -m6},
        {m3, -m4, m1, m2}, 
        {m4, -m6, m2, m5}
    };

    for (int i = 0; i < 4; i++) {
        vector<double> mi1 = m[I_YMZ[i]];
        vector<double> mi2 = m[I_ZMY[i]];
        vector<double> mmi1 = mm1[i];
        vector<double> mmi2 = mm2[i];

        for (int j = 0; j < 4; j++) {
            mi1[I_YMZ[j]] = mmi1[j];
            mi2[I_ZMY[j]] = mmi2[j];
        }
    }
    toDir3(d, m);
    return m;
};