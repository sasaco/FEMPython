#include "Load.h";

#include <format>;

//--------------------------------------------------------------------//
// �׏d����
// node - �ߓ_���x��
// coords - �Ǐ����W�n
// x,y,z - x,y,z����
// rx,ry,rz - x,y,z�������]����
Load::Load(int _node, Coordinates _coords, double x, double y, double z, double rx, double ry, double rz)
    : Vector3R(x, y, z, rx, ry, rz){

    node = node;
    coords = _coords;
    globalX = x;

};

// �׏d������\���������Ԃ�
// nodes - �ߓ_
string Load::toString(vector<FENode> nodes) {

    string s = format("Load\t{}\t{}", nodes[node].label, globalX);

    if (coords) {
        s += '\t' + coords.label;
    }

    return s;
};
