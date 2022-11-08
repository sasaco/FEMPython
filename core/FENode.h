#pragma once
#include <string>
using namespace std;         //  –¼‘O‹óŠÔŽw’è

class FENode {

private:

public:
    int label;
    double x;
    double y;
    double z;

    FENode(int _label, double _x, double _y, double _z);

    void clone(FENode out);

    string toString();
};