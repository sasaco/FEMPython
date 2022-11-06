import "Nodes.h";

using namespace std;         //  –¼‘O‹óŠÔŽw’è


Nodes::Nodes(int* _nodes) {
    nodes = _nodes;
}

int Nodes::nodeCount() {
    return nodes.size();
}