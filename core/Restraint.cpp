#include "Restraint.h"


//--------------------------------------------------------------------//
// 拘束条件
// node - 節点ラベル
// coords - 局所座標系
// restx,resty,restz - x,y,z方向の拘束の有無
// x,y,z - 強制変位のx,y,z成分
// restrx,restry,restrz - x,y,z方向の回転拘束の有無
// rx,ry,rz - 強制変位のx,y,z軸周り回転角
Restraint::Restraint(
    int _node, Coordinates _coords,
    bool restx, bool resty, bool restz,
    double x, double y, double z,
    bool restrx, bool restry, bool restrz, 
    double rx, double ry, double rz) : Vector3R(x, y, z, rx, ry, rz) {
    ;
    node = _node;
    coords = _coords;

    rest[0] = restx; 
    rest[1] = resty;
    rest[2] = restz;
    rest[3] = restrx;
    rest[4] = restry;
    rest[5] = restrz;

    globalX = x;
};

// 拘束条件を表す文字列を返す
// nodes - 節点
string Restraint::toString(vector<FENode> nodes) {

    string s =  format("Restraint\t{}", nodes[node].label);

    for (int i = 0; i < 6; i++) {
        if (rest[i]) {
            s += "\t1\t";
            s += to_string(x[i]);
        }
        else {
            s += "\t0\t";
            s += to_string(x[i]);
        }
    }

    s += "\t";
    s += to_string(coords.label);

    return s;

};