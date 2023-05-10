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

    // 断面積
    double area();

    // 断面２次モーメント
    double iy();
    double iz();

    // 断面２次極モーメント
    double ip();

    // せん断補正係数を返す
    double shearCoef();

    // 歪・応力ベクトルを返す
    MatrixXd strainStress(Material material, double ex, double thd, double kpy, double kpz,
        double sy, double sz);

    // 質量・重心周りの慣性モーメントを返す
    VectorXd massInertia(double dens, double l);

    // 断面を表す文字列を返す
    string toString();


};

