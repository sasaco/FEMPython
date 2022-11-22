#pragma once
#include "ShellElement.h";
#include "numeric.h";


class TriElement1 : public ShellElement {

private:
    // �O�p�`1���v�f�̐ߓ_�̃�,�ō��W
    vector<vector<double>> TRI1_NODE = { {0, 0}, {1, 0}, {0, 1} };
    // �O�p�`1���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
    vector<vector<double>> TRI1_INT = { {C1_3, C1_3, 0.5} };

    // �O�p�`2���v�f�̐ϕ��_�̃�,�ō��W,�d�݌W��
    vector<vector<double>> TRI2_INT = { {GTRI2[0], GTRI2[0], C1_6}, {GTRI2[1], GTRI2[0], C1_6},
        {GTRI2[0], GTRI2[1], C1_6} };

    // �O�p�`1���v�f�̎��ʃ}�g���b�N�X�W��
    vector<vector<double>> TRI1_MASS1 = { {1, 0.5, 0.5}, {0.5, 1, 0.5}, {0.5, 0.5, 1} };


public:
    TriElement1(int label, int material, int param, vector<int> nodes);

    string getName() override;

    int nodeCount() override;

    vector<vector<double>> shapeFunction(double xsi, double eta) override;

    vector<vector<double>> shapeFunction2(double xsi, double eta);

    vector<vector<double>> shapeFunction3(vector<FENode> p, vector<vector<double>> d, double xsi, double eta);

    double jacobian(vector<FENode> p);

    vector<vector<double>> massMatrix(vector<FENode> p, double dens, double t);

    vector<vector<double>> stiffnessMatrix(vector<FENode> p, vector<vector<double>> d1, ShellParameter sp);

    vector<vector<double>> strainMatrix1(vector<vector<double>> sf, vector<vector<double>> jinv);

    vector<vector<double>> strainMatrix2(vector<vector<double>> sf, vector<double> jinv);

    vector<vector<double>> shapeFunctionMatrix(vector<FENode> p, double coef, double t);

    vector<vector<double>> geomStiffnessMatrix(vector<FENode> p, u, d1, ShellParameter sp);

    vector<double> strainPart(vector<FENode> p, vector<double> v, double n[3], vector<vector<double>> d, double xsi, double eta, double zeta, double t);

    vector<vector<double>> strainMatrix(vector<vector<double>> sf1, vector<vector<double>>  sf3, vector<vector<double>> jinv, vector<vector<double>> d, double zeta, double t);

    Strain toStrain(double s[]);

    Stress toStress(double s[]);

};
