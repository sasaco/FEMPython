#pragma once
#include "FElement.h"
#include "FENode.h"
#include "Material.h"
#include "ShellParameter.h"
#include "Stress.h"
#include "Strain.h"

//#include <format>
#include <string>
#include <vector>
using std::vector;

#include<Eigen/Core>
#include<Eigen/LU>
using namespace Eigen;

class ShellElement : public FElement {

private:
    int label;
    int param;
    bool isShell;

    MatrixXd nodeP;
    MatrixXd intP;

public:
    ShellElement();
    ShellElement(int label, int material, int param, vector<int> nodes, MatrixXd _nodeP, MatrixXd _intP);

    virtual string getName() = 0;

    MatrixXd jacobianMatrix(vector<FENode> p, MatrixXd sf, Vector3 n, double t);

    MatrixXd jacobInv(MatrixXd ja, MatrixXd d);

    MatrixXd grad(vector<FENode> p, MatrixXd ja, MatrixXd sf, MatrixXd d, double t);

    MatrixXd strainMatrix1(MatrixXd ja, MatrixXd sf, MatrixXd d);

    MatrixXd strainMatrix(MatrixXd ja, MatrixXd sf, MatrixXd d, double zeta, double t);

    virtual MatrixXd shapeFunction(double xsi, double eta) = 0;

    MatrixXd shapePart(vector<FENode> p, VectorXd x, double w, double t);

    MatrixXd gradPart(vector<FENode> p, VectorXd x, double w, double t);

    MatrixXd shapeFunctionMatrix(vector<FENode> p, double coef, ShellParameter sp);

    MatrixXd gradMatrix(vector<FENode> p, double coef, ShellParameter sp);

    MatrixXd geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);

    tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
        strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);

    VectorXd strainPart(vector<FENode> p, VectorXd v, Vector3 n, MatrixXd d, double  xsi, double eta, double zeta, double t);

    tuple<Strain, Stress, double, Strain, Stress, double>
        elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);

    Strain toStrain(VectorXd s);

    Stress toStress(VectorXd s);

    string toString(vector<Material> materials, vector<ShellParameter> params, vector<FENode> p);

};


