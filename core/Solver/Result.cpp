#include "Result.h"

//--------------------------------------------------------------------//
// �v�Z����
Result::Result() {
    clear();
    type = NODE_DATA;
};

// �v�Z���ʂ���������
void Result::clear() {

    displacement.clear();
    dispMax = 0;
    angleMax = 0;
    strain1.clear();
    stress1.clear();
    sEnergy1.clear();
    strain2.clear();
    stress2.clear();
    sEnergy2.clear();
    temperature.clear();
    tempMax = 0;
    eigenValue.clear();
    calculated = false;
    value.clear();
    minValue = 0;
    maxValue = 0;
};



// �ߓ_���x��ݒ肷��
// bc - ���E����
// t - �ߓ_���x��\���x�N�g��
// nodeCount - �ߓ_��
void Result::setTemperature(BoundaryCondition bc, VectorXd t, vector<FENode> node) {

    temperature.clear();
    auto temp = bc.temperature;
    int ii = 0;
    for (int i = 0; i < node.size(); i++) {
        double tt = 0;
        if (bc.bcList[i] < 0) {
            tt = t(ii);
            ii++;
        }
        else {
            tt = temp[bc.bcList[i]].t;
        }
        tempMax = max(tempMax, tt);
        temperature.push_back(tt);
    }
    calculated = true;
}

// �ߓ_�ψʂ�ݒ肷��
// bc - ���E����
// disp - �ߓ_�ψʂ�\���x�N�g��
// nodeCount - �ߓ_��
void Result::setDisplacement(BoundaryCondition bc, VectorXd disp, vector<FENode> node) {
    
    displacement.clear();
    dispMax = 0;
    angleMax = 0;
    auto rests = bc.restraints;
    int ii = 0;
    for (int i = 0; i < node.size(); i++) {
        Vector3R v = Vector3R();
        string id = node[i].label;
        int i0 = bc.nodeIndex[id];
        auto bcDof = bc.dof[id];
        int r = -1;
        for (int j = 0; j < bcDof; j++) {
            int bcl = bc.bcList[i0 + j];
            if (bcl < 0) {
                v.x[j] = disp(ii);
                ii++;
            }
            else {
                r = (int)floor(bcl / 6);
                v.x[j] = rests[r].x[j];
            }
        }
        /*
        if ((r >= 0) && rests[r].coords) {
            v.x = rests[r].coords.toGlobal(x);
        }
        */
        dispMax = max(dispMax, v.magnitude());
        angleMax = max(angleMax, v.magnitudeR());
        displacement.push_back(v);
    }
    calculated = true;
}


// �ߓ_�c�E���͂�����������
// count - �ߓ_��
void Result::initStrainAndStress(int count) {
    strain1.clear();
    strain2.clear();
    stress1.clear();
    stress2.clear();
    sEnergy1.clear();
    sEnergy2.clear();
    VectorXd zeros = VectorXd::Zero(6);
    for (int i = 0; i < count; i++) {
        strain1.push_back(Strain(zeros));
        strain2.push_back(Strain(zeros));
        stress1.push_back(Stress(zeros));
        stress2.push_back(Stress(zeros));
        sEnergy1.push_back(0);
        sEnergy2.push_back(0);
    }
}



// �ߓ_�̍\����͌��ʂɒl��������
// i - �ߓ_�̃C���f�b�N�X
// eps1,str1,se1,eps2,str2,se2 - �\�ʁE���ʂ̘c�C���́C�c�G�l���M�[���x
void Result::addStructureData(int i, Strain eps1, Stress str1, double se1,
    Strain eps2, Stress str2, double se2) {
    strain1[i].add(eps1);
    stress1[i].add(str1);
    sEnergy1[i] += se1;
    strain2[i].add(eps2);
    stress2[i].add(str2);
    sEnergy2[i] += se2;
};


// �ߓ_�̍\����͌��ʂɒl���|����
// i - �ߓ_�̃C���f�b�N�X
// coef - �v�Z���ʂɊ|����W��
void Result::mulStructureData(int i, double coef) {
    strain1[i].mul(coef);
    stress1[i].mul(coef);
    sEnergy1[i] *= coef;
    strain2[i].mul(coef);
    stress2[i].mul(coef);
    sEnergy2[i] *= coef;
}

