#pragma once
#include "SolidElement.h";

class HexaElement2 : public SolidElement {

private:

    // 六面体2次要素の節点のξ,η,ζ座標
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

    // 六面体2次要素の積分点のξ,η,ζ座標,重み係数
    vector<vector<double>> HEXA2_INT; // コンストラクタで初期化


public:

    HexaElement2(int label, int material, vector<int> nodes);

    string getName() override;

    void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) override;

};