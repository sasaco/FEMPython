#include "SolidElement.h";

/// <summary>
/// �l�ʑ�2���v�f
/// �J���r��...
/// </summary>
class TetraElement2 : SolidElement {

private:
    // �l�ʑ�2���v�f�̐ߓ_�̃�,��,�č��W
    const vector<vector<double>> TETRA2_NODE = {
        {0, 0, 0},
        {1, 0, 0},
        {0, 1, 0},
        {0, 0, 1},
        {0.5, 0, 0},
        {0.5, 0.5, 0},
        {0, 0.5, 0},
        {0, 0, 0.5},
        {0.5, 0, 0.5},
        {0, 0.5, 0.5}
    };

    // �l�ʑ�2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
    const vector<vector<double>> TETRA2_INT = {
        {GTETRA2[0], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[1], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[1], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[0], GTETRA2[1], C1_24}
    };

public:
    TetraElement2(int label, int material, vector<int> nodes);

    string getName() override;

    void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) override;

};

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

    return [[xez * (2 * xez - 1), 1 - 4 * xez, 1 - 4 * xez, 1 - 4 * xez],
        [xsi * (2 * xsi - 1), 4 * xsi - 1, 0, 0],
        [eta * (2 * eta - 1), 0, 4 * eta - 1, 0],
        [zeta * (2 * zeta - 1), 0, 0, 4 * zeta - 1],
        [4 * xez * xsi, 4 * (xez - xsi), -4 * xsi, -4 * xsi],
        [4 * xsi * eta, 4 * eta, 4 * xsi, 0],
        [4 * xez * eta, -4 * eta, 4 * (xez - eta), -4 * eta],
        [4 * xez * zeta, -4 * zeta, -4 * zeta, 4 * (xez - zeta)],
        [4 * xsi * zeta, 4 * zeta, 0, 4 * xsi],
        [4 * eta * zeta, 0, 4 * zeta, 4 * eta]];
};
