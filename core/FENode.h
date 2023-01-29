#pragma once

#include "Vector3.hpp"

//#include <format>
#include <string>
using namespace std;

class FENode : public Vector3 {

private:

public:
    int label;

    FENode();
    FENode(int _label, double _x, double _y, double _z);

    void clone(FENode out);

    string toString();
};