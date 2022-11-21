#include "WedgeElement2.h"

//--------------------------------------------------------------------//
// ���`2���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
WedgeElement2::WedgeElement2(int label, int material, vector<int> nodes) :
    SolidElement(label, material, nodes, WEDGE2_NODE, WEDGE2_INT){
};

// �v�f���̂�Ԃ�
string WedgeElement2::getName() {
    return "'WedgeElement2";
};

// �ߓ_����Ԃ�
int WedgeElement2::nodeCount() {
    return 15;
};


// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
void WedgeElement2::shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) {

    double xe = 1 - xsi - eta;

    out = {
        {
            xe * (xe - 0.5 * zeta - 1) * (1 - zeta),
            -(2 * xe - 0.5 * zeta - 1) * (1 - zeta),
            -(2 * xe - 0.5 * zeta - 1) * (1 - zeta), 
            xe * (zeta - xe + 0.5)
        },
        {
            xsi * (xsi - 0.5 * zeta - 1) * (1 - zeta), 
            (2 * xsi - 0.5 * zeta - 1) * (1 - zeta),
            0, 
            xsi * (zeta - xsi + 0.5)
        },
        {
            eta * (eta - 0.5 * zeta - 1) * (1 - zeta), 
            0,
            (2 * eta - 0.5 * zeta - 1) * (1 - zeta), 
            eta * (zeta - eta + 0.5)
        },
        {
            xe * (xe + 0.5 * zeta - 1) * (1 + zeta), 
            -(2 * xe + 0.5 * zeta - 1) * (1 + zeta),
            -(2 * xe + 0.5 * zeta - 1) * (1 + zeta), 
            xe * (zeta + xe - 0.5)
        },
        {
            xsi * (xsi + 0.5 * zeta - 1) * (1 + zeta), 
            (2 * xsi + 0.5 * zeta - 1) * (1 + zeta),
            0, 
            xsi * (zeta + xsi - 0.5)
        },
        {
            eta * (eta + 0.5 * zeta - 1) * (1 + zeta), 
            0,
            (2 * eta + 0.5 * zeta - 1) * (1 + zeta), 
            eta * (zeta + eta - 0.5)
        },
        {
            2 * xe * xsi * (1 - zeta), 
            2 * (xe - xsi) * (1 - zeta), 
            -2 * xsi * (1 - zeta),
            -2 * xe * xsi
        },
        {
            2 * xsi * eta * (1 - zeta), 
            2 * eta * (1 - zeta), 
            2 * xsi * (1 - zeta),
            -2 * xsi * eta
        },
        {
            2 * eta * xe * (1 - zeta), 
            -2 * eta * (1 - zeta), 
            2 * (xe - eta) * (1 - zeta),
            -2 * eta * xe
        },
        {
            2 * xe * xsi * (1 + zeta), 
            2 * (xe - xsi) * (1 + zeta), 
            -2 * xsi * (1 + zeta),
            2 * xe * xsi
        },
        {
            2 * xsi * eta * (1 + zeta), 
            2 * eta * (1 + zeta), 
            2 * xsi * (1 + zeta),
            2 * xsi * eta
        },
        {
            2 * eta * xe * (1 + zeta), 
            -2 * eta * (1 + zeta), 
            2 * (xe - eta) * (1 + zeta),
            2 * eta * xe
        },
        {
            xe * (1 + zeta) * (1 - zeta), 
            -(1 + zeta) * (1 - zeta), 
            -(1 + zeta) * (1 - zeta),
            -2 * xe * zeta
        },
        {
            xsi * (1 + zeta) * (1 - zeta), 
            (1 + zeta) * (1 - zeta), 
            0, 
            -2 * xsi * zeta
        },
        {
            eta * (1 + zeta) * (1 - zeta), 
            0, 
            (1 + zeta) * (1 - zeta), 
            -2 * eta * zeta
        }
    };
};
