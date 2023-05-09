#pragma once
#include "Material.h"
#include "SectionManager.h"

#include <string>
using namespace std;         //  名前空間指定
using std::string;

//--------------------------------------------------------------------//
// 矩形断面
// ss - データ文字列
class RectSection : public SectionManager {

private:
    const double PI = 3.14159265358979323846;   // pi

    double b1;  // 外側幅
    double h1;  // 外側高さ
    double b2;  // 内側幅
    double h2;  // 内側高さ

    // 断面積
    double area;

    // 断面２次モーメント
    double iy;
    double iz;


    double zy;
    double zz;

    // 断面２次極モーメント
    double ip;

    // 矩形断面の捩り係数を求める
    VectorXd rectCoef(double ba);

public:
    // 矩形断面のせん断補正係数
    const double KS_RECT = 5 / 6;
    // 矩形断面の捩り係数
    const double COEF_K1 = 64 / pow(PI, 5);
    const double COEF_K = 8 / pow(PI, 2);


    RectSection(double ss[4]);

    // せん断補正係数を返す
    double shearCoef() override;

    // 歪・応力ベクトルを返す
    MatrixXd strainStress(Material material, double ex, double thd, double kpy, double kpz,
        double sy, double sz) override;

    // 質量・重心周りの慣性モーメントを返す
    VectorXd massInertia(double dens, double l) override;

    // 断面を表す文字列を返す
    string toString() override;

};