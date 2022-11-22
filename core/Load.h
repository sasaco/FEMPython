#pragma once
#include "Vector3R.h";
#include "Coordinates.h";
#include "FENode.h";

#include <string>;
#include <vector>;
using namespace std;


class Load : public Vector3R {

private:
    double globalX;

public:
    int node;
    Coordinates coords;

    Load(int _node, Coordinates _coords, double x, double y, double z, double rx, double ry, double rz);

    string toString(vector<FENode> nodes);
};
