#pragma once

#include "BoundaryCondition.h"
#include "FElement.h"
#include "FENode.h"
#include "Material.h"
#include "Strain.h"
#include "Stress.h"

//#include <fmt/core.h>
#include <string>
#include <vector>
using namespace std;
using std::string;
using std::vector;

#include<Eigen/Core>
#include<Eigen/LU>
 using namespace Eigen;

class SolidElement : public FElement {

private:
    double PI = M_PI;

protected:
    MatrixXd nodeP;
    MatrixXd intP;

public:

    SolidElement();
    SolidElement(int label, int material, vector<int> nodes);

    virtual MatrixXd jacobianMatrix(vector<FENode> p, MatrixXd sf);

    virtual MatrixXd grad(vector<FENode> p, MatrixXd ja, MatrixXd sf);

    MatrixXd strainMatrix(MatrixXd grad);

    virtual MatrixXd shapeFunction(double xsi, double eta, double zeta) = 0;

    MatrixXd shapePart(vector<FENode> p, VectorXd x, double w);

    MatrixXd gradPart(vector<FENode> p, VectorXd x, double w);

    virtual MatrixXd massMatrix(vector<FENode> p, double dens);
    
    virtual MatrixXd stiffnessMatrix(vector<FENode> p, MatrixXd d1);

    virtual MatrixXd shapeFunctionMatrix(vector<FENode> p, double coef);

    virtual MatrixXd gradMatrix(vector<FENode> p, double coef);

    virtual MatrixXd geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, MatrixXd d1);

    virtual tuple<vector<Strain>, vector<Stress>, vector<double>> strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1);

    VectorXd strainPart(vector<FENode> p, VectorXd v, VectorXd x);

    virtual tuple<Strain, Stress, double> elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1);

    virtual string getName() = 0;
    
    virtual  VectorXd angle(vector<FENode> p) = 0;

    double solidAngle(FENode p0, FENode p1, FENode p2, FENode p3);

    string toString(vector<Material> materials, vector<FENode> p);
};

