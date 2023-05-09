#pragma once
#include "ShellElement.h";

class QuadElement1 : public ShellElement {

private:
    // 四角形1次要素の節点のξ,η座標
    Eigen::Matrix<double, 4, 2> QUAD1_NODE;
    // 四角形1次要素の積分点のξ,η座標,重み係数
    Eigen::Matrix<double, 4, 3> QUAD1_INT;

public:
    QuadElement1();
    QuadElement1(string label, string material, string param, vector<string> nodes);

    string getName() override;

    MatrixXd shapeFunction(double xsi, double eta) override;

    MatrixXd massMatrix(vector<FENode> p, double dens, double t);

    MatrixXd stiffnessMatrix(vector<FENode> p, MatrixXd d1, ShellParameter sp);

    MatrixXd stiffPart(vector<FENode> p, MatrixXd d1, Vector3Dim n,
        double xsi, double eta, double t);

};