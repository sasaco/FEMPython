#pragma once

#include <numbers>
#include <cmath>

//--------------------------------------------------------------------//
// ３次元ベクトル（並進＋回転）
// x,y,z - x,y,z成分
// rx,ry,rz - x,y,z軸周り回転角
class Vector3R {

private:

public:
    Vector3R();
    Vector3R(double _x, double _y, double _z, double _rx, double _ry, double _rz);

    double x[6];

    // 並進成分の大きさを返す
    double magnitude();
    // 並進成分の大きさの2乗を返す
    double magnitudeSq();

    // 回転成分の大きさを返す
    double magnitudeR();

    // 回転成分の大きさの2乗を返す
    double magnitudeSqR();

    // ベクトルのコピーを返す
    void clone(Vector3R* out);
};