#include "Temperature.h"

//#include <format>

//--------------------------------------------------------------------//
// 温度固定条件
// node - 節点ラベル
// t - 温度
Temperature::Temperature() {
    node = -1;
    t = 0;
};
Temperature::Temperature(int _node, double _t) {
    node = _node;
    t = _t;
};

// 温度固定条件を表す文字列を返す
// nodes - 節点
string Temperature::toString(vector<FENode> nodes) {
    //return format("Temperature\t{}\t{}", nodes[node].label, t);

    return "Temperature";
};