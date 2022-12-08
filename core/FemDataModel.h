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
#include <map>
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

    void resetNodes(map<int, int> map, ElementManager &s);
    void resetNodePointer(map<int, int> map, int &node);
    void resetElementPointer(map<int, int> map, int &element);

    void resetMaterialLabel();
    void resetParameterLabel();
    void resetCoordinates();

    void resetCoordinatesPointer(map<int, Coordinates> map, Restraint &bc);
    void resetCoordinatesPointer(map<int, Coordinates> map, Load &bc);


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
    void calculate();

};