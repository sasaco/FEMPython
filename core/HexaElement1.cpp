#include "HexaElement1.h";

//--------------------------------------------------------------------//
// �Z�ʑ�1���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
HexaElement1::HexaElement1(int label, int material, vector<int> nodes) : 
    SolidElement(label, material, nodes, HEXA1_NODE, HEXA1_INT) {

    // �Z�ʑ�1���v�f�̎��ʃ}�g���b�N�X�W��
    for (int i = 0; i < 8; i++) {
        for (int j = 0; j < 8; j++) {
            double s = abs(HEXA1_NODE[i][0] - HEXA1_NODE[j][0]) +
                abs(HEXA1_NODE[i][1] - HEXA1_NODE[j][1]) +
                abs(HEXA1_NODE[i][2] - HEXA1_NODE[j][2]);
            HEXA1_MASS_BASE[i][j] = pow(0.5, 0.5 * s) / 27;
        }
    }
};


// �v�f���̂�Ԃ�
string HexaElement1::getName() {
    return "HexaElement1";
} 


// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
void HexaElement1::shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) {

    out = {
        {0.125 * (1 - xsi) * (1 - eta) * (1 - zeta), -0.125 * (1 - eta) * (1 - zeta),
        -0.125 * (1 - xsi) * (1 - zeta), -0.125 * (1 - xsi) * (1 - eta)} ,
        {0.125 * (1 + xsi) * (1 - eta) * (1 - zeta), 0.125 * (1 - eta) * (1 - zeta),
        -0.125 * (1 + xsi) * (1 - zeta), -0.125 * (1 + xsi) * (1 - eta)},
        {0.125 * (1 + xsi) * (1 + eta) * (1 - zeta), 0.125 * (1 + eta) * (1 - zeta),
        0.125 * (1 + xsi) * (1 - zeta), -0.125 * (1 + xsi) * (1 + eta)},
        {0.125 * (1 - xsi) * (1 + eta) * (1 - zeta), -0.125 * (1 + eta) * (1 - zeta),
        0.125 * (1 - xsi) * (1 - zeta), -0.125 * (1 - xsi) * (1 + eta)},
        {0.125 * (1 - xsi) * (1 - eta) * (1 + zeta), -0.125 * (1 - eta) * (1 + zeta),
        -0.125 * (1 - xsi) * (1 + zeta), 0.125 * (1 - xsi) * (1 - eta)},
        {0.125 * (1 + xsi) * (1 - eta) * (1 + zeta), 0.125 * (1 - eta) * (1 + zeta),
        -0.125 * (1 + xsi) * (1 + zeta), 0.125 * (1 + xsi) * (1 - eta)},
        {0.125 * (1 + xsi) * (1 + eta) * (1 + zeta), 0.125 * (1 + eta) * (1 + zeta),
        0.125 * (1 + xsi) * (1 + zeta), 0.125 * (1 + xsi) * (1 + eta)},
        {0.125 * (1 - xsi) * (1 + eta) * (1 + zeta), -0.125 * (1 + eta) * (1 + zeta),
        0.125 * (1 - xsi) * (1 + zeta), 0.125 * (1 - xsi) * (1 + eta)}
    };

}


// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
void HexaElement1::massMatrix(vector<FENode> p, double dens, vector<vector<double>> out) {

    double _ja = 0;

    for (int i = 0; i < 8; i++) {

        vector<vector<double>> sf;
        shapeFunction(HEXA1_INT[i][0], HEXA1_INT[i][1],
            HEXA1_INT[i][2], sf);

        double ja[9];
        jacobianMatrix(p, sf, ja);
        double det = determinant(ja);

        _ja += abs(det);
    }

    double coef = dens * _ja;
    
    for (int i = 0; i < 3 * 8; ++i) {
        vector<double> m;
        for (int j = 0; j < 3 * 8; ++j) {
            m.push_back(0);
        }
        out.push_back(m);
    }

    for (int i = 0; i < 8; i++) {
        int i3 = 3 * i;
        for (int j = 0; j < 8; j++) {
            double value = coef * HEXA1_MASS_BASE[i][j];
            int j3 = 3 * j;
            out[i3][j3] += value;
            out[i3 + 1][j3 + 1] += value;
            out[i3 + 2][j3 + 2] += value;
        }
    }
}
