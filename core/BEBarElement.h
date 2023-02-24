#pragma once
#include "BarElement.h";

class BEBarElement : public BarElement {

private:

public:
    BEBarElement(int label, int material, int param, , vector<int> nodes, Vector3 axis);

    string getName();

    vector<vector<double>> stiffBend(double l, Material material, Section sect);

    vector<vector<double>> bendCurveShare(vector<double> v, double l, Material material, Section sect);

    vector<vector<double>> massMatrix(vector<FENode> p, double dens, Section sect);

};
