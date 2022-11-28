#include "Stress.h"

//--------------------------------------------------------------------//
// 応力
// s - 成分
var Stress = function(s) {
    SymmetricTensor3.call(this, s);
};

// ミーゼス応力を返す
Stress.prototype.mises = function() {
    var dxy = this.xx - this.yy, dyz = this.yy - this.zz, dzx = this.zz - this.xx;
    var ss = dxy * dxy + dyz * dyz + dzx * dzx;
    var tt = this.xy * this.xy + this.yz * this.yz + this.zx * this.zx;
    return Math.sqrt(0.5 * ss + 3 * tt);
};