#pragma once
#include "SolidElement.h";

class HexaElement1 : public SolidElement {

private:

    // 六面体1次要素の節点のξ,η,ζ座標
    vector<vector<double>> HEXA1_NODE = {
        {-1, -1, -1},
        {1, -1, -1},
        {1, 1, -1},
        {-1, 1, -1},
        {-1, -1, 1},
        {1, -1, 1},
        {1, 1, 1},
        {-1, 1, 1}
    };

    // 六面体1次要素の積分点のξ,η,ζ座標,重み係数
    vector<vector<double>> HEXA1_INT = {
        { FElement::GX2[0], FElement::GX2[0], FElement::GX2[0], 1 },
        { FElement::GX2[1], FElement::GX2[0], FElement::GX2[0], 1 },
        { FElement::GX2[0], FElement::GX2[1], FElement::GX2[0], 1 },
        { FElement::GX2[1], FElement::GX2[1], FElement::GX2[0], 1 },
        { FElement::GX2[0], FElement::GX2[0], FElement::GX2[1], 1 },
        { FElement::GX2[1], FElement::GX2[0], FElement::GX2[1], 1 },
        { FElement::GX2[0], FElement::GX2[1], FElement::GX2[1], 1 },
        { FElement::GX2[1], FElement::GX2[1], FElement::GX2[1], 1 }
    };

    // 六面体1次要素の質量マトリックス係数
    double HEXA1_MASS_BASE[8][8]; // コンストラクタで初期化

public:

    HexaElement1(int label, int material, vector<int> nodes);

    string getName() override;

    void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) override;

    void massMatrix(vector<FENode> p, double dens, vector<vector<double>> out) override;
};