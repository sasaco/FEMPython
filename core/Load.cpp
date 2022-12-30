#include "Load.h"

//--------------------------------------------------------------------//
// 荷重条件
// 
Load::Load(vector<string> ss) {

    coords = -1;

    node = stoi(ss[1]);
    Vector3R::x[0] = stod(ss[2]);
    Vector3R::x[1] = stod(ss[3]);
    Vector3R::x[2] = stod(ss[4]);
    Vector3R::x[3] = 0;
    Vector3R::x[4] = 0;
    Vector3R::x[5] = 0;

    if (ss.size() < 8) {
        if (ss.size() > 5) {
            coords = stoi(ss[5]);
        }
    }
    else {

        if (ss.size() > 8) {
            coords = stoi(ss[8]);
        }
        Vector3R::x[3] = stod(ss[5]);
        Vector3R::x[4] = stod(ss[6]);
        Vector3R::x[5] = stod(ss[7]);
    }
    for (int i = 0; i < 6; i++) {
        globalX[i] = Vector3R::x[i];
    }
}


// node - 節点ラベル
// coords - 局所座標系
// x,y,z - x,y,z成分
// rx,ry,rz - x,y,z軸周り回転成分
Load::Load(int _node, int _coords, 
            double x, double y, double z, double rx, double ry, double rz) :
    Vector3R(x, y, z, rx, ry, rz) {

    node = _node;
    coords = _coords;
    for (int i = 0; i < 6; i++) {
        globalX[i] = Vector3R::x[i];
    }
};

// 荷重条件を表す文字列を返す
// nodes - 節点
string Load::toString(vector<FENode> nodes) {

    //string s = format("Load\t{}\t{}", nodes[node].label, globalX);
    //s += '\t' + coords;
    //return s;

    return "Load";
};
