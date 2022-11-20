#pragma once
#include "vector3.hpp"
#include <vector>
using std::vector;

class Nodes : public Vector3 {

private:
    vector<int> nodes;
    ;
public:
    Nodes(vector<int> _nodes);

    virtual int nodeCount();

};