/*
// �ŗL�l�f�[�^��ǉ�����
// ev - �ŗL�l
Result.prototype.addEigenValue = function(ev) {
    this.eigenValue.push(ev);
    this.calculated = true;
};

// �R���^�[�}�f�[�^��ݒ肷��
// param - �f�[�^�̎��
// component - �f�[�^�̐���
// data - �R���^�[�}�Q�ƌ�
Result.prototype.setContour = function(param, component, data) {
    if (param < 0) return;
    data = data || this;
    var dpara = [data.displacement, data.strain1, data.stress1, data.sEnergy1,
        data.temperature];
    var count = dpara[param].length;
    if (count == = 0) return;
    this.value.length = 0;
    this.value[0] = data.getData(param, component, 0);
    this.minValue = this.value[0];
    this.maxValue = this.value[0];
    for (var i = 1; i < count; i++) {
        this.value[i] = data.getData(param, component, i);
        this.minValue = Math.min(this.minValue, this.value[i]);
        this.maxValue = Math.max(this.maxValue, this.value[i]);
    }
};

// �f�[�^�����o��
// param - �f�[�^�̎��
// component - �f�[�^�̐���
// index - �ߓ_�̃C���f�b�N�X
Result.prototype.getData = function(param, component, index) {
    switch (param) {
    case DISPLACEMENT:
        switch (component) {
        case X:
        case Y:
        case Z:
        case RX:
        case RY:
        case RZ:
            return this.displacement[index].x[component];
        case MAGNITUDE:
            return this.displacement[index].magnitude();
        }
        break;
    case STRAIN:
        if (component < SHIFT) {
            return this.getTensorComp(this.strain1[index], component);
        }
        else {
            return this.getTensorComp(this.strain2[index], component - SHIFT);
        }
        break;
    case STRESS:
        if (component < SHIFT) {
            return this.getTensorComp(this.stress1[index], component);
        }
        else {
            return this.getTensorComp(this.stress2[index], component - SHIFT);
        }
        break;
    case S_ENERGY:
        if (component == = 0) {
            return this.sEnergy1[index];
        }
        else {
            return this.sEnergy2[index];
        }
        break;
    case TEMPERATURE:
        return this.temperature[index];
    }
    return 0;
};

// �c�E���͂����o��
// s - �c or ����
// component - �f�[�^�̐���
Result.prototype.getTensorComp = function(s, component) {
    if (component < 6) {
        return s.vector()[component];
    }
    else if (component <= 10) {
        var pri = s.principal();
        if (component == = MAX_PRINCIPAL)      return pri[0];
        else if (component == = MIN_PRINCIPAL) return pri[2];
        else if (component == = MID_PRINCIPAL) return pri[1];
        else if (component == = MAX_SHARE)     return 0.5 * (pri[0] - pri[2]);
    }
    else if (component == = VON_MISES) {
        return s.mises();
    }
    return 0;
};

// �f�[�^�������Ԃ�
// nodes - �ߓ_
// elems - �v�f
Result.prototype.toStrings = function(nodes, elems) {
    var s = [], tuple, i;
    if (this.type == = ELEMENT_DATA) {
        s.push('ResultType\tElement');
        tuple = elems;
    }
    else {
        s.push('ResultType\tNode');
        tuple = nodes;
    }
    for (i = 0; i < this.displacement.length; i++) {
        s.push('Displacement\t' + nodes[i].label.toString(10) + '\t' +
            this.displacement[i].x.join('\t'));
    }
    for (i = 0; i < this.strain1.length; i++) {
        s.push('Strain1\t' + tuple[i].label.toString(10) + '\t' +
            this.strain1[i].vector().join('\t'));
    }
    for (i = 0; i < this.stress1.length; i++) {
        s.push('Stress1\t' + tuple[i].label.toString(10) + '\t' +
            this.stress1[i].vector().join('\t'));
    }
    for (i = 0; i < this.sEnergy1.length; i++) {
        s.push('StrEnergy1\t' + tuple[i].label.toString(10) + '\t' +
            this.sEnergy1[i]);
    }
    for (i = 0; i < this.strain2.length; i++) {
        s.push('Strain2\t' + tuple[i].label.toString(10) + '\t' +
            this.strain2[i].vector().join('\t'));
    }
    for (i = 0; i < this.stress2.length; i++) {
        s.push('Stress2\t' + tuple[i].label.toString(10) + '\t' +
            this.stress2[i].vector().join('\t'));
    }
    for (i = 0; i < this.sEnergy2.length; i++) {
        s.push('StrEnergy2\t' + tuple[i].label.toString(10) + '\t' +
            this.sEnergy2[i]);
    }
    for (i = 0; i < this.temperature.length; i++) {
        s.push('Temp\t' + nodes[i].label.toString(10) + '\t' +
            this.temperature[i]);
    }
    for (i = 0; i < this.eigenValue.length; i++) {
        Array.prototype.push.apply
        (s, this.eigenValue[i].toStrings(nodes, tuple));
    }
    return s;
};
*/