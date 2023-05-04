#pragma once
#include "SolidElement.h";

class HexaElement2 : public SolidElement {

private:

    // 六面体2次要素の節点のξ,η,ζ座標
    Eigen::Matrix<double, 20, 3> HEXA2_NODE;
    // 六面体2次要素の積分点のξ,η,ζ座標,重み係数
    Eigen::Matrix<double, 9, 4> HEXA2_INT; // コンストラクタで初期化



public:

    HexaElement2();
    HexaElement2(string label, string material, vector<string> nodes);

    string getName() override;

    MatrixXd shapeFunction(double xsi, double eta, double zeta) override;

};