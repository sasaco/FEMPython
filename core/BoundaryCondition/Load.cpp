#include "Load.h"

//--------------------------------------------------------------------//
// 荷重条件
// 
Load::Load() : Vector3R() {
    coords = -1;
    node = "";
    nodeIndex = -1;
}
Load::Load(vector<string> ss) {

    coords = -1;

    node = ss[1];
    nodeIndex = -1;

    Vector3R::x.clear();
    Vector3R::x.push_back(stod(ss[2]));
    Vector3R::x.push_back(stod(ss[3]));
    Vector3R::x.push_back(stod(ss[4]));
    Vector3R::x.push_back(0);
    Vector3R::x.push_back(0);
    Vector3R::x.push_back(0);

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
    globalX.clear();
    for (int i = 0; i < 6; i++) {
        globalX.push_back(Vector3R::x[i]);
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

    globalX.clear();
    for (int i = 0; i < 6; i++) {
        globalX.push_back(Vector3R::x[i]);
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
