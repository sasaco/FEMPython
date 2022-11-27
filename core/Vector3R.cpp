#include "Vector3R.h";

#include <numbers>

//--------------------------------------------------------------------//
// ３次元ベクトル（並進＋回転）
// x,y,z - x,y,z成分
// rx,ry,rz - x,y,z軸周り回転角
Vector3R::Vector3R(double _x, double _y, double _z, double _rx, double _ry, double _rz) {
    x[0] = _x;
    x[1] = _y;
    x[2] = _z;
    x[3] = _rx;
    x[4] = _ry;
    x[5] = _rz;
};

// 並進成分の大きさを返す
double Vector3R::magnitude() {
    return sqrt(magnitudeSq());
};

// 並進成分の大きさの2乗を返す
double Vector3R::magnitudeSq() {
    return x[0] * x[0] + x[1] * x[1] + x[2] * x[2];
};

// 回転成分の大きさを返す
double Vector3R::magnitudeR() {
    return sqrt(magnitudeSqR());
};

// 回転成分の大きさの2乗を返す
double Vector3R::magnitudeSqR() {
    return x[3] * x[3] + x[4] * x[4] + x[5] * x[5];
};

// ベクトルのコピーを返す
void Vector3R::clone(Vector3R* out) {

    out->x[0] = x[0];
    out->x[1] = x[1];
    out->x[2] = x[2];
    out->x[3] = x[3];
    out->x[4] = x[4];
    out->x[5] = x[5];
};