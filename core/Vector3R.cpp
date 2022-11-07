#include "Vector3R.h";

#include <numbers>

//--------------------------------------------------------------------//
// �R�����x�N�g���i���i�{��]�j
// x,y,z - x,y,z����
// rx,ry,rz - x,y,z�������]�p
Vector3R::Vector3R(double _x = 0, double _y = 0, double _z = 0, double _rx = 0, double _ry = 0, double _rz = 0) {
    x[0] = _x;
    x[1] = _y;
    x[2] = _z;
    x[3] = _rx;
    x[4] = _ry;
    x[5] = _rz;
};

// ���i�����̑傫����Ԃ�
double Vector3R::magnitude() {
    return sqrt(magnitudeSq());
};

// ���i�����̑傫����2���Ԃ�
double Vector3R::magnitudeSq() {
    return x[0] * x[0] + x[1] * x[1] + x[2] * x[2];
};

// ��]�����̑傫����Ԃ�
double Vector3R::magnitudeR() {
    return sqrt(magnitudeSqR());
};

// ��]�����̑傫����2���Ԃ�
double Vector3R::magnitudeSqR() {
    return x[3] * x[3] + x[4] * x[4] + x[5] * x[5];
};

// �x�N�g���̃R�s�[��Ԃ�
void Vector3R::clone(Vector3R* out) {

    out->x[0] = x[0];
    out->x[1] = x[1];
    out->x[2] = x[2];
    out->x[3] = x[3];
    out->x[4] = x[4];
    out->x[5] = x[5];
};