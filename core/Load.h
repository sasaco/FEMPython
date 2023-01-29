#pragma once

#include "Vector3R.h"
#include "Coordinates.h"
#include "FENode.h"

#include <string>
#include <vector>
//#include <format>
using namespace std;
using std::string;
using std::vector;

#include<Eigen/Core>
using namespace Eigen;

class Load : public Vector3R {

private:

public:
    int node;
    int coords;
    double globalX[6];

    Load();
    Load(vector<string> columns);
    Load(int _node, int _coords, double x, double y, double z, double rx, double ry, double rz);

    string toString(vector<FENode> nodes);
};
