#pragma once

#include <string>
#include <vector>
using namespace std;
using std::string;
using std::vector;

#include<Eigen/Core>
using namespace Eigen;

class Coordinates {

private:
    MatrixXd c;

public:
    int label;

    Coordinates();

    Coordinates(int _label,
        double n11, double n12, double n13,
        double n21, double n22, double n23,
        double n31, double n32, double n33);

    void toGlobal(vector<double> x, vector<double> out);

    //void transMatrix(MatrixXd matrix, int dof, int idx0, int ndof);

    //void transVector(VectorXd vector, int dof, int idx0, int ndof);

    string toString();
};