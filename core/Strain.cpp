#include "Strain.h"

//--------------------------------------------------------------------//
// 歪
// s - 成分
Strain::Strain(double* s) 
    : SymmetricTensor3(s){
    xy = 0.5 * s[3];
    yz = 0.5 * s[4];
    zx = 0.5 * s[5];
};

// テンソルをベクトルとして返す
double* Strain::vector() {

    double s[] = {
        xx, yy, zz, 2 * xy, 2 * yz, 2 * zx 
    };

    return s;
};