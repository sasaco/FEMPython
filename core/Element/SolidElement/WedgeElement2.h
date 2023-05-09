#pragma once
#include "SolidElement.h"

class WedgeElement2 : public SolidElement {

private:

    // 楔形2次要素の節点のξ,η,ζ座標
    Eigen::Matrix<double, 15, 3> WEDGE2_NODE;

    // 楔形2次要素の積分点のξ,η,ζ座標,重み係数
    Eigen::Matrix<double, 9, 4> WEDGE2_INT;

public:
    WedgeElement2();
    WedgeElement2(string label, string material, vector<string> nodes);

    string getName() override;

    int nodeCount() override;

    MatrixXd shapeFunction(double xsi, double eta, double zeta) override;

};
