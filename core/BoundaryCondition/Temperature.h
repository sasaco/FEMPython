#pragma once
#include "FENode.h"


#include <string>
#include <vector>
using namespace std;         //  名前空間指定

//--------------------------------------------------------------------//
// 温度固定条件
// node - 節点ラベル
// t - 温度
class Temperature {

private:

public:
    string node;
    int nodeIndex;

    double t;

    Temperature();
    Temperature(string _node, double _t);

    string toString(vector<FENode> nodes);
};