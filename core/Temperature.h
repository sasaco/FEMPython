#pragma once
#include "FENode.h"


#include <string>
#include <vector>
using namespace std;         //  ���O��Ԏw��

//--------------------------------------------------------------------//
// ���x�Œ����
// node - �ߓ_���x��
// t - ���x
class Temperature {

private:
    int node;
    double t;

public:
    Temperature(int _node, double _t);

    string toString(vector<FENode> nodes);
};