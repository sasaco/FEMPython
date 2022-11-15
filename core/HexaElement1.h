#pragma once
#include "SolidElement.h";

class HexaElement1 : public SolidElement {

private:

    // �Z�ʑ�1���v�f�̐ߓ_�̃�,��,�č��W
    const vector<vector<double>> HEXA1_NODE = {
        {-1, -1, -1},
        {1, -1, -1},
        {1, 1, -1},
        {-1, 1, -1},
        {-1, -1, 1},
        {1, -1, 1},
        {1, 1, 1},
        {-1, 1, 1}
    };

    // �Z�ʑ�1���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
    const vector<vector<double>> HEXA1_INT = {
        {GX2[0], GX2[0], GX2[0], 1},
        {GX2[1], GX2[0], GX2[0], 1},
        {GX2[0], GX2[1], GX2[0], 1},
        {GX2[1], GX2[1], GX2[0], 1},
        {GX2[0], GX2[0], GX2[1], 1},
        {GX2[1], GX2[0], GX2[1], 1},
        {GX2[0], GX2[1], GX2[1], 1},
        {GX2[1], GX2[1], GX2[1], 1}
    };

    // �Z�ʑ�1���v�f�̎��ʃ}�g���b�N�X�W��
    double HEXA1_MASS_BASE[8][8]; // �R���X�g���N�^�ŏ�����

public:

    HexaElement1(int label, int material, vector<int> nodes);

    string getName() override;

    void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) override;

    void massMatrix(vector<FENode> p, double dens, vector<vector<double>> out) override;
};