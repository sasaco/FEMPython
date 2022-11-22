#pragma once
import "Material.h";

#include <string>
#include <numbers>
using namespace std;         //  名前空間指定



//--------------------------------------------------------------------//
// 円形断面
// ss - データ文字列
class CircleSection {

private:
    const double PI = numbers::pi;

    double d1;  // 外径
    double d2;  // 内径
    // 断面積
    double area;
    // 断面２次モーメント
    double iy;
    double iz;
    // 断面２次極モーメント
    double ip;

public:
    // 円形断面のせん断補正係数
    const double KS_CIRCLE = 6 / 7;

    CircleSection(double ss[2]);

    // せん断補正係数を返す
    double shearCoef();

    // 歪・応力ベクトルを返す
    void strainStress(Material material, double ex, double thd, double kpy, double kpz,
        double sy, double sz, double out[4][6]);

    // 質量・重心周りの慣性モーメントを返す
    void massInertia(double dens, double l, double out[4]);

    // 断面を表す文字列を返す
    string toString();
};
