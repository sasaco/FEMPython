#pragma once
#include "BarElement.h"

class BEBarElement : public BarElement {

private:

public:
    BEBarElement();
    BEBarElement(string label, string material, string param, vector<string> nodes, Vector3Dim axis);

    string getName() override;

    MatrixXd stiffBend(double l, Material material, SectionManager &sect) override;

    vector<VectorXd> bendCurveShare(VectorXd v, double l, Material material, SectionManager &sect) override;

    MatrixXd massMatrix(vector<FENode> p, double dens, SectionManager sect);

};
