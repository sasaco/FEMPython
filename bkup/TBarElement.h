#pragma once
#include "BarElement.h"

class TBarElement : public BarElement {

private:


public:
    TBarElement(int label, int material, int param, double* nodes, double* axis);

    string getName();

    vector<vector<double>> stiffBend(double l, Material material, Section sect);

    vector<vector<double>> bendCurveShare(vector<double> v, double l, Material material, Section sect);

    vector<vector<double>> massMatrix(vector<FENode> p, double dens, Section sect);

};