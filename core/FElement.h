#pragma once
#include "Material.h";
#include "Nodes.h";
#include "BoundaryCondition.h";

#include <vector>
using std::vector;


//--------------------------------------------------------------------//
// 要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
class FElement : public Nodes {

private:
    const double C1_3 = 1 / 3;
    const double C1_6 = 1 / 6;
    const double C1_12 = 1 / 12;
    const double C1_24 = 1 / 24;

    // 三角形2次要素のガウス積分の積分点座標
    const double GTRI2[2] = { 1 / 6, 2 / 3 };
    // 四面体2次要素のガウス積分の積分点座標
    const double GTETRA2[2] = { 0.25 - 0.05 * sqrt(5), 0.25 + 0.15 * sqrt(5) };
    // 四角形1次要素のガウス積分の積分点座標
    const double GX2[2] = { -1 / sqrt(3), 1 / sqrt(3) };
    // 四角形2次要素のガウス積分の積分点座標
    const double GX3[3] = { -sqrt(0.6), 0, sqrt(0.6) };
    // ガウス積分の重み係数
    const double GW3[3] = { 5 / 9, 8 / 9, 5 / 9 };

    int label;
    Material material;
    bool isShell;
    bool isBar;

public:
    FElement(int _label, Material _material, vector<int> _nodes);

    // 積分点の剛性マトリックスを返す
    void stiffPart(vector<vector<double>> d, vector<vector<double>> b, double coef, vector<vector<double>> k);

    // 節点変位を1次元配列に変換する
    void toArray(vector<BoundaryCondition> u, int dof, vector<double> out);

    // 節点変位を局所座標系・1次元配列に変換する
    void toLocalArray(vector<BoundaryCondition> u, vector<vector<double>>d, vector<double> v);
};