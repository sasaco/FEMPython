#pragma once

#include "Vector3R.h"
#include "Coordinates.h"
#include "FENode.h"

//#include <format>
#include <string>
#include <vector>
using namespace std;         //  名前空間指定
using std::string;
using std::vector;

#include<Eigen/Core>
using namespace Eigen;

class Restraint : public Vector3R {

private:

public:
    int node;
    int coords;
    bool rest[6];
    double globalX[6];

    Restraint();
    Restraint(vector<string> columns);

    Restraint(int _node, int _coords,
        bool restx, bool resty, bool restz,
        double x, double y, double z,
        bool restrx, bool restry, bool restrz,
        double rx, double ry, double rz);


    string toString(vector<FENode> nodes);

};