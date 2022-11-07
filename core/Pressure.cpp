//--------------------------------------------------------------------//
// –Êˆ³ğŒ
// element - —v‘fƒ‰ƒxƒ‹
// face - —v‘f‹«ŠE–Ê
// press - –Êˆ³
var Pressure = function(element, face, press) {
    ElementBorderBound.call(this, element, face);
    this.press = press;
};

// –Êˆ³ğŒ‚ğ•\‚·•¶š—ñ‚ğ•Ô‚·
// elems - —v‘f
Pressure.prototype.toString = function(elems) {
    return 'Pressure\t' + elems[this.element].label.toString(10) + '\t' +
        this.face + '\t' + this.press;
};