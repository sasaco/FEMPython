#pragma once
#include "SolidElement.h"

class WedgeElement1 : SolidElement {

private:
    // 楔形1次要素の節点のξ,η,ζ座標
    Eigen::Matrix<double, 6, 3> WEDGE1_NODE;

    // 楔形1次要素の積分点のξ,η,ζ座標,重み係数
    Eigen::Matrix<double, 2, 4> WEDGE1_INT;

public:
    WedgeElement1();
    WedgeElement1(string label, string material, vector<string> nodes);

    string getName() override;

    MatrixXd shapeFunction(double xsi, double eta, double zeta) override;
 
    MatrixXd massMatrix(vector<FENode> p, double dens) override;

};