#pragma once
#include "MeshModel.h"
#include "Material.h"
#include "ShellParameter.h"
#include "BarParameter.h"
#include "BoundaryCondition.h"
#include "Solver.h"
#include "Result.h"


//--------------------------------------------------------------------//
// FEM データモデル
#include <format>
#include <numbers>
#include <vector>
using namespace std;
using std::vector;


class FemDataModel {

private:
    double COEF_F_W = 0.5 / numbers::pi;    // f/ω比 1/2π


    bool hasShellBar;                       // シェル要素または梁要素を含まない
    Solver solver;		                    // 連立方程式求解オブジェクト
    Result result;		                    // 計算結果

    // Method
    void reNumbering();

    void resetNodes(vector<int> map, ElementManager s);
    int resetNodePointer(vector<int> map, int node);
    int resetElementPointer(vector<int> map, int element);

    void resetMaterialLabel();
    void resetParameterLabel();
    void resetCoordinates();

    void resetCoordinatesPointer(vector<Coordinates> map, Restraint bc);
    void resetCoordinatesPointer(vector<Coordinates> map, Load bc);


public:
    MeshModel mesh;		                    // メッシュモデル
    BoundaryCondition bc;	                // 境界条件
    vector<Material> materials;             // 材料
    vector<ShellParameter> shellParams;     // シェルパラメータ
    vector<BarParameter> barParams;         // 梁パラメータ
    vector<Coordinates> coordinates;	    // 局所座標系

    FemDataModel();

    void clear();
    void init();

};