#pragma once

#include "Vector3Dim.hpp"

//#include <fmt/core.h>
#include <string>
using namespace std;

class FENode : public Vector3Dim {

private:

public:
    string label;

    FENode();
    FENode(string _label, double _x, double _y, double _z);

    void clone(FENode out);

    string toString();
};