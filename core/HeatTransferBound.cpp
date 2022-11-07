//--------------------------------------------------------------------//
// ”M“`’B‹«ŠEğŒ
// element - —v‘fƒ‰ƒxƒ‹
// face - —v‘f‹«ŠE–Ê
// htc - ”M“`’B—¦
// outTemp - ŠO•”‰·“x
var HeatTransferBound = function(element, face, htc, outTemp) {
    ElementBorderBound.call(this, element, face);
    this.htc = htc;
    this.outTemp = outTemp;
};

// ”M“`’B‹«ŠEğŒ‚ğ•\‚·•¶š—ñ‚ğ•Ô‚·
// elems - —v‘f
HeatTransferBound.prototype.toString = function(elems) {
    return 'HTC\t' + elems[this.element].label.toString(10) + '\t' +
        this.face + '\t' + this.htc + '\t' + this.outTemp;
};