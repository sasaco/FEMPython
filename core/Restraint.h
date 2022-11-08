#pragma once
#include "Vector3R.h";
#include "Coordinates.h";
#include "FENode.h";

#include <string>
#include <vector>
using namespace std;         //  –¼‘O‹óŠÔŽw’è

class Restraint : public Vector3R {

private:
    double globalX;

public:
    int node;
    Coordinates coords;
    bool rest[6];

    Restraint(int _node, Coordinates _coords,
        bool restx, bool resty, bool restz,
        double x, double y, double z,
        bool restrx, bool restry, bool restrz,
        double rx, double ry, double rz);


    string toString(vector<FENode> nodes);

};