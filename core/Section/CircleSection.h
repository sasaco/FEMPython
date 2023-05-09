#pragma once

#include "Material.h"

#include <string>
//#include <fmt/core.h>
using namespace std;         //  名前空間指定
using std::string;
#include <cmath>

//--------------------------------------------------------------------//
// 円形断面
// ss - データ文字列
class CircleSection {

private:
    double PI = 3.14159265358979323846;   // pi

    double d1;  // 外径
    double d2;  // 内径

public:
    // 円形断面のせん断補正係数
    double KS_CIRCLE = 6 / 7;

    // 断面積
    double area;
    // 断面２次モーメント
    double iy;
    double iz;
    // 断面２次極モーメント
    double ip;

    CircleSection();
    CircleSection(vector<double> ss);

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
