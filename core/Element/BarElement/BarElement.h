#pragma once
#include "FElement.h"
#include "Vector3.h"


class BarElement : public FElement {

private:
    double I_YMZ[4] = { 1, 5, 7, 11 };	// y������,z�����萬���̃C���f�b�N�X
    double I_ZMY[4] = { 2, 4, 8, 10 };	// z������,y�����萬���̃C���f�b�N�X

    int param;
    bool isBar;
    Vector3 axis;

public:
    BarElement(int label, int material, int param, vector<int> nodes, Vector3 axis);

    vector<vector<double>> stiffnessMatrix(vector<FENode> p, Material material, Section sect);

    vector<vector<double>> gradMatrix(vector<FENode> p, double coef, Section sect);

    vector<vector<double>> geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, Material material, Section sect);

    vector<vector<double>> strainStress(vector<FENode> p, vector<BoundaryCondition> u, Material material, Section sect);

    vector<vector<double>> elementStrainStress(vector<FENode> p, vector<BoundaryCondition> u, Material material, Section sect);

    string toString(vector< Material> materials, vector< BarParameter> params, vector<FENode> p);

};