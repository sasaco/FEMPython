import "Nodes.h";

using namespace std;         //  名前空間指定


Nodes::Nodes(int* _nodes) {
    nodes = _nodes;
}

int Nodes::nodeCount() {
    return nodes.size();
}