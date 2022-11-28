#include "Stress.h"

//--------------------------------------------------------------------//
// ‰—Í
// s - ¬•ª
var Stress = function(s) {
    SymmetricTensor3.call(this, s);
};

// ƒ~[ƒ[ƒX‰—Í‚ğ•Ô‚·
Stress.prototype.mises = function() {
    var dxy = this.xx - this.yy, dyz = this.yy - this.zz, dzx = this.zz - this.xx;
    var ss = dxy * dxy + dyz * dyz + dzx * dzx;
    var tt = this.xy * this.xy + this.yz * this.yz + this.zx * this.zx;
    return Math.sqrt(0.5 * ss + 3 * tt);
};