#pragma once
#include "SolidElement.h";

class HexaElement1 : public SolidElement {

private:

    // 六面体1次要素の節点のξ,η,ζ座標
    MatrixXd HEXA1_NODE; 

    // 六面体1次要素の積分点のξ,η,ζ座標,重み係数
    MatrixXd HEXA1_INT; 

    // 六面体1次要素の質量マトリックス係数
    MatrixXd HEXA1_MASS_BASE;


public:

    HexaElement1(int label, int material, vector<int> nodes);

    string getName() override;

    MatrixXd shapeFunction(double xsi, double eta, double zeta) override;

    MatrixXd massMatrix(vector<FENode> p, double dens) override;
};