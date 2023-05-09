#pragma once

#include"Material.h"

#include<Eigen/Core>
using namespace Eigen;

class SectionManager {

private:

public:

    // �f�ʐ�
    double area;
    // �f�ʂQ�����[�����g
    double iy;
    double iz;
    // �f�ʂQ���Ƀ��[�����g
    double ip;

    SectionManager();

    // ����f�␳�W����Ԃ�
    virtual double shearCoef() = 0;

    // �c�E���̓x�N�g����Ԃ�
    virtual MatrixXd strainStress(Material material, double ex, double thd, double kpy, double kpz,
        double sy, double sz) = 0;

    // ���ʁE�d�S����̊������[�����g��Ԃ�
    virtual VectorXd massInertia(double dens, double l) = 0;

    // �f�ʂ�\���������Ԃ�
    virtual string toString() = 0;


};

