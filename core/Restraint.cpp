#include "Restraint.h";

#include <format>

//--------------------------------------------------------------------//
// S‘©ğŒ
// node - ß“_ƒ‰ƒxƒ‹
// coords - ‹ÇŠÀ•WŒn
// restx,resty,restz - x,y,z•ûŒü‚ÌS‘©‚Ì—L–³
// x,y,z - ‹­§•ÏˆÊ‚Ìx,y,z¬•ª
// restrx,restry,restrz - x,y,z•ûŒü‚Ì‰ñ“]S‘©‚Ì—L–³
// rx,ry,rz - ‹­§•ÏˆÊ‚Ìx,y,z²ü‚è‰ñ“]Šp
Restraint::Restraint(int _node, Coordinates _coords,
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

// S‘©ğŒ‚ğ•\‚·•¶š—ñ‚ğ•Ô‚·
// nodes - ß“_
string Restraint::toString(vector<FENode> nodes) {

    string s =  format("Restraint\t{}", nodes[node].label);

    for (int i = 0; i < 6; i++) {
        if (rest[i]) {
            s += '\t1\t' + x[i];
        }
        else {
            s += '\t0\t' + x[i];
        }
    }
    if (coords) {
        s += '\t' + coords.label;
    }
    return s;


};