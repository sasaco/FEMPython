#pragma once
#include "SolidElement.h";

/// <summary>
/// 四面体1次要素
/// </summary>
class TetraElement1 : public SolidElement {

private:


public:
    TetraElement1();
    TetraElement1(string label, string material, vector<string> nodes);

    string getName() override;

    MatrixXd shapeFunction(double xsi, double eta, double zeta) override;

    double jacobian(vector<FENode> p);

    MatrixXd grad(vector<FENode> p, double ja);

    MatrixXd massMatrix(vector<FENode> p, double dens) override;

    MatrixXd stiffnessMatrix(vector<FENode> p, MatrixXd d1) override;

    MatrixXd shapeFunctionMatrix(vector<FENode> p, double coef) override;

    MatrixXd gradMatrix(vector<FENode> p, double coef) override;

    MatrixXd geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) override;

    tuple<vector<Strain>, vector<Stress>, vector<double>> strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) override;

    tuple<Strain, Stress, double> elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) override;
};

