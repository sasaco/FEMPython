#pragma once
#include "SolidElement.h";

class TetraElement2 : SolidElement {

private:
    Eigen::Matrix<double, 10, 3> TETRA2_NODE;
    Eigen::Matrix<double, 4, 4> TETRA2_INT;

public:
    TetraElement2();
    TetraElement2(string label, string material, vector<string> nodes);

    string getName() override;

    MatrixXd shapeFunction(double xsi, double eta, double zeta) override;

};