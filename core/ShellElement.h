#pragma once
#include "FElement.h";
#include "FENode.h";
#include "Material.h";
#include "numeric.h";
#include "ShellParameter.h";

#include <string>
#include <vector>

class ShellElement : public FElement {

private:
    // �O�p�`2���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
    vector<vector<double>> TRI2_INT = { {GTRI2[0], GTRI2[0], C1_6}, {GTRI2[1], GTRI2[0], C1_6},
        {GTRI2[0], GTRI2[1], C1_6} };
    // �l�p�`1���v�f�̐ߓ_�̃�,�ō��W
    vector<vector<double>> QUAD1_NODE = { {-1, -1}, {1, -1}, {1, 1}, {-1, 1} };
    // �l�p�`1���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
    vector<vector<double>> QUAD1_INT = { {GX2[0], GX2[0], 1}, {GX2[1], GX2[0], 1}, {GX2[0], GX2[1], 1},
        {GX2[1], GX2[1], 1} };
    // �O�p�`1���v�f�̎��ʃ}�g���b�N�X�W��
    vector<vector<double>> TRI1_MASS1 = { {1, 0.5, 0.5}, {0.5, 1, 0.5}, {0.5, 0.5, 1} };

    int label;
    int param;
    bool isShell;
    vector<vector<double>> nodeP;
    vector<vector<double>> intP;

public:
    ShellElement(int label, int material, int param, vector<int> nodes, vector<vector<double>> _nodeP, vector<vector<double>> _intP);

    virtual string getName() = 0;

    void jacobianMatrix(vector<FENode> p, vector<vector<double>> sf, double n[3], double t, double out[9]);

    void jacobInv(double ja[9], vector<vector<double>> d, vector<double> out);

    void grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, vector<vector<double>> d, double t, vector<vector<double>> out);

    void strainMatrix1(double ja[9], vector<vector<double>> sf, vector<vector<double>> d, vector<vector<double>> out);

    void strainMatrix(double ja[9], vector<vector<double>> sf, vector<vector<double>> d, double zeta, double t, vector<vector<double>> out);

    void shapePart(vector<FENode> p, vector<double> x, double w, double t, vector<vector<double>> out);

    void gradPart(vector<FENode> p, double x[3], double w, double t, vector<vector<double>> out);

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


