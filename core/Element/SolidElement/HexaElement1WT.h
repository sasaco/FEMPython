#pragma once
#include "HexaElement1.h";

/// <summary>
/// �U�ʑ̂P����K���v�f�iWilson-Taylor�^�j
/// �J���r��...
/// </summary>
class HexaElement1WT : public HexaElement1 {

private:
    MatrixXd te;

public:
    HexaElement1WT();
    HexaElement1WT(string label, string material, vector<string> nodes);

    string getName() override;

    MatrixXd stiffnessMatrix(vector<FENode> p, MatrixXd d1) override;

    VectorXd strainPart(vector<FENode> p, VectorXd v, VectorXd x) override;

    MatrixXd strainMatrix2(VectorXd ji, VectorXd x);

    VectorXd stiffPart2(MatrixXd d, MatrixXd b1, MatrixXd b2, double coef);

};