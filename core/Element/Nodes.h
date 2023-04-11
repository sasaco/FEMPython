#pragma once
#include "Vector3Dim.hpp"
#include <vector>
using std::vector;

class Nodes : public Vector3Dim {

private:

public:
    vector<string> nodes;

    Nodes();
    Nodes(vector<int> _nodes);

    virtual int nodeCount();

};