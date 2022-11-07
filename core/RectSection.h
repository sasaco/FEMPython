#pragma once
import "Material.h";

#include <string>
#include <numbers>
using namespace std;         //  名前空間指定


//--------------------------------------------------------------------//
// 矩形断面
// ss - データ文字列
class RectSection {

private:
    const double PI = numbers::pi;

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
    void rectCoef(double ba, double out[4]);

public:
    // 矩形断面のせん断補正係数
    const double KS_RECT = 5 / 6;
    // 矩形断面の捩り係数
    const double COEF_K1 = 64 / pow(numbers::pi, 5);
    const double COEF_K = 8 / pow(numbers::pi, 2);


    RectSection(double ss[4]);


    // せん断補正係数を返す
    double shearCoef();

    // 歪・応力ベクトルを返す
    void strainStress(Material material, double ex, double thd, double kpy, double kpz,
        double sy, double sz, double out[4][6]);

    // 質量・重心周りの慣性モーメントを返す
    void massInertia(double dens, double l, double out[4]);

    string toString();
};