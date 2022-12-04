#pragma once

#include "Vector3R.h"
#include "Coordinates.h"
#include "FENode.h"

#include <format>
#include <string>
#include <vector>
using namespace std;         //  名前空間指定
using std::string;
using std::vector;

class Restraint : public Vector3R {

private:
    double globalX;

public:
    int node;
    int coords;
    bool rest[6];

    Restraint(vector<string> columns);
    
    Restraint(int _node, int _coords,
        bool restx, bool resty, bool restz,
        double x, double y, double z,
        bool restrx, bool restry, bool restrz,
        double rx, double ry, double rz);


    string toString(vector<FENode> nodes);

};