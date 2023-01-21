#pragma once

#include"Material.h"

#include<Eigen/Core>
using namespace Eigen;

class Section {

private:


public:
    // �f�ʐ�
    double area;

    // �f�ʂQ�����[�����g
    double iy;
    double iz;

    // �f�ʂQ���Ƀ��[�����g
    double ip;

    // �c�E���̓x�N�g����Ԃ�
    MatrixXd strainStress(Material material, double ex, double thd, double kpy, double kpz,
        VectorXd sy, VectorXd sz);
};

