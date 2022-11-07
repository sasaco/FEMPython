//--------------------------------------------------------------------//
// S‘©ğŒ
// node - ß“_ƒ‰ƒxƒ‹
// coords - ‹ÇŠÀ•WŒn
// restx,resty,restz - x,y,z•ûŒü‚ÌS‘©‚Ì—L–³
// x,y,z - ‹­§•ÏˆÊ‚Ìx,y,z¬•ª
// restrx,restry,restrz - x,y,z•ûŒü‚Ì‰ñ“]S‘©‚Ì—L–³
// rx,ry,rz - ‹­§•ÏˆÊ‚Ìx,y,z²ü‚è‰ñ“]Šp
var Restraint = function(node, coords, restx, resty, restz, x, y, z,
    restrx, restry, restrz, rx, ry, rz) {
    Vector3R.call(this, x, y, z, rx, ry, rz);
    this.node = node;
    this.coords = coords;
    this.rest = [restx, resty, restz, restrx, restry, restrz];
    this.globalX = this.x;
};

// S‘©ğŒ‚ğ•\‚·•¶š—ñ‚ğ•Ô‚·
// nodes - ß“_
Restraint.prototype.toString = function(nodes) {
    var s = 'Restraint\t' + nodes[this.node].label.toString(10);
    for (var i = 0; i < 6; i++) {
        if (this.rest[i]) {
            s += '\t1\t' + this.x[i];
        }
        else {
            s += '\t0\t' + this.x[i];
        }
    }
    if (this.coords) {
        s += '\t' + this.coords.label.toString(10);
    }
    return s;
};