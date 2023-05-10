#pragma once

#include"Material.h"

#include"RectSection.h"
#include"CircleSection.h"

#include<Eigen/Core>
using namespace Eigen;

class SectionManager {

private:
    string type;
    RectSection _RectSection;
    CircleSection _CircleSection;

public:
    SectionManager();
    SectionManager(string _type, vector<double> ss);

    // �f�ʐ�
    double area();

    // �f�ʂQ�����[�����g
    double iy();
    double iz();

    // �f�ʂQ���Ƀ��[�����g
    double ip();

    // ����f�␳�W����Ԃ�
    double shearCoef();

    // �c�E���̓x�N�g����Ԃ�
    MatrixXd strainStress(Material material, double ex, double thd, double kpy, double kpz,
        double sy, double sz);

    // ���ʁE�d�S����̊������[�����g��Ԃ�
    VectorXd massInertia(double dens, double l);

    // �f�ʂ�\���������Ԃ�
    string toString();


};

