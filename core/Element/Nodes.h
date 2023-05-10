#pragma once
#include "Vector3Dim.hpp"
#include <string>
#include <vector>
using namespace std;
using std::vector;

class Nodes : public Vector3Dim {

private:

public:
    vector<string> nodes;

    Nodes();
    Nodes(vector<string> _nodes);

    virtual int nodeCount();

};