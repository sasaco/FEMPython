#include "TetraElement2.h";

//--------------------------------------------------------------------//
// �l�ʑ�2���v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
TetraElement2::TetraElement2(int label, int material, vector<int> nodes) :
    SolidElement(label, material, nodes, TETRA2_NODE, TETRA2_INT) {
};


// �v�f���̂�Ԃ�
string TetraElement2::getName() {
    return "TetraElement2";
}


// �`��֐��s�� [ Ni dNi/d�� dNi/d�� dNi/d�� ] ��Ԃ�
// xsi,eta,zeta - �v�f������,��,�č��W
void TetraElement2::shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) {
    
    double xez = 1 - xsi - eta - zeta;

    out= {
        { xez * (2 * xez - 1),      1 - 4 * xez,        1 - 4 * xez,        1 - 4 * xez      },
        { xsi * (2 * xsi - 1),      4 * xsi - 1,        0,                  0                },
        { eta * (2 * eta - 1),      0,                  4 * eta - 1,        0                },
        { zeta * (2 * zeta - 1),    0,                  0,                  4 * zeta - 1     },
        { 4 * xez * xsi,            4 * (xez - xsi),    -4 * xsi,           -4 * xsi         },
        { 4 * xsi * eta,            4 * eta,            4 * xsi,            0                },
        { 4 * xez * eta,            -4 * eta,           4 * (xez - eta),    -4 * eta         },
        { 4 * xez * zeta,           -4 * zeta,          -4 * zeta,          4 * (xez - zeta) },
        { 4 * xsi * zeta,           4 * zeta,           0,                  4 * xsi          },
        { 4 * eta * zeta,           0,                  4 * zeta,           4 * eta          }
    };
};
