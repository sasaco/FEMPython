#include "SymmetricTensor3.h"
//--------------------------------------------------------------------//
// ３次元対称テンソル
// s - 成分
var SymmetricTensor3 = function(s) {
    this.xx = s[0];
    this.yy = s[1];
    this.zz = s[2];
    this.xy = s[3];
    this.yz = s[4];
    this.zx = s[5];
};

// テンソルをベクトルとして返す
SymmetricTensor3.prototype.vector = function() {
    return[this.xx, this.yy, this.zz, this.xy, this.yz, this.zx];
};

// テンソルを加える
// t - 加えるテンソル
SymmetricTensor3.prototype.add = function(t) {
    this.xx += t.xx;
    this.yy += t.yy;
    this.zz += t.zz;
    this.xy += t.xy;
    this.yz += t.yz;
    this.zx += t.zx;
};

// 成分にスカラーを掛ける
// a - 掛けるスカラー
SymmetricTensor3.prototype.mul = function(a) {
    this.xx *= a;
    this.yy *= a;
    this.zz *= a;
    this.xy *= a;
    this.yz *= a;
    this.zx *= a;
};

// 固有値を返す
SymmetricTensor3.prototype.principal = function() {
    return eigenvalue(this, 100).lambda;
};

// テンソルを回転させる
// d - 方向余弦マトリックス
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

// テンソルの内積を計算する
// t - 相手のテンソル
SymmetricTensor3.prototype.innerProduct = function(t) {
    return this.xx * t.xx + this.yy * t.yy + this.zz * t.zz +
        2 * (this.xy * t.xy + this.yz * t.yz + this.zx * t.zx);
};