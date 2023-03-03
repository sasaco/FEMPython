#include "Restraint.h"


//--------------------------------------------------------------------//
// 拘束条件
Restraint::Restraint() : Vector3R() {
    node = -1;
    coords = -1;
}
Restraint::Restraint(vector<string> ss) : Vector3R() {

    node = stoi(ss[1]);
    rest[0] = stoi(ss[2]) != 0;
    rest[1] = stoi(ss[4]) != 0;
    rest[2] = stoi(ss[6]) != 0;
    rest[3] = false;
    rest[4] = false;
    rest[5] = false;

    Vector3R::x[0] = stod(ss[3]);
    Vector3R::x[1] = stod(ss[5]);
    Vector3R::x[2] = stod(ss[7]);
    Vector3R::x[3] = 0;
    Vector3R::x[4] = 0;
    Vector3R::x[5] = 0;

    coords = -1;

    if (ss.size() < 14) {
        if (ss.size() > 8) {
            coords = stoi(ss[8]);
        }
    }
    else {
        if (ss.size() > 14) {
            coords = stoi(ss[14]);
        } 
        rest[3] = stoi(ss[8]) != 0;
        rest[4] = stoi(ss[10]) != 0;
        rest[5] = stoi(ss[12]) != 0;
        Vector3R::x[3] = stod(ss[9]);
        Vector3R::x[4] = stod(ss[11]);
        Vector3R::x[5] = stod(ss[13]);
    }
}

// node - 節点ラベル
// coords - 局所座標系
// restx,resty,restz - x,y,z方向の拘束の有無
// x,y,z - 強制変位のx,y,z成分
// restrx,restry,restrz - x,y,z方向の回転拘束の有無
// rx,ry,rz - 強制変位のx,y,z軸周り回転角
Restraint::Restraint(
    int _node, int _coords,
    bool restx, bool resty, bool restz,
    double x, double y, double z,
    bool restrx, bool restry, bool restrz, 
    double rx, double ry, double rz) : Vector3R(x, y, z, rx, ry, rz) {

    node = _node;
    coords = _coords;

    rest[0] = restx; 
    rest[1] = resty;
    rest[2] = restz;
    rest[3] = restrx;
    rest[4] = restry;
    rest[5] = restrz;

};

// 拘束条件を表す文字列を返す
// nodes - 節点
string Restraint::toString(vector<FENode> nodes) {

    //string s =  format("Restraint\t{}", nodes[node].label);

    //for (int i = 0; i < 6; i++) {
    //    if (rest[i]) {
    //        s += "\t1\t";
    //        s += to_string(x[i]);
    //    }
    //    else {
    //        s += "\t0\t";
    //        s += to_string(x[i]);
    //    }
    //}

    //s += "\t";
    //s += to_string(coords);

    //return s;

    return "Restraint";

};