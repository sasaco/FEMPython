#include "HeatTransferBound.h";

#include <format>

//--------------------------------------------------------------------//
// ”M“`’B‹«ŠEğŒ
// element - —v‘fƒ‰ƒxƒ‹
// face - —v‘f‹«ŠE–Ê
// htc - ”M“`’B—¦
// outTemp - ŠO•”‰·“x
HeatTransferBound::HeatTransferBound(int _element, string face, double _htc, double _outTemp) 
    : ElementBorderBound(_element, _face){
    htc = _htc;
    outTemp = _outTemp;
}

// ”M“`’B‹«ŠEğŒ‚ğ•\‚·•¶š—ñ‚ğ•Ô‚·
// elems - —v‘f
string HeatTransferBound::toString(vecor<T> elems) {

    return format("HTC\t{}\t{}\t{}\t{}\t{}", elems[element].label, face, htc, outTemp);

};