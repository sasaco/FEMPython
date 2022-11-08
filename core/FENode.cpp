#include "FENode.h";

#include <format>


//--------------------------------------------------------------------//
// �ߓ_
// label - �ߓ_���x��
// x,y,z - x,y,z���W
FENode::FENode(int _label, double _x, double _y, double _z) {

    label = _label;
    x = _x;
    y = _y;
    z = _z;
};

// �ߓ_�̃R�s�[��Ԃ�
void FENode::clone(FENode out) {

    out.label = label;
    out.x = x;
    out.y = y;
    out.z = z;
};

// �ߓ_��\���������Ԃ�
string FENode::toString() {

    return format("Node\t{}\t{}\t{}\t{}",
        label, x, y, z);

};
