import "Nodes.h";

using namespace std;         //  ���O��Ԏw��


Nodes::Nodes(int* _nodes) {
    nodes = _nodes;
}

int Nodes::nodeCount() {
    return nodes.size();
}