#pragma once
#include <vector>
using std::vector;

class Nodes {

private:
    vector<int> nodes;
    ;
public:
    Nodes(vector<int> _nodes);

    virtual int nodeCount();

};