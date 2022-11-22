#pragma once
#include "ShellElement.h";

class QuadElement1 : public ShellElement {

private:
    // 四角形1次要素の節点のξ,η座標
    vector<vector<double>> QUAD1_NODE = { {-1, -1}, {1, -1}, {1, 1}, {-1, 1} };
    // 四角形1次要素の積分点のξ,η座標,重み係数
    vector<vector<double>> QUAD1_INT = { {GX2[0], GX2[0], 1}, {GX2[1], GX2[0], 1}, {GX2[0], GX2[1], 1},
        {GX2[1], GX2[1], 1} };

public:
    QuadElement1(int label, int material, int param, vector<int> nodes);

    string getName() override;

    vector<vector<double>> shapeFunction(double xsi, double eta);

    vector<vector<double>> massMatrix(vector<FENode> p, double dens, double t);

    vector<vector<double>> massMatrix(vector<FENode> p, double dens, double t);

    vector<vector<double>> stiffnessMatrix(vector<FENode> p, vector<vector<double>> d1, ShellParameter sp);

    vector<vector<double>> stiffPart(vector<FENode> p, vector<vector<double>> d1, double* n,
        double xsi, double eta, double t);

};