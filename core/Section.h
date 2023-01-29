#pragma once

#include"Material.h"

#include<Eigen/Core>
using namespace Eigen;

class Section {

private:


public:
    // 断面積
    double area;

    // 断面２次モーメント
    double iy;
    double iz;

    // 断面２次極モーメント
    double ip;

    // 歪・応力ベクトルを返す
    MatrixXd strainStress(Material material, double ex, double thd, double kpy, double kpz,
        VectorXd sy, VectorXd sz);
};

