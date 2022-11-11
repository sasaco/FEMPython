#include "Restraint.h";
#include "Load.h";
#include "Pressure.h";
#include "Temperature.h";
#include "HeatTransferBound.h";

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

public:
    BoundaryCondition();

    void clear();
    void init();
    int setPointerStructure(int count);
    int setPointerHeat(int count);
    double getRestDisp(int bc);

    void toStrings(vector<FENode> nodes, int elems, vector<string> s);

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
    restraints.sort(compareNodeLabel);
    loads.sort(compareNodeLabel);
    pressures.sort(compareElementLabel);
    temperature.sort(compareNodeLabel);
    htcs.sort(compareElementLabel);
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
void BoundaryCondition::toStrings(vector<FENode> nodes, int elems, vector<string> s) {

    for (int i = 0; i < restraints.size(); i++) {
        s.push_back(restraints[i].toString(nodes));
    }
    for (int i = 0; i < loads.size(); i++) {
        s.push_back(loads[i].toString(nodes));
    }
    for (i = 0; i < this.pressures.length; i++) {
        s.push(this.pressures[i].toString(elems));
    }
    for (i = 0; i < this.temperature.length; i++) {
        s.push(this.temperature[i].toString(nodes));
    }
    for (i = 0; i < this.htcs.length; i++) {
        s.push(this.htcs[i].toString(elems));
    }
    return s;
};

// 節点ラベルを比較する
// bc1,bc2 - 比較する境界条件
function compareNodeLabel(bc1, bc2) {
    if (bc1.node < bc2.node)      return -1;
    else if (bc1.node > bc2.node) return 1;
    else                       return 0;
}

// 要素ラベルを比較する
// bc1,bc2 - 比較する境界条件
function compareElementLabel(bc1, bc2) {
    if (bc1.element < bc2.element)      return -1;
    else if (bc1.element > bc2.element) return 1;
    else                             return 0;
}



//--------------------------------------------------------------------//
// 要素境界条件
// element - 要素ラベル
// face - 要素境界面
var ElementBorderBound = function(element, face) {
    this.element = element;
    this.face = face;
};

// 要素境界を返す
// elem - 要素
ElementBorderBound.prototype.getBorder = function(elem) {
    if (this.face.length == = 2) {
        var j;
        if (this.face.charAt(0) == = 'F') {
            j = parseInt(this.face.charAt(1)) - 1;
            return elem.border(this.element, j);
        }
        else if (this.face.charAt(0) == = 'E') {
            j = parseInt(this.face.charAt(1)) - 1;
            return elem.borderEdge(this.element, j);
        }
    }
    return null;
};



//--------------------------------------------------------------------//
// 荷重条件
// node - 節点ラベル
// coords - 局所座標系
// x,y,z - x,y,z成分
// rx,ry,rz - x,y,z軸周り回転成分
var Load = function(node, coords, x, y, z, rx, ry, rz) {
    Vector3R.call(this, x, y, z, rx, ry, rz);
    this.node = node;
    this.coords = coords;
    this.globalX = this.x;
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

