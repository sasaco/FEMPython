#include "Vector3R.h";
#include "Coordinates.h";

class Load : public Vector3R {

private:
    double globalX;

public:
    int node;
    Coordinates coords;

    Load(int _node, Coordinates _coords, double x, double y, double z, double rx, double ry, double rz);


};


//--------------------------------------------------------------------//
// �׏d����
// node - �ߓ_���x��
// coords - �Ǐ����W�n
// x,y,z - x,y,z����
// rx,ry,rz - x,y,z�������]����
Load::Load(int _node, Coordinates _coords, double x, double y, double z, double rx, double ry, double rz)
    : Vector3R(x, y, z, rx, ry, rz){

    node = node;
    coords = _coords;
    globalX = x;

};

// �׏d������\���������Ԃ�
// nodes - �ߓ_
Load.prototype.toString = function(nodes) {
    var s = 'Load\t' + nodes[this.node].label.toString(10) + '\t' +
        this.x.join('\t');
    if (this.coords) {
        s += '\t' + this.coords.label.toString(10);
    }
    return s;
};
