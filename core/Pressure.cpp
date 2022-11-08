#include "Pressure.h";

#include <format>


//--------------------------------------------------------------------//
// –Êˆ³ğŒ
// element - —v‘fƒ‰ƒxƒ‹
// face - —v‘f‹«ŠE–Ê
// press - –Êˆ³
Pressure::Pressure(int _element, string _face, double _press) : ElementBorderBound(_element, _face) {

    element = _element;
    press = _press;
    face = _face;

};

// –Êˆ³ğŒ‚ğ•\‚·•¶š—ñ‚ğ•Ô‚·
// elems - —v‘f
string Pressure::toString(vector<T> elems) {
    return format("Pressure\t{}\t{}\t{}", elems[element].label, face press);
};