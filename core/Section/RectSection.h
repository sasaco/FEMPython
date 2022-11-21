#pragma once
import "Material.h";

#include <string>
#include <numbers>
using namespace std;         //  ���O��Ԏw��


//--------------------------------------------------------------------//
// ��`�f��
// ss - �f�[�^������
class RectSection {

private:
    const double PI = numbers::pi;

    double b1;  // �O����
    double h1;  // �O������
    double b2;  // ������
    double h2;  // ��������

    // �f�ʐ�
    double area;

    // �f�ʂQ�����[�����g
    double iy;
    double iz;


    double zy;
    double zz;

    // �f�ʂQ���Ƀ��[�����g
    double ip;

    // ��`�f�ʂ̝���W�������߂�
    void rectCoef(double ba, double out[4]);

public:
    // ��`�f�ʂ̂���f�␳�W��
    const double KS_RECT = 5 / 6;
    // ��`�f�ʂ̝���W��
    const double COEF_K1 = 64 / pow(numbers::pi, 5);
    const double COEF_K = 8 / pow(numbers::pi, 2);


    RectSection(double ss[4]);


    // ����f�␳�W����Ԃ�
    double shearCoef();

    // �c�E���̓x�N�g����Ԃ�
    void strainStress(Material material, double ex, double thd, double kpy, double kpz,
        double sy, double sz, double out[4][6]);

    // ���ʁE�d�S����̊������[�����g��Ԃ�
    void massInertia(double dens, double l, double out[4]);

    string toString();
};