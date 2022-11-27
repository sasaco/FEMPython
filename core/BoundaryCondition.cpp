#include "Restraint.h";
#include "Load.h";
#include "Pressure.h";
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

BoundaryCondition::BoundaryCondition() {
    clear();
};

// データを消去する
void BoundaryCondition::clear() {
    restraints.clear();		// 拘束条件
    loads.clear();		    // 荷重条件
    pressures.clear();		// 面圧条件
    temperature.clear();    // 節点温度条件
    htcs.clear();			// 熱伝達境界条件
    loadMax = 0;		    // 最大荷重
    pressMax = 0;		    // 最大面圧
    dof.clear();			// 節点の自由度
    nodeIndex.clear();		// 荷重ベクトルの節点ポインタ
    bcList.clear();		    // 境界条件を設定した節点のリスト
};

// 境界条件を初期化する
void BoundaryCondition::init() {

    compareNodeLabel(restraints);
    compareNodeLabel(loads);
    compareElementLabel(pressures);
    compareNodeLabel(temperature);
    compareElementLabel(htcs);

    loadMax = 0;
    pressMax = 0;

    for (int i = 0; i < loads.size(); i++) {
        loadMax = max(loadMax, loads[i].magnitude());
    }
    for (int i = 0; i < pressures.size(); i++) {
        pressMax = max(pressMax, pressures[i].press);
    }

}


// 構造解析の節点ポインタを設定する
// count - 節点数
int BoundaryCondition::setPointerStructure(int count) {
    nodeIndex.clear();
    bcList.clear();
    int dofAll = 0;
    for (int i = 0; i < count; i++) {
        nodeIndex[i] = dofAll;
        dofAll += dof[i];
    }
    for (int i = 0; i < dofAll; i++) {
        bcList[i] = -1;
    }
    for (int i = 0; i < restraints.size(); i++) {
        Restraint r = restraints[i];
        int index0 = nodeIndex[r.node];
        int rdof = dof[r.node];
        for (int j = 0; j < rdof; j++) {
            if (r.rest[j]) bcList[index0 + j] = 6 * i + j;
        }
    }
    return dofAll;
};

// 熱解析の節点ポインタを設定する
// count - 節点数
int BoundaryCondition::setPointerHeat(int count) {
    dof.clear();
    nodeIndex.clear();
    bcList.clear();
    int temps = temperature.size();
    for (int i = 0; i < count; i++) {
        bcList[i] = -1;
    }
    for (int i = 0; i < temps; i++) {
        Temperature t = temperature[i];
        bcList[t.node] = i;
    }
    return temps;
}

// 強制変位を返す
// bc - 変位自由度ポインタ
double BoundaryCondition::getRestDisp(int bc) {
    int i = round(bc / 6);
    Restraint r = restraints[i];
    int j = bc % 6;
    double result = r.x[j];
    return result;
};

// データ文字列を返す
// nodes - 節点
// elems - 要素
vector<string> BoundaryCondition::toStrings(vector<FENode> nodes, vector<any> elems) {

    vector<string> s;

    for (int i = 0; i < restraints.size(); i++) {
        s.push_back(restraints[i].toString(nodes));
    }
    for (int i = 0; i < loads.size(); i++) {
        s.push_back(loads[i].toString(nodes));
    }
    for (int i = 0; i < pressures.size(); i++) {
        s.push_back(pressures[i].toString(elems));
    }
    for (int i = 0; i < temperature.size(); i++) {
        s.push_back(temperature[i].toString(nodes));
    }
    for (int i = 0; i < htcs.size(); i++) {
        s.push_back(htcs[i].toString(elems));
    }
    return s;
};


// 節点ラベルを比較する
// bc1,bc2 - 比較する境界条件
template <typename T>
void BoundaryCondition::compareNodeLabel(T target) {

    sort(target.begin(), target.end(),
        [](auto bc1, auto bc2) -> int {
            if (bc1.node < bc2.node)        return -1;
            else if (bc1.node > bc2.node)   return 1;
            else                            return 0;
        });

}

// 要素ラベルを比較する
// bc1,bc2 - 比較する境界条件
template <typename T>
void BoundaryCondition::compareElementLabel(T target) {

    sort(target.begin(), target.end(),
        [](auto bc1, auto bc2) -> int {
            if (bc1.element < bc2.element)      return -1;
            else if (bc1.element > bc2.element) return 1;
            else                                return 0;
        });
}
