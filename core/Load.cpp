#include "Load.h";

#include <format>;

//--------------------------------------------------------------------//
// 荷重条件
// node - 節点ラベル
// coords - 局所座標系
// x,y,z - x,y,z成分
// rx,ry,rz - x,y,z軸周り回転成分
Load::Load(int _node, Coordinates _coords, double x, double y, double z, double rx, double ry, double rz)
    : Vector3R(x, y, z, rx, ry, rz){

    node = node;
    coords = _coords;
    globalX = x;

};

// 荷重条件を表す文字列を返す
// nodes - 節点
string Load::toString(vector<FENode> nodes) {

    string s = format("Load\t{}\t{}", nodes[node].label, globalX);

    if (coords) {
        s += '\t' + coords.label;
    }

    return s;
};
