#include "Restraint.h";
#include "Load.h";
#include "Pressure.h";
#include "Temperature.h";
#include "HeatTransferBound.h";

//--------------------------------------------------------------------//
// ���E����
class BoundaryCondition {

private:
    vector<Restraint> restraints;	    // �S������
    vector<Load> loads;                 // �׏d����
    vector<Pressure> pressures;         // �ʈ�����
    vector<Temperature> temperature;    // �ߓ_���x����
    vector<HeatTransferBound> htcs;	    // �M�`�B���E����
    double loadMax;		// �ő�׏d
    double pressMax;		// �ő�ʈ�
    vector<int> dof;			// �ߓ_�̎��R�x
    vector<int> nodeIndex;		// �׏d�x�N�g���̐ߓ_�|�C���^
    vector<int> bcList;		// ���E������ݒ肵���ߓ_�̃��X�g

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

// �f�[�^����������
void BoundaryCondition::clear() {
    restraints.clear();		// �S������
    loads.clear();		    // �׏d����
    pressures.clear();		// �ʈ�����
    temperature.clear();    // �ߓ_���x����
    htcs.clear();			// �M�`�B���E����
    loadMax = 0;		    // �ő�׏d
    pressMax = 0;		    // �ő�ʈ�
    dof.clear();			// �ߓ_�̎��R�x
    nodeIndex.clear();		// �׏d�x�N�g���̐ߓ_�|�C���^
    bcList.clear();		    // ���E������ݒ肵���ߓ_�̃��X�g
};

// ���E����������������
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

// �\����͂̐ߓ_�|�C���^��ݒ肷��
// count - �ߓ_��
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

// �M��͂̐ߓ_�|�C���^��ݒ肷��
// count - �ߓ_��
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

// �����ψʂ�Ԃ�
// bc - �ψʎ��R�x�|�C���^
double BoundaryCondition::getRestDisp(int bc) {
    int i = round(bc / 6);
    Restraint r = restraints[i];
    int j = bc % 6;
    double result = r.x[j];
    return result;
};

// �f�[�^�������Ԃ�
// nodes - �ߓ_
// elems - �v�f
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

// �ߓ_���x�����r����
// bc1,bc2 - ��r���鋫�E����
function compareNodeLabel(bc1, bc2) {
    if (bc1.node < bc2.node)      return -1;
    else if (bc1.node > bc2.node) return 1;
    else                       return 0;
}

// �v�f���x�����r����
// bc1,bc2 - ��r���鋫�E����
function compareElementLabel(bc1, bc2) {
    if (bc1.element < bc2.element)      return -1;
    else if (bc1.element > bc2.element) return 1;
    else                             return 0;
}



//--------------------------------------------------------------------//
// �v�f���E����
// element - �v�f���x��
// face - �v�f���E��
var ElementBorderBound = function(element, face) {
    this.element = element;
    this.face = face;
};

// �v�f���E��Ԃ�
// elem - �v�f
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
// �׏d����
// node - �ߓ_���x��
// coords - �Ǐ����W�n
// x,y,z - x,y,z����
// rx,ry,rz - x,y,z�������]����
var Load = function(node, coords, x, y, z, rx, ry, rz) {
    Vector3R.call(this, x, y, z, rx, ry, rz);
    this.node = node;
    this.coords = coords;
    this.globalX = this.x;
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

