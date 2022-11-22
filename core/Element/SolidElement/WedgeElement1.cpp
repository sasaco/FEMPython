#include "WedgeElement1.h";

//--------------------------------------------------------------------//
// ���`1���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
WedgeElement1::WedgeElement1(int label, int material, vector<int> nodes) :
    SolidElement(label, material, nodes, WEDGE1_NODE, WEDGE1_INT) {


};

// �v�f���̂�Ԃ�
string WedgeElement1::getName() {
    return "WedgeElement1";
};


// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
void WedgeElement1::shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) {

    out = {
        {
            0.5 * (1 - xsi - eta) * (1 - zeta), 
            -0.5 * (1 - zeta), 
            -0.5 * (1 - zeta),
            -0.5 * (1 - xsi - eta)
        },
        {
            0.5 * xsi * (1 - zeta), 
            0.5 * (1 - zeta), 
            0, 
            -0.5 * xsi
        },
        {
            0.5 * eta * (1 - zeta), 
            0, 
            0.5 * (1 - zeta), 
            -0.5 * eta
        },
        {
            0.5 * (1 - xsi - eta) * (1 + zeta), 
            -0.5 * (1 + zeta), 
            -0.5 * (1 + zeta),
            0.5 * (1 - xsi - eta)
        },
        {
            0.5 * xsi * (1 + zeta), 
            0.5 * (1 + zeta), 
            0, 
            0.5 * xsi
        },
        {
            0.5 * eta * (1 + zeta), 
            0, 
            0.5 * (1 + zeta), 
            0.5 * eta
        }
    };
};


// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
void WedgeElement1::massMatrix(vector<FENode> p, double dens, vector<vector<double>> out) {
    
    double ja = 0;

    for (int i = 0; i < 2; i++) {
        vector<vector<double>> sf;
        shapeFunction(WEDGE1_INT[i][0], WEDGE1_INT[i][1],
            WEDGE1_INT[i][2], sf);

        double jm[9];
        jacobianMatrix(p, sf, jm);
        double det = determinant(jm);

        ja += det;
    }

    double v0 = dens * ja / 36;
    for (int i = 0; i < 18; ++i) {
        vector<double> m;
        for (int j = 0; j < 18; ++j) {
            m.push_back(0);
        }
        out.push_back(m);
    }

    for (int i = 0; i < 6; i++) {
        for (int j = 0; j < 6; j++) {
            double value = v0;
            int i3 = 3 * i;
            int j3 = 3 * j;

            if (i % 3 != j % 3) value *= 0.5;

            if (floor(i / 3) != floor(j / 3)) value *= 0.5;

            out[i3][j3] += value;
            out[i3 + 1][j3 + 1] += value;
            out[i3 + 2][j3 + 2] += value;
        }
    }
};