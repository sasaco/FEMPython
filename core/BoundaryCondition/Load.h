#pragma once

#include "Vector3R.h"
#include "Coordinates.h"
#include "FENode.h"

#include <string>
#include <vector>
//#include <fmt/core.h>
using namespace std;
using std::string;
using std::vector;

#include<Eigen/Core>
using namespace Eigen;

class Load : public Vector3R {

private:

public:
    string node;
    int nodeIndex;

    string coords;
    vector<double> globalX;

    Load();
    Load(vector<string> columns);
    Load(string _node, string _coords, double x, double y, double z, double rx, double ry, double rz);

    string toString(vector<FENode> nodes);
};
