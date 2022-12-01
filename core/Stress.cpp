#include "Stress.h"

//--------------------------------------------------------------------//
// ‰—Í
// s - ¬•ª
Stress::Stress(VectorXd s)
    : SymmetricTensor3(s){
};

// ƒ~[ƒ[ƒX‰—Í‚ğ•Ô‚·
double Stress::mises() {

    double dxy = xx - yy;
    double dyz = yy - zz;
    double dzx = zz - xx;
    double ss = dxy * dxy + dyz * dyz + dzx * dzx;
    double tt = xy * xy + yz * yz + zx * zx;
    return sqrt(0.5 * ss + 3 * tt);
};