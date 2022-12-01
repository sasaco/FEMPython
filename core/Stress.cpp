#include "Stress.h"

//--------------------------------------------------------------------//
// 応力
// s - 成分
Stress::Stress(VectorXd s)
    : SymmetricTensor3(s){
};

// ミーゼス応力を返す
double Stress::mises() {

    double dxy = xx - yy;
    double dyz = yy - zz;
    double dzx = zz - xx;
    double ss = dxy * dxy + dyz * dyz + dzx * dzx;
    double tt = xy * xy + yz * yz + zx * zx;
    return sqrt(0.5 * ss + 3 * tt);
};