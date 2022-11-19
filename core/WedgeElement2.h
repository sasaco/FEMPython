#pragma once
#include "SolidElement.h";

class WedgeElement2 : public SolidElement {

private:

    // ���`2���v�f�̐ߓ_�̃�,��,�č��W
    const vector<vector<double>> WEDGE2_NODE = {
        {0, 0, -1},
        {1, 0, -1},
        {0, 1, -1},
        {0, 0, 1},
        {1, 0, 1},
        {0, 1, 1},
        {0.5, 0, -1},
        {0.5, 0.5, -1},
        {0, 0.5, -1},
        {0.5, 0, 1},
        {0.5, 0.5, 1},
        {0, 0.5, 1},
        {0, 0, 0},
        {1, 0, 0},
        {0, 1, 0}
    };

    // ���`2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
    const vector<vector<double>> WEDGE2_INT = {
        {GTRI2[0], GTRI2[0], GX3[0], C1_6 * GW3[0]},
        {GTRI2[1], GTRI2[0], GX3[0], C1_6 * GW3[0]},
        {GTRI2[0], GTRI2[1], GX3[0], C1_6 * GW3[0]},
        {GTRI2[0], GTRI2[0], GX3[1], C1_6 * GW3[1]},
        {GTRI2[1], GTRI2[0], GX3[1], C1_6 * GW3[1]},
        {GTRI2[0], GTRI2[1], GX3[1], C1_6 * GW3[1]},
        {GTRI2[0], GTRI2[0], GX3[2], C1_6 * GW3[2]},
        {GTRI2[1], GTRI2[0], GX3[2], C1_6 * GW3[2]},
        {GTRI2[0], GTRI2[1], GX3[2], C1_6 * GW3[2]}
    };

public:
    WedgeElement2(int label, int material, vector<int> nodes);

    string getName() override;

    int nodeCount() override;

    void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) override;

};
