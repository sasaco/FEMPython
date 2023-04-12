#include "Nodes.h"

Nodes::Nodes() {}
Nodes::Nodes(vector<string>_nodes) {
    nodes = _nodes;
}

int Nodes::nodeCount() {

    int result = (int)nodes.size();
    return result;
}