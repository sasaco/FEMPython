#pragma once
#include "SolidElement.h";

class WedgeElement1 : SolidElement {

private:
    // ���`1���v�f�̐ߓ_�̃�,��,�č��W
    const vector<vector<double>> WEDGE1_NODE = {
        {0, 0, -1},
        {1, 0, -1},
        {0, 1, -1},
        {0, 0, 1},
        {1, 0, 1},
        {0, 1, 1}
    };

    // ���`1���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
    const vector<vector<double>> WEDGE1_INT = {
        {C1_3, C1_3, GX2[0], 0.5},
        {C1_3, C1_3, GX2[1], 0.5}
    };

public:
    WedgeElement1(int label, int material, vector<int> nodes);

    string getName() override;

    void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) override;

    void massMatrix(vector<FENode> p, double dens, vector<vector<double>> out) override;

};