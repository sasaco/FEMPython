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
// 荷重条件
// node - 節点ラベル
// coords - 局所座標系
// x,y,z - x,y,z成分
// rx,ry,rz - x,y,z軸周り回転成分
Load::Load(int _node, Coordinates _coords, double x, double y, double z, double rx, double ry, double rz)
    : Vector3R(x, y, z, rx, ry, rz){

    node = node;
    coords = _coords;
    globalX = x;

};

// 荷重条件を表す文字列を返す
// nodes - 節点
Load.prototype.toString = function(nodes) {
    var s = 'Load\t' + nodes[this.node].label.toString(10) + '\t' +
        this.x.join('\t');
    if (this.coords) {
        s += '\t' + this.coords.label.toString(10);
    }
    return s;
};
