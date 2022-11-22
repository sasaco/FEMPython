#pragma once
#include <string>
#include <vector>
using namespace std;


class Coordinates {

private:

    int label;
    double c[9];

public:
    Coordinates(int _label,
        double n11, double n12, double n13,
        double n21, double n22, double n23,
        double n31, double n32, double n33);

    void toGlobal(double x[6], double out[9]);

    void transMatrix(vector<vector<double>> matrix, int dof, int idx0, int ndof);

    void transVector(vector<double> vector, int dof, int idx0, int ndof);

    string toString();
};