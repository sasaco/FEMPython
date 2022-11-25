#pragma once

//--------------------------------------------------------------------//
// FEM データモデル
#include <numbers>
#include <vector>
using namespace std;
using std::vector;


class FemDataModel {

private:
    double COEF_F_W = 0.5 / numbers::pi;    // f/ω比 1/2π
    /*
    vector<Material> materials;             // 材料
    vector<ShellParameter> shellParams;     // シェルパラメータ
    vector<BarParameter> barParams;         // 梁パラメータ
    vector<Coordinates> coordinates;	    // 局所座標系
    MeshModel mesh;		                    // メッシュモデル
    BoundaryCondition bc;	                // 境界条件
    Solver solver;		                    // 連立方程式求解オブジェクト
    Result result;		                    // 計算結果
    bool hasShellBar;                       // シェル要素または梁要素を含まない

    // Method
    void init();
    void reNumbering();
    */
public:

    FemDataModel();
    void clear();

};