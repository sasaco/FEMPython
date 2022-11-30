#pragma once
#include "FElement.h";
#include "FENode.h";
#include "Material.h";
#include "numeric.h";
#include "ShellParameter.h";
#include "Stress.h";
#include "Strain.h";

#include <string>
#include <vector>
using std::vector;

#include<Eigen/Core>


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

    Vector3d jacobInv(Matrix3d ja, MatrixXd d);

    MatrixXd grad(vector<FENode> p, Vector3d ja, MatrixXd sf, Matrix3d d, double t);

    MatrixXd strainMatrix1(Vector3d ja, MatrixXd sf, Matrix3d d);

    MatrixXd strainMatrix(Vector3d ja, MatrixXd sf, Matrix3d d, double zeta, double t);

    virtual MatrixXd shapeFunction(double xsi, double eta) = 0;

    MatrixXd shapePart(vector<FENode> p, Vector3d x, double w, double t);

    MatrixXd gradPart(vector<FENode> p, Vector3d x, double w, double t);

    void shapeFunctionMatrix(vector<FENode> p, double coef, ShellParameter sp, vector<vector<double>> out);

    void geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, ShellParameter sp, vector<vector<double>> out);

    void strainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, ShellParameter sp,
        vector<Strain> strain1, vector<Stress> stress1, vector<double> energy1,
        vector<Strain> strain2, vector<Stress> stress2, vector<double> energy2);

    void strainPart(vector<FENode> p, vector<double> v, double n[3], vector<vector<double>> d, double  xsi, double eta, double zeta, double t, vector<double> out);

    void elementStrainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, ShellParameter sp,
        vector<Strain> _Strain1, vector<Stress> _Stress1, double energy1,
        vector<Strain> _Strain2, vector<Stress> _Stress2, double energy2);

    void toStrain(vector<double> s, Strain out);

    void toStress(vector<double> s, Stress out);

    string toString(vector<Material> materials, vector<ShellParameter> params, vector<FENode> p);

    vector<double> toLocal(d, vector<FENode> p);
};


