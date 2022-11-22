#pragma once
#include "FElement.h";
#include "FENode.h";
#include "Material.h";
#include "numeric.h";


#include <string>
#include <vector>
using namespace std;



class SolidElement : public FElement {

private:

    vector<vector<double>> nodeP;
    vector<vector<double>> intP;

public:

    SolidElement(int label, int material, vector<int> nodes);
    SolidElement(int label, int material, vector<int> nodes, vector<vector<double>> _nodeP, vector<vector<double>> _intP);

    virtual void jacobianMatrix(vector<FENode> p, vector<vector<double>> sf, double out[9]);

    void grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, vector<vector<double>> out);

    void strainMatrix(vector<vector<double>> grad, vector<vector<double>> out);

    virtual void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) = 0;

    void shapePart(vector<FENode> p, double x[3], double w, vector<vector<double>> out);

    void gradPart(vector<FENode> p, double x[3], double w, vector<vector<double>> out);

    virtual void massMatrix(vector<FENode> p, double dens, vector<vector<double>> out);

    void addMatrix(vector<vector<double>> a, vector<vector<double>> da);

    virtual void stiffnessMatrix(vector<FENode> p, vector<vector<double>> d1, vector<vector<double>> out);

    virtual void shapeFunctionMatrix(vector<FENode> p, double coef, vector<vector<double>> out);

    virtual void gradMatrix(vector<FENode> p, double coef, vector<vector<double>> out);

    virtual void geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, vector<vector<double>> out);

    virtual void strainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
        vector<Strain> strain, vector<Stress> stress, vector<double> energy);

    void strainPart(vector<FENode> p, vector<double> v, double x[3], vector<double> out);

    virtual void elementStrainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
        Strain _Strain, Stress _Stress, double energy);

    virtual string getName() = 0;

    string toString(vector<Material> materials, vector<FENode> p);
};

