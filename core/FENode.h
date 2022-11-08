#pragma once
#include <string>
using namespace std;         //  名前空間指定

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