#include "Vector3R.h";

#include <numbers>

//--------------------------------------------------------------------//
// ‚RŽŸŒ³ƒxƒNƒgƒ‹i•Ài{‰ñ“]j
// x,y,z - x,y,z¬•ª
// rx,ry,rz - x,y,zŽ²Žü‚è‰ñ“]Šp
Vector3R::Vector3R(double _x = 0, double _y = 0, double _z = 0, double _rx = 0, double _ry = 0, double _rz = 0) {
    x[0] = _x;
    x[1] = _y;
    x[2] = _z;
    x[3] = _rx;
    x[4] = _ry;
    x[5] = _rz;
};

// •Ài¬•ª‚Ì‘å‚«‚³‚ð•Ô‚·
double Vector3R::magnitude() {
    return sqrt(magnitudeSq());
};

// •Ài¬•ª‚Ì‘å‚«‚³‚Ì2æ‚ð•Ô‚·
double Vector3R::magnitudeSq() {
    return x[0] * x[0] + x[1] * x[1] + x[2] * x[2];
};

// ‰ñ“]¬•ª‚Ì‘å‚«‚³‚ð•Ô‚·
double Vector3R::magnitudeR() {
    return sqrt(magnitudeSqR());
};

// ‰ñ“]¬•ª‚Ì‘å‚«‚³‚Ì2æ‚ð•Ô‚·
double Vector3R::magnitudeSqR() {
    return x[3] * x[3] + x[4] * x[4] + x[5] * x[5];
};

// ƒxƒNƒgƒ‹‚ÌƒRƒs[‚ð•Ô‚·
void Vector3R::clone(Vector3R* out) {

    out->x[0] = x[0];
    out->x[1] = x[1];
    out->x[2] = x[2];
    out->x[3] = x[3];
    out->x[4] = x[4];
    out->x[5] = x[5];
};