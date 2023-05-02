#pragma once

#include "SolidElement.h"

#include<Eigen/Core>
using namespace Eigen;

class HexaElement1 : public SolidElement {

private:

    // 六面体1次要素の節点のξ,η,ζ座標
    Eigen::Matrix<double, 8,3> HEXA1_NODE;

    // 六面体1次要素の積分点のξ,η,ζ座標,重み係数
    Eigen::Matrix<double, 8, 4>  HEXA1_INT;

    // 六面体1次要素の質量マトリックス係数
    Eigen::Matrix<double, 8, 8>  HEXA1_MASS_BASE;


public:
    HexaElement1();
    HexaElement1(string label, string material, vector<string> nodes);

    string getName() override;

    MatrixXd shapeFunction(double xsi, double eta, double zeta) override;

    MatrixXd massMatrix(vector<FENode> p, double dens) override;

    VectorXd angle(vector<FENode> p);
};