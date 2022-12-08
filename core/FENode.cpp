#include "FENode.h"

//--------------------------------------------------------------------//
// 節点
// label - 節点ラベル
// x,y,z - x,y,z座標
FENode::FENode() {

    label = -1;
    x = 0;
    y = 0;
    z = 0;
};

FENode::FENode(int _label, double _x, double _y, double _z) {

    label = _label;
    x = _x;
    y = _y;
    z = _z;
};

// 節点のコピーを返す
void FENode::clone(FENode out) {

    out.label = label;
    out.x = x;
    out.y = y;
    out.z = z;
};

// 節点を表す文字列を返す
string FENode::toString() {

    //return format("Node\t{}\t{}\t{}\t{}",
    //    label, x, y, z);

    return "Node";

}
