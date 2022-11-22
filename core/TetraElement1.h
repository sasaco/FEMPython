#pragma once
#include "SolidElement.h";

/// <summary>
/// Žl–Ê‘Ì1ŽŸ—v‘f
/// </summary>
class TetraElement1 : public SolidElement {

private:


public:
    TetraElement1(int label, int material, vector<int> nodes);

    string getName() override;

    void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) override;

    double jacobian(vector<FENode> p);

    void grad(vector<FENode> p, double ja, vector<vector<double>> out);

    void massMatrix(vector<FENode> p, double dens, vector<vector<double>> out) override;

    void stiffnessMatrix(vector<FENode> p, vector<vector<double>> d1, vector<vector<double>> out) override;

    void shapeFunctionMatrix(vector<FENode> p, double coef, vector<vector<double>> out) override;

    void gradMatrix(vector<FENode> p, double coef, vector<vector<double>> out) override;

    void geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
        vector<vector<double>> out) override;

    void strainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
        vector<Strain> strain, vector<Stress> stress, vector<double> energy) override;

    void elementStrainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
        Strain _Strain, Stress _Stress, double energy) override;
};

