#pragma once
#include "ShellElement.h"


class TriElement1 : public ShellElement {

private:
    // 三角形1次要素の節点のξ,η座標
    Eigen::Matrix<double, 3, 2> TRI1_NODE;

    // 三角形1次要素の積分点のξ,η座標,重み係数
    Eigen::Matrix<double, 1, 3> TRI1_INT;

    // 三角形2次要素の積分点のξ,η座標,重み係数
    Eigen::Matrix<double, 2, 3> TRI2_INT;

    // 三角形1次要素の質量マトリックス係数
    Eigen::Matrix<double, 3, 3> TRI1_MASS1;


public:
    TriElement1();
    TriElement1(string label, string material, string param, vector<string> nodes);

    string getName() override;

    int nodeCount() override;

    MatrixXd shapeFunction(double xsi, double eta) override;

    MatrixXd shapeFunction2(double xsi, double eta);

    MatrixXd shapeFunction3(vector<FENode> p, MatrixXd d, double xsi, double eta);

    double jacobian(vector<FENode> p);

    MatrixXd massMatrix(vector<FENode> p, double dens, double t);

    MatrixXd stiffnessMatrix(vector<FENode> p, MatrixXd d1, ShellParameter sp);

    MatrixXd strainMatrix1(MatrixXd sf, VectorXd jinv);

    MatrixXd strainMatrix2(MatrixXd sf, VectorXd jinv);

    MatrixXd shapeFunctionMatrix(vector<FENode> p, double coef, double t);

    MatrixXd geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, VectorXd d1, ShellParameter sp);

    VectorXd strainPart(vector<FENode> p, VectorXd v, Vector3Dim n, MatrixXd d, double xsi, double eta, double zeta, double t);

    MatrixXd strainMatrix(MatrixXd sf1, MatrixXd sf3, MatrixXd jinv, MatrixXd d, double zeta, double t);

    Strain toStrain(VectorXd s);

    Stress toStress(VectorXd s);

};
