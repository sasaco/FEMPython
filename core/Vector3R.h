#pragma once
//--------------------------------------------------------------------//
// �R�����x�N�g���i���i�{��]�j
// x,y,z - x,y,z����
// rx,ry,rz - x,y,z�������]�p
class Vector3R {

private:

public:
    Vector3R(double _x = 0, double _y = 0, double _z = 0, double _rx = 0, double _ry = 0, double _rz = 0);

    double x[6];

    // ���i�����̑傫����Ԃ�
    double magnitude();
    // ���i�����̑傫����2���Ԃ�
    double magnitudeSq();

    // ��]�����̑傫����Ԃ�
    double magnitudeR();

    // ��]�����̑傫����2���Ԃ�
    double magnitudeSqR();

    // �x�N�g���̃R�s�[��Ԃ�
    void clone(Vector3R* out);
};