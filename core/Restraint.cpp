#include "Restraint.h";

#include <format>

//--------------------------------------------------------------------//
// �S������
// node - �ߓ_���x��
// coords - �Ǐ����W�n
// restx,resty,restz - x,y,z�����̍S���̗L��
// x,y,z - �����ψʂ�x,y,z����
// restrx,restry,restrz - x,y,z�����̉�]�S���̗L��
// rx,ry,rz - �����ψʂ�x,y,z�������]�p
Restraint::Restraint(int _node, Coordinates _coords,
    bool restx, bool resty, bool restz,
    double x, double y, double z,
    bool restrx, bool restry, bool restrz, 
    double rx, double ry, double rz) : Vector3R(x, y, z, rx, ry, rz) {
    ;
    node = _node;
    coords = _coords;

    rest[0] = restx; 
    rest[1] = resty;
    rest[2] = restz;
    rest[3] = restrx;
    rest[4] = restry;
    rest[5] = restrz;

    globalX = x;
};

// �S��������\���������Ԃ�
// nodes - �ߓ_
string Restraint::toString(vector<FENode> nodes) {

    string s =  format("Restraint\t{}", nodes[node].label);

    for (int i = 0; i < 6; i++) {
        if (rest[i]) {
            s += '\t1\t' + x[i];
        }
        else {
            s += '\t0\t' + x[i];
        }
    }
    if (coords) {
        s += '\t' + coords.label;
    }
    return s;


};