#include "Nodes.h"

Nodes::Nodes(vector<int>_nodes) {
    nodes = _nodes;
}

int Nodes::nodeCount() {

    int result = (int)nodes.size();
    return result;
}