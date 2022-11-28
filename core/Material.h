#pragma once

#include <format>
#include <string>
using namespace std;         //  名前空間指定
using std::string;


//--------------------------------------------------------------------//
// 材料
// label - 材料番号
// ee - ヤング率 (縦弾性係数) 
// nu - ポアソン比
// dens - 密度
// hCon - 熱伝導率
// sHeat - 比熱
class Material {

private:
    // 矩形断面のせん断補正係数
    const double KS_RECT = 5 / 6;

    int label;
    double nu;
    double dens;
    double hCon;
    double sHeat;
    double cv;	

    //void matrix2Dstrain(double out[3][3]);
    //void matrixAxiSymetric(double out[4][4]);
    //void matrixTorsion(double out[2][2]);

public:
    Material(int _label, double _ee, double _nu, double _dens, double _hCon, double _sHeat);

    double ee;
    //double nu;
    //double dens;
    //double hCon;
    //double sHeat;
    //double cv;	// 体積比熱
    double gg;    // 横弾性係数


    double m2d[3][3]; // set from matrix2Dstress
    void matrix2Dstress();

    double msh[5][5]; // set from matrixShell
    void matrixShell();

    void matrix3D();
    double m3d[6][6]; // set from matrix3D

    string toString();

};