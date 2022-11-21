#pragma once
#include "SolidElement.h";

class HexaElement2 : public SolidElement {

private:

    // �Z�ʑ�2���v�f�̐ߓ_�̃�,��,�č��W
    const vector<vector<double>> HEXA2_NODE = {
        {-1, -1, -1},
        {1, -1, -1},
        {1, 1, -1},
        {-1, 1, -1},
        {-1, -1, 1},
        {1, -1, 1},
        {1, 1, 1},
        {-1, 1, 1},
        {0, -1, -1},
        {1, 0, -1},
        {0, 1, -1},
        {-1, 0, -1},
        {0, -1, 1},
        {1, 0, 1},
        {0, 1, 1},
        {-1, 0, 1},
        {-1, -1, 0},
        {1, -1, 0},
        {1, 1, 0},
        {-1, 1, 0}
    };

    // �Z�ʑ�2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
    vector<vector<double>> HEXA2_INT; // �R���X�g���N�^�ŏ�����


public:

    HexaElement2(int label, int material, vector<int> nodes);

    string getName() override;

    void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) override;

};