#include "Strain.h"

//--------------------------------------------------------------------//
// �c
// s - ����
Strain::Strain(VectorXd s)
    : SymmetricTensor3(s){
    xy = 0.5 * s[3];
    yz = 0.5 * s[4];
    zx = 0.5 * s[5];
};

// �e���\�����x�N�g���Ƃ��ĕԂ�
VectorXd Strain::vector() {

    VectorXd s;
    s << xx, yy, zz, 2 * xy, 2 * yz, 2 * zx ;

    return s;
};