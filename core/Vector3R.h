#pragma once
//--------------------------------------------------------------------//
// ３次元ベクトル（並進＋回転）
// x,y,z - x,y,z成分
// rx,ry,rz - x,y,z軸周り回転角
class Vector3R {

private:

public:
    Vector3R(double _x = 0, double _y = 0, double _z = 0, double _rx = 0, double _ry = 0, double _rz = 0);

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