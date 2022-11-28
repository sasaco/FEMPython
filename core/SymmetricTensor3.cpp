#include "SymmetricTensor3.h"
//--------------------------------------------------------------------//
// �R�����Ώ̃e���\��
// s - ����
var SymmetricTensor3 = function(s) {
    this.xx = s[0];
    this.yy = s[1];
    this.zz = s[2];
    this.xy = s[3];
    this.yz = s[4];
    this.zx = s[5];
};

// �e���\�����x�N�g���Ƃ��ĕԂ�
SymmetricTensor3.prototype.vector = function() {
    return[this.xx, this.yy, this.zz, this.xy, this.yz, this.zx];
};

// �e���\����������
// t - ������e���\��
SymmetricTensor3.prototype.add = function(t) {
    this.xx += t.xx;
    this.yy += t.yy;
    this.zz += t.zz;
    this.xy += t.xy;
    this.yz += t.yz;
    this.zx += t.zx;
};

// �����ɃX�J���[���|����
// a - �|����X�J���[
SymmetricTensor3.prototype.mul = function(a) {
    this.xx *= a;
    this.yy *= a;
    this.zz *= a;
    this.xy *= a;
    this.yz *= a;
    this.zx *= a;
};

// �ŗL�l��Ԃ�
SymmetricTensor3.prototype.principal = function() {
    return eigenvalue(this, 100).lambda;
};

// �e���\������]������
// d - �����]���}�g���b�N�X
SymmetricTensor3.prototype.rotate = function(d) {
    var mat = [[this.xx, this.xy, this.zx], [this.xy, this.yy, this.yz],
        [this.zx, this.yz, this.zz]];
    var s = [0, 0, 0, 0, 0, 0];
    for (var i = 0; i < 3; i++) {
        for (var j = 0; j < 3; j++) {
            var mij = mat[i][j];
            for (var k = 0; k < 3; k++) {
                s[k] += d[k][i] * d[k][j] * mij;
                s[k + 3] += d[k][i] * d[(k + 1) % 3][j] * mij;
            }
        }
    }
    this.xx = s[0];
    this.yy = s[1];
    this.zz = s[2];
    this.xy = s[3];
    this.yz = s[4];
    this.zx = s[5];
};

// �e���\���̓��ς��v�Z����
// t - ����̃e���\��
SymmetricTensor3.prototype.innerProduct = function(t) {
    return this.xx * t.xx + this.yy * t.yy + this.zz * t.zz +
        2 * (this.xy * t.xy + this.yz * t.yz + this.zx * t.zx);
};