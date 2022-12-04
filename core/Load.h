#pragma once

#include "Vector3R.h"
#include "Coordinates.h"
#include "FENode.h"

#include <string>
#include <vector>
#include <format>
using namespace std;
using std::string;
using std::vector;


class Load : public Vector3R {

private:
    double globalX;

public:
    int node;
    int coords;

    Load(vector<string> columns);
    Load(int _node, int _coords, double x, double y, double z, double rx, double ry, double rz);

    string toString(vector<FENode> nodes);
};
