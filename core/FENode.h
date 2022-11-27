#pragma once

#include "Vector3.hpp"

#include <string>
using namespace std;

class FENode : public Vector3 {

private:

public:
    int label;
    double x;
    double y;
    double z;

    FENode();
    FENode(int _label, double _x, double _y, double _z);

    void clone(FENode out);

    string toString();
};