#pragma once
#include "SolidElement.h";

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