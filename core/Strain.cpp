#include "Strain.h"
//--------------------------------------------------------------------//
// �c
// s - ����
var Strain = function(s) {
    SymmetricTensor3.call(this, s);
    this.xy = 0.5 * s[3];
    this.yz = 0.5 * s[4];
    this.zx = 0.5 * s[5];
};

// �e���\�����x�N�g���Ƃ��ĕԂ�
Strain.prototype.vector = function() {
    return[this.xx, this.yy, this.zz, 2 * this.xy, 2 * this.yz, 2 * this.zx];
};