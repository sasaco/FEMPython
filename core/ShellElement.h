#pragma once
#include "FElement.h"
#include "FENode.h"
#include "Material.h"
#include "ShellParameter.h"
#include "Stress.h"
#include "Strain.h"

#include <format>
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
    ShellElement(int label, int material, int param, vector<int> nodes, MatrixXd _nodeP, MatrixXd _intP);

    virtual string getName() = 0;

    Matrix3d jacobianMatrix(vector<FENode> p, MatrixXd sf, Vector3 n, double t);

    Matrix3d jacobInv(Matrix3d ja, MatrixXd d);

    MatrixXd grad(vector<FENode> p, Matrix3d ja, MatrixXd sf, Matrix3d d, double t);

    MatrixXd strainMatrix1(Matrix3d ja, MatrixXd sf, Matrix3d d);

    MatrixXd strainMatrix(Matrix3d ja, MatrixXd sf, Matrix3d d, double zeta, double t);

    virtual MatrixXd shapeFunction(double xsi, double eta) = 0;

    MatrixXd shapePart(vector<FENode> p, Vector3d x, double w, double t);

    MatrixXd gradPart(vector<FENode> p, Vector3d x, double w, double t);

    MatrixXd shapeFunctionMatrix(vector<FENode> p, double coef, ShellParameter sp);

    MatrixXd gradMatrix(vector<FENode> p, double coef, ShellParameter sp);

    MatrixXd geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);

    tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
        strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);

    VectorXd strainPart(vector<FENode> p, VectorXd v, Vector3 n, Matrix3d d, double  xsi, double eta, double zeta, double t);

    tuple<Strain, Stress, double, Strain, Stress, double>
        elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);

    Strain toStrain(VectorXd s);

    Stress toStress(VectorXd s);

    string toString(vector<Material> materials, vector<ShellParameter> params, vector<FENode> p);

};


