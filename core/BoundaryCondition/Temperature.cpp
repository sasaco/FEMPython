#include "Temperature.h"

#include <format>

//--------------------------------------------------------------------//
// ���x�Œ����
// node - �ߓ_���x��
// t - ���x
Temperature::Temperature(int _node, double _t) {
    node = _node;
    t = _t;
};

// ���x�Œ������\���������Ԃ�
// nodes - �ߓ_
string Temperature::toString(vector<FENode> nodes) {
    return format("Temperature\t{}\t{}", nodes[node].label, t);
};