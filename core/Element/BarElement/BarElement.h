﻿#pragma once
#include "FElement.h"
#include "FENode.h"
#include "Material.h"
#include "SectionManager.h"
#include "BarParameter.h"
#include "Strain.h"
#include "Stress.h"
#include "Vector3Dim.hpp"

#include<string>
#include<vector>
//#include<fmt/core.h>
using namespace std;
using std::string;
using std::vector;

#include<Eigen/Core>
using namespace Eigen;

class BarElement : public FElement {

private:

    string param;
    bool isBar;

protected:
    int I_YMZ[4] = { 1, 5, 7, 11 };	// y軸方向,z軸周り成分のインデックス
    int I_ZMY[4] = { 2, 4, 8, 10 };	// z軸方向,y軸周り成分のインデックス

    Vector3Dim axis;

public:
    BarElement();
    BarElement(string label, string material, string param, vector<string> nodes, Vector3Dim axis);

    MatrixXd stiffnessMatrix(vector<FENode> p, Material material, SectionManager sect);

    virtual MatrixXd stiffBend(double l, Material material, SectionManager sect) = 0;

    MatrixXd gradMatrix(vector<FENode> p, double coef, SectionManager sect);

    MatrixXd geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, Material material, SectionManager sect);

    tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
        strainStress(vector<FENode> p, vector<Vector3R> u, Material material, SectionManager sect);

    virtual vector<VectorXd> bendCurveShare(VectorXd v, double l, Material material, SectionManager sect) = 0;

    tuple<Strain, Stress, double, Strain, Stress, double>
        elementStrainStress(vector<FENode> p, vector<Vector3R> u, Material material, SectionManager sect);

    string toString(vector< Material> materials, vector<BarParameter> params, vector<FENode> p);

    virtual string getName() = 0;
};