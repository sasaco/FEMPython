#pragma once

//#include <format>
#include <string>
using namespace std;         //  名前空間指定
using std::string;

#include<Eigen/Core>
using namespace Eigen;

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
    double KS_RECT = 5 / 6;

    double nu;
    double dens;
    double hCon;
    double sHeat;
    double cv;	

    //void matrix2Dstrain(double out[3][3]);
    //void matrixAxiSymetric(double out[4][4]);
    //void matrixTorsion(double out[2][2]);

public:
    int label;

    double ee;
    //double nu;
    //double dens;
    //double hCon;
    //double sHeat;
    //double cv;	// 体積比熱
    double gg;    // 横弾性係数

    Eigen::Matrix<double, 3, 3> m2d;
    Eigen::Matrix<double, 5, 5> msh;
    Eigen::Matrix<double, 6, 6> m3d;

    Material(int _label, double _ee, double _nu, double _dens, double _hCon, double _sHeat);
    Material(const Material& mat);

    void matrix2Dstress();
    void matrixShell();
    void matrix3D();

    string toString();

};