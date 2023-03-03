#include "Strain.h"

//--------------------------------------------------------------------//
// 歪
// s - 成分
Strain::Strain() : SymmetricTensor3() {
};
Strain::Strain(VectorXd s)
    : SymmetricTensor3(s){
    xy = 0.5 * s[3];
    yz = 0.5 * s[4];
    zx = 0.5 * s[5];
};

// テンソルをベクトルとして返す
VectorXd Strain::vector() {

    VectorXd s(6);
    s(0) = xx;
    s(1) = yy;
    s(2) = zz;
    s(3) = 2 * xy;
    s(4) = 2 * yz;
    s(5) = 2 * zx;

    return s;
};