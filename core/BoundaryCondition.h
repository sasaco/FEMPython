#pragma once
#include "Temperature.h";
#include "HeatTransferBound.h";

#include <vector>
#include <algorithm>
using namespace std;
using std::vector;

//--------------------------------------------------------------------//
// 境界条件
class BoundaryCondition {

private:
    vector<Restraint> restraints;	    // 拘束条件
    vector<Load> loads;                 // 荷重条件
    vector<Pressure> pressures;         // 面圧条件
    vector<Temperature> temperature;    // 節点温度条件
    vector<HeatTransferBound> htcs;	    // 熱伝達境界条件
    double loadMax;		// 最大荷重
    double pressMax;		// 最大面圧
    vector<int> dof;			// 節点の自由度
    vector<int> nodeIndex;		// 荷重ベクトルの節点ポインタ
    vector<int> bcList;		// 境界条件を設定した節点のリスト

    template <typename T>
    void compareNodeLabel(T target);

    template <typename T>
    void compareElementLabel(T target);

public:
    BoundaryCondition();

    void clear();
    void init();
    int setPointerStructure(int count);
    int setPointerHeat(int count);
    double getRestDisp(int bc);

    vector<string> toStrings(vector<FENode> nodes, int elems);

};