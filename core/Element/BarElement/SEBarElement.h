#pragma once

#include "BarElement.h";

class SEBarElement : public BarElement {

private:

public:
    SEBarElement();
    SEBarElement(string label, vector<string> nodes, string material);

    string getName();

    MatrixXd stiffBend(double l, Material material, Section sect);

    MatrixXd bendCurveShare(VectorXd v, double l, Material material, Section sect);

    //vector<vector<double>> massMatrix(vector<FENode> p, double dens, Section sect);
};