#include "HexaElement2.h";


//--------------------------------------------------------------------//
// �Z�ʑ̃Z�����f�B�s�e�B��2���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
HexaElement2::HexaElement2(int label, int material, vector<int> nodes) :
    SolidElement(label, material, nodes, HEXA2_NODE, HEXA2_INT) {

    // �Z�ʑ�2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
    for (int k = 0; k < 3; k++) {
        for (int j = 0; j < 3; j++) {
            vector<double> a;
            for (int i = 0; i < 3; i++) {
                a.push_back(GX3[i]);
                a.push_back(GX3[j]);
                a.push_back(GX3[k]);
                a.push_back(GW3[i] * GW3[j] * GW3[k]);
                HEXA2_INT.push_back(a);
            }
        }
    }
}

// �v�f���̂�Ԃ�
string HexaElement2::getName() {
    return "HexaElement2";
}


// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
void HexaElement2::shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) {

    out= {
        {
            0.125 * (1 - xsi) * (1 - eta) * (1 - zeta) * (-xsi - eta - zeta - 2),
            0.125 * (1 - eta) * (1 - zeta) * (2 * xsi + eta + zeta + 1),
            0.125 * (1 - xsi) * (1 - zeta) * (xsi + 2 * eta + zeta + 1),
            0.125 * (1 - xsi) * (1 - eta) * (xsi + eta + 2 * zeta + 1)
        }, 
        {
            0.125 * (1 + xsi) * (1 - eta) * (1 - zeta) * (xsi - eta - zeta - 2),
            0.125 * (1 - eta) * (1 - zeta) * (2 * xsi - eta - zeta - 1),
            0.125 * (1 + xsi) * (1 - zeta) * (-xsi + 2 * eta + zeta + 1),
            0.125 * (1 + xsi) * (1 - eta) * (-xsi + eta + 2 * zeta + 1)
        },
        {
            0.125 * (1 + xsi) * (1 + eta) * (1 - zeta) * (xsi + eta - zeta - 2),
            0.125 * (1 + eta) * (1 - zeta) * (2 * xsi + eta - zeta - 1),
            0.125 * (1 + xsi) * (1 - zeta) * (xsi + 2 * eta - zeta - 1),
            0.125 * (1 + xsi) * (1 + eta) * (-xsi - eta + 2 * zeta + 1)
        },
        {
            0.125 * (1 - xsi) * (1 + eta) * (1 - zeta) * (-xsi + eta - zeta - 2),
            0.125 * (1 + eta) * (1 - zeta) * (2 * xsi - eta + zeta + 1),
            0.125 * (1 - xsi) * (1 - zeta) * (-xsi + 2 * eta - zeta - 1),
            0.125 * (1 - xsi) * (1 + eta) * (xsi - eta + 2 * zeta + 1)
        },
        {
            0.125 * (1 - xsi) * (1 - eta) * (1 + zeta) * (-xsi - eta + zeta - 2),
            0.125 * (1 - eta) * (1 + zeta) * (2 * xsi + eta - zeta + 1),
            0.125 * (1 - xsi) * (1 + zeta) * (xsi + 2 * eta - zeta + 1),
            0.125 * (1 - xsi) * (1 - eta) * (-xsi - eta + 2 * zeta - 1)
        },
        {
            0.125 * (1 + xsi) * (1 - eta) * (1 + zeta) * (xsi - eta + zeta - 2),
            0.125 * (1 - eta) * (1 + zeta) * (2 * xsi - eta + zeta - 1),
            0.125 * (1 + xsi) * (1 + zeta) * (-xsi + 2 * eta - zeta + 1),
            0.125 * (1 + xsi) * (1 - eta) * (xsi - eta + 2 * zeta - 1)
        },
        {
            0.125 * (1 + xsi) * (1 + eta) * (1 + zeta) * (xsi + eta + zeta - 2),
            0.125 * (1 + eta) * (1 + zeta) * (2 * xsi + eta + zeta - 1),
            0.125 * (1 + xsi) * (1 + zeta) * (xsi + 2 * eta + zeta - 1),
            0.125 * (1 + xsi) * (1 + eta) * (xsi + eta + 2 * zeta - 1)
        },
        {
            0.125 * (1 - xsi) * (1 + eta) * (1 + zeta) * (-xsi + eta + zeta - 2),
            0.125 * (1 + eta) * (1 + zeta) * (2 * xsi - eta - zeta + 1),
            0.125 * (1 - xsi) * (1 + zeta) * (-xsi + 2 * eta + zeta - 1),
            0.125 * (1 - xsi) * (1 + eta) * (-xsi + eta + 2 * zeta - 1)
        },
        {
            0.25 * (1 + xsi) * (1 - xsi) * (1 - eta) * (1 - zeta),
            -0.5 * xsi * (1 - eta) * (1 - zeta),
            -0.25 * (1 + xsi) * (1 - xsi) * (1 - zeta),
            -0.25 * (1 + xsi) * (1 - xsi) * (1 - eta)
        },
        {
            0.25 * (1 + xsi) * (1 + eta) * (1 - eta) * (1 - zeta),
            0.25 * (1 + eta) * (1 - eta) * (1 - zeta),
            -0.5 * (1 + xsi) * eta * (1 - zeta),
            -0.25 * (1 + xsi) * (1 + eta) * (1 - eta)
        },
        {
            0.25 * (1 + xsi) * (1 - xsi) * (1 + eta) * (1 - zeta),
            -0.5 * xsi * (1 + eta) * (1 - zeta),
            0.25 * (1 + xsi) * (1 - xsi) * (1 - zeta),
            -0.25 * (1 + xsi) * (1 - xsi) * (1 + eta)
        },
        {
            0.25 * (1 - xsi) * (1 + eta) * (1 - eta) * (1 - zeta),
            -0.25 * (1 + eta) * (1 - eta) * (1 - zeta),
            -0.5 * (1 - xsi) * eta * (1 - zeta),
            -0.25 * (1 - xsi) * (1 + eta) * (1 - eta)
        },
        {
            0.25 * (1 + xsi) * (1 - xsi) * (1 - eta) * (1 + zeta),
            -0.5 * xsi * (1 - eta) * (1 + zeta),
            -0.25 * (1 + xsi) * (1 - xsi) * (1 + zeta),
            0.25 * (1 + xsi) * (1 - xsi) * (1 - eta)
        },
        {
            0.25 * (1 + xsi) * (1 + eta) * (1 - eta) * (1 + zeta),
            0.25 * (1 + eta) * (1 - eta) * (1 + zeta),
            -0.5 * (1 + xsi) * eta * (1 + zeta),
            0.25 * (1 + xsi) * (1 + eta) * (1 - eta)
        },
        {
            0.25 * (1 + xsi) * (1 - xsi) * (1 + eta) * (1 + zeta),
            -0.5 * xsi * (1 + eta) * (1 + zeta),
            0.25 * (1 + xsi) * (1 - xsi) * (1 + zeta),
            0.25 * (1 + xsi) * (1 - xsi) * (1 + eta)
        },
        {
            0.25 * (1 - xsi) * (1 + eta) * (1 - eta) * (1 + zeta),
            -0.25 * (1 + eta) * (1 - eta) * (1 + zeta),
            -0.5 * (1 - xsi) * eta * (1 + zeta),
            0.25 * (1 - xsi) * (1 + eta) * (1 - eta)
        },
        {
            0.25 * (1 - xsi) * (1 - eta) * (1 + zeta) * (1 - zeta),
            -0.25 * (1 - eta) * (1 + zeta) * (1 - zeta),
            -0.25 * (1 - xsi) * (1 + zeta) * (1 - zeta),
            -0.5 * (1 - xsi) * (1 - eta) * zeta
        },
        {
            0.25 * (1 + xsi) * (1 - eta) * (1 + zeta) * (1 - zeta),
            0.25 * (1 - eta) * (1 + zeta) * (1 - zeta),
            -0.25 * (1 + xsi) * (1 + zeta) * (1 - zeta),
            -0.5 * (1 + xsi) * (1 - eta) * zeta
        },
        {
            0.25 * (1 + xsi) * (1 + eta) * (1 + zeta) * (1 - zeta),
            0.25 * (1 + eta) * (1 + zeta) * (1 - zeta),
            0.25 * (1 + xsi) * (1 + zeta) * (1 - zeta),
            -0.5 * (1 + xsi) * (1 + eta) * zeta
        },
        {
            0.25 * (1 - xsi) * (1 + eta) * (1 + zeta) * (1 - zeta),
            -0.25 * (1 + eta) * (1 + zeta) * (1 - zeta),
            0.25 * (1 - xsi) * (1 + zeta) * (1 - zeta),
            -0.5 * (1 - xsi) * (1 + eta) * zeta
        }
    };
}