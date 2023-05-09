#pragma once

#include"Material.h"

#include<Eigen/Core>
using namespace Eigen;

class SectionManager {

private:

public:

    // 断面積
    double area;
    // 断面２次モーメント
    double iy;
    double iz;
    // 断面２次極モーメント
    double ip;

    SectionManager();

    // せん断補正係数を返す
    virtual double shearCoef() = 0;

    // 歪・応力ベクトルを返す
    virtual MatrixXd strainStress(Material material, double ex, double thd, double kpy, double kpz,
        double sy, double sz) = 0;

    // 質量・重心周りの慣性モーメントを返す
    virtual VectorXd massInertia(double dens, double l) = 0;

    // 断面を表す文字列を返す
    virtual string toString() = 0;


};

