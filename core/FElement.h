#pragma once

#include "Nodes.h"
#include "Vector3R.h"
#include "Vector3.hpp"

#include <numeric>
#include <vector>
using namespace std;
using std::vector;

#include <Eigen/Core>
using namespace Eigen;

//--------------------------------------------------------------------//
// 要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
class FElement : public Nodes {

private:
    bool isShell;
    bool isBar;

public:
    double C1_3 = 1 / 3;
    double C1_6 = 1 / 6;
    double C1_12 = 1 / 12;
    double C1_24 = 1 / 24;
    
    // 三角形2次要素のガウス積分の積分点座標
    double GTRI2[2] = { 1 / 6, 2 / 3 };
    // 四面体2次要素のガウス積分の積分点座標
    double GTETRA2[2] = { 0.25 - 0.05 * sqrt(5), 0.25 + 0.15 * sqrt(5) };
    // 四角形1次要素のガウス積分の積分点座標
    double GX2[2] = { -1 / sqrt(3), 1 / sqrt(3) };
    // 四角形2次要素のガウス積分の積分点座標
    double GX3[3] = { -sqrt(0.6), 0, sqrt(0.6) };
    // ガウス積分の重み係数
    double GW3[3] = { 5 / 9, 8 / 9, 5 / 9 };

    int label;
    int material;

    FElement(int _label, int _material, vector<int> _nodes);

    // 積分点の剛性マトリックスを返す
    MatrixXd stiffPart(MatrixXd d, MatrixXd b, double coef);

    // 節点変位を1次元配列に変換する
    VectorXd toArray(vector<Vector3R> u, int dof);

    // 節点変位を局所座標系・1次元配列に 変換する
    VectorXd toLocalArray(vector<Vector3R> u, vector<vector<double>> d);

    double determinant(VectorXd ja);

    MatrixXd dirMatrix(vector<Vector3> p, VectorXd axis);

    vector<Vector3> dirVectors(vector<Vector3> p, Vector3 axis);

    Vector3 normalVector(vector<Vector3> p);

};