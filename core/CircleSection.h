#pragma once

#include <string>
using namespace std;         //  ���O��Ԏw��
#include <numbers>


import "Material.h";

//--------------------------------------------------------------------//
// �~�`�f��
// ss - �f�[�^������
class CircleSection {

private:
    const double PI = numbers::pi;

    double d1;  // �O�a
    double d2;  // ���a
    // �f�ʐ�
    double area;
    // �f�ʂQ�����[�����g
    double iy;
    double iz;
    // �f�ʂQ���Ƀ��[�����g
    double ip;

public:
    // �~�`�f�ʂ̂���f�␳�W��
    const double KS_CIRCLE = 6 / 7;

    CircleSection(double ss[2]);

    // ����f�␳�W����Ԃ�
    double shearCoef();

    // �c�E���̓x�N�g����Ԃ�
    void strainStress(Material material, double ex, double thd, double kpy, double kpz,
        double sy, double sz, double out[4][6]);

    // ���ʁE�d�S����̊������[�����g��Ԃ�
    void massInertia(double dens, double l, double out[4]);

    // �f�ʂ�\���������Ԃ�
    string toString();
};
