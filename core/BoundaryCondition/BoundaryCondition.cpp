#include "BoundaryCondition.h"

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

    compareNodeLabel<Restraint>(restraints);
    compareNodeLabel<Load>(loads);
    compareElementLabel<Pressure>(pressures);
    compareNodeLabel<Temperature>(temperature);
    compareElementLabel<HeatTransferBound>(htcs);

    loadMax = 0;
    pressMax = 0;

    for (unsigned int i = 0; i < loads.size(); i++) {
        loadMax = max(loadMax, loads[i].magnitude());
    }
    for (unsigned int i = 0; i < pressures.size(); i++) {
        pressMax = max(pressMax, pressures[i].press);
    }

}


// 構造解析の節点ポインタを設定する
// count - 節点数
int BoundaryCondition::setPointerStructure(vector<FENode> nodes) {
    nodeIndex.clear();
    bcList.clear();

    int dofAll = 0;
    for (int i = 0; i < nodes.size(); i++) {
        FENode node = nodes[i];
        nodeIndex[node.label] = dofAll;
        dofAll += dof[node.label];
    }
    for (int i = 0; i < dofAll; i++) {
        bcList.push_back(-1);
    }
    for (unsigned int i = 0; i < restraints.size(); i++) {
        Restraint r = restraints[i];
        int index0 = nodeIndex[r.node];
        int rdof = dof[r.node];
        for (int j = 0; j < rdof; j++) {
            if (r.rest[j]) {
                bcList[index0 + j] = 6 * i + j;
            }
        }
    }
    return dofAll;
}

// 熱解析の節点ポインタを設定する
// count - 節点数
int BoundaryCondition::setPointerHeat(int count) {
    dof.clear();
    nodeIndex.clear();
    bcList.clear();
    int temps = (int)temperature.size();
    for (int i = 0; i < count; i++) {
        bcList[i] = -1;
    }
    for (int i = 0; i < temps; i++) {
        Temperature t = temperature[i];
        bcList[t.nodeIndex] = i;
    }
    return temps;
}

// 強制変位を返す
// bc - 変位自由度ポインタ
double BoundaryCondition::getRestDisp(int bc) {
    int i = (int)round(bc / 6);
    Restraint r = restraints[i];
    int j = bc % 6;
    double result = r.x[j];
    return result;
};

// データ文字列を返す
// nodes - 節点
// elems - 要素
//vector<string> BoundaryCondition::toStrings(vector<FENode> nodes, vector<ElementManager> elems) {
//
//    vector<string> s;
//
//    for (int i = 0; i < restraints.size(); i++) {
//        s.push_back(restraints[i].toString(nodes));
//    }
//    for (int i = 0; i < loads.size(); i++) {
//        s.push_back(loads[i].toString(nodes));
//    }
//    for (int i = 0; i < pressures.size(); i++) {
//        s.push_back(pressures[i].toString(elems));
//    }
//    for (int i = 0; i < temperature.size(); i++) {
//        s.push_back(temperature[i].toString(nodes));
//    }
//    for (int i = 0; i < htcs.size(); i++) {
//        s.push_back(htcs[i].toString(elems));
//    }
//    return s;
//};


// 節点ラベルを比較する
// bc1,bc2 - 比較する境界条件
template <typename T> 
void BoundaryCondition::compareNodeLabel(vector<T> target) {
   
    sort(target.begin(), target.end(),
        [](const auto& bc1, const auto& bc2)
        {
            return bc1.node < bc2.node;
        });

}

// 要素ラベルを比較する
// bc1,bc2 - 比較する境界条件
template <typename T>
void BoundaryCondition::compareElementLabel(vector<T> target) {

    sort(target.begin(), target.end(),
        [](const auto& bc1, const auto& bc2)
        {
            return bc1.element < bc2.element;
        });

}
