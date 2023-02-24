#pragma once
#include "MeshModel.h"
#include "Material.h"
#include "ShellParameter.h"
#include "BarParameter.h"
#include "BoundaryCondition.h"
#include "Result.h"
#include "FENode.h"


#include <format>
#include <numbers>
#include <vector>
#include <map>
using namespace std;
using std::vector;


//--------------------------------------------------------------------//
// FEM データモデル
class FemDataModel {

private:
    double COEF_F_W = 0.5 / numbers::pi;    // f/ω比 1/2π


    bool hasShellBar;                       // シェル要素または梁要素を含まない
    // Solver solver;		                    // 連立方程式求解オブジェクト

    // Method
    void reNumbering();

    void resetNodes(map<int, int> map, ElementManager &s);
    void resetNodePointer(map<int, int> map, int &node);
    void resetElementPointer(map<int, int> map, int &element);

    void resetMaterialLabel();
    void resetParameterLabel();
    void resetCoordinates();

    template <typename T>
    void resetCoordinatesPointer(map<int, Coordinates> map, T &bc);


public:
    Result result;		                    // 計算結果
    MeshModel mesh;		                    // メッシュモデル
    BoundaryCondition bc;	                // 境界条件
    vector<Material> materials;             // 材料
    vector<ShellParameter> shellParams;     // シェルパラメータ
    vector<BarParameter> barParams;         // 梁パラメータ
    vector<Coordinates> coordinates;	    // 局所座標系

    FemDataModel();

    void clear();
    void init();
    int setNodeDoF();
    // void calculate();
    void calculateElementStress();
    void calculateNodeStress();

};