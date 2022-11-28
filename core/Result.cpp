#include "Result.h"

//--------------------------------------------------------------------//
// �v�Z����
var Result = function() {
    this.displacement = [];		// �ψ�
    this.dispMax = 0;		// �ψʂ̑傫���̍ő�l
    this.angleMax = 0;		// ��]�p�̑傫���̍ő�l
    this.strain1 = [];		// �ߓ_�c
    this.stress1 = [];		// �ߓ_����
    this.sEnergy1 = [];		// �ߓ_�c�G�l���M�[���x
    this.strain2 = [];
    this.stress2 = [];
    this.sEnergy2 = [];
    this.temperature = [];		// �ߓ_���x
    this.tempMax = 0;		// ���x�̍ő�l
    this.eigenValue = [];		// �ŗL�l�f�[�^
    this.calculated = false;	// �v�Z�O���v�Z���ʖ���
    this.type = NODE_DATA;		// �f�[�^�ێ��`�ԁF�ߓ_�f�[�^
    this.value = [];		// �R���^�[�}�f�[�^
    this.minValue = 0;		// �R���^�[�}�f�[�^�ŏ��l
    this.maxValue = 0;		// �R���^�[�}�f�[�^�ő�l
};

// �v�Z���ʂ���������
Result.prototype.clear = function() {
    this.displacement.length = 0;
    this.strain1.length = 0;
    this.strain2.length = 0;
    this.stress1.length = 0;
    this.stress2.length = 0;
    this.sEnergy1.length = 0;
    this.sEnergy2.length = 0;
    this.temperature.length = 0;
    this.dispMax = 0;
    this.angleMax = 0;
    this.tempMax = 0;
    this.eigenValue.length = 0;
    this.calculated = false;
    this.value.length = 0;
    this.minValue = 0;
    this.maxValue = 0;
};

// �ߓ_�ψʂ�ݒ肷��
// bc - ���E����
// disp - �ߓ_�ψʂ�\���x�N�g��
// nodeCount - �ߓ_��
Result.prototype.setDisplacement = function(bc, disp, nodeCount) {
    this.displacement.length = 0;
    this.dispMax = 0;
    this.angleMax = 0;
    var rests = bc.restraints, ii = 0;
    for (var i = 0; i < nodeCount; i++) {
        var v = new Vector3R(), i0 = bc.nodeIndex[i], bcDof = bc.dof[i], r = -1, x = v.x;
        for (var j = 0; j < bcDof; j++) {
            var bcl = bc.bcList[i0 + j];
            if (bcl < 0) {
                x[j] = disp[ii];
                ii++;
            }
            else {
                r = parseInt(bcl / 6);
                x[j] = rests[r].x[j];
            }
        }
        if ((r >= 0) && rests[r].coords) {
            v.x = rests[r].coords.toGlobal(x);
        }
        this.dispMax = Math.max(this.dispMax, v.magnitude());
        this.angleMax = Math.max(this.angleMax, v.magnitudeR());
        this.displacement.push(v);
    }
    this.calculated = true;
};

// �ߓ_���x��ݒ肷��
// bc - ���E����
// t - �ߓ_���x��\���x�N�g��
// nodeCount - �ߓ_��
Result.prototype.setTemperature = function(bc, t, nodeCount) {
    this.temperature.length = 0;
    var temp = bc.temperature, ii = 0;
    for (var i = 0; i < nodeCount; i++) {
        var tt;
        if (bc.bcList[i] < 0) {
            tt = t[ii];
            ii++;
        }
        else {
            tt = temp[bc.bcList[i]].t;
        }
        this.tempMax = Math.max(this.tempMax, tt);
        this.temperature.push(tt);
    }
    this.calculated = true;
};

// �ߓ_�̍\����͌��ʂɒl��������
// i - �ߓ_�̃C���f�b�N�X
// eps1,str1,se1,eps2,str2,se2 - �\�ʁE���ʂ̘c�C���́C�c�G�l���M�[���x
Result.prototype.addStructureData = function(i, eps1, str1, se1,
    eps2, str2, se2) {
    this.strain1[i].add(eps1);
    this.stress1[i].add(str1);
    this.sEnergy1[i] += se1;
    this.strain2[i].add(eps2);
    this.stress2[i].add(str2);
    this.sEnergy2[i] += se2;
};

// �ߓ_�̍\����͌��ʂɒl���|����
// i - �ߓ_�̃C���f�b�N�X
// coef - �v�Z���ʂɊ|����W��
Result.prototype.mulStructureData = function(i, coef) {
    this.strain1[i].mul(coef);
    this.stress1[i].mul(coef);
    this.sEnergy1[i] *= coef;
    this.strain2[i].mul(coef);
    this.stress2[i].mul(coef);
    this.sEnergy2[i] *= coef;
};

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

// �ߓ_�c�E���͂�����������
// count - �ߓ_��
Result.prototype.initStrainAndStress = function(count) {
    this.strain1.length = 0;
    this.strain2.length = 0;
    this.stress1.length = 0;
    this.stress2.length = 0;
    this.sEnergy1.length = 0;
    this.sEnergy2.length = 0;
    var zeros = [0, 0, 0, 0, 0, 0];
    for (var i = 0; i < count; i++) {
        this.strain1[i] = new Strain(zeros);
        this.strain2[i] = new Strain(zeros);
        this.stress1[i] = new Stress(zeros);
        this.stress2[i] = new Stress(zeros);
        this.sEnergy1[i] = 0;
        this.sEnergy2[i] = 0;
    }
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
