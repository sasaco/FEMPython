#pragma once
class Result
{

private:
    /*
    // �f�[�^�^
    var NONE = -1;		// ��f�[�^
    var DISPLACEMENT = 0;	// �ψ�
    var STRAIN = 1;		// �c
    var STRESS = 2;		// ����
    var S_ENERGY = 3;		// �c�G�l���M�[���x
    var TEMPERATURE = 4;	// ���x
    // ����
    var X = 0;		// x����
    var Y = 1;		// y����
    var Z = 2;		// z����
    var RX = 3;		// x����]����
    var RY = 4;		// y����]����
    var RZ = 5;		// z����]����
    var XY = 3;		// xy����f����
    var YZ = 4;		// yz����f����
    var ZX = 5;		// zx����f����
    var MAGNITUDE = 6;	// �傫��
    var MAX_PRINCIPAL = 7;	// �ő�听��
    var MIN_PRINCIPAL = 8;	// �ŏ��听��
    var MID_PRINCIPAL = 9;	// ���Ԏ听��
    var MAX_SHARE = 10;	// �ő傹��f����
    var VON_MISES = 11;	// �~�[�[�X����
    var SHIFT = 12;		// �����V�t�g��
    // �ψʂ̐���
    var DISP_COMPONENT = ['Mag.', 'x', 'y', 'z'];
    var DISP2_COMPONENT = ['Mag.', 'x', 'y', 'z', 'rotx', 'roty', 'rotz'];
    // �c�̐���
    var STRAIN_COMPONENT = ['Max.prin.', 'Min.prin.', 'Mid.prin.',
        'Max.share',
        'x', 'y', 'z', 'xy', 'yz', 'zx'];
    // ���͂̐���
    var STRESS_COMPONENT = ['Max.prin.', 'Min.prin.', 'Mid.prin.',
        'Max.share', 'Von mises',
        'x', 'y', 'z', 'xy', 'yz', 'zx'];
    // �c�G�l���M�[���x�̐���
    var ENERGY_COMPONENT = ['Energy'];
    var COMP_MAP = { 'Mag.':MAGNITUDE,'x' : X,'y' : Y,'z' : Z,
                  'rotx' : RX,'roty' : RY,'rotz' : RZ,'xy' : XY,'yz' : YZ,'zx' : ZX,
                  'Max.prin.' : MAX_PRINCIPAL,'Min.prin.' : MIN_PRINCIPAL,
                  'Mid.prin.' : MID_PRINCIPAL,'Max.share' : MAX_SHARE,
                  'Von mises' : VON_MISES,'Energy' : 0,
                  'x 1' : X,'y 1' : Y,'z 1' : Z,'xy 1' : XY,'yz 1' : YZ,'zx 1' : ZX,
                  'Max.prin. 1' : MAX_PRINCIPAL,'Min.prin. 1' : MIN_PRINCIPAL,
                  'Mid.prin. 1' : MID_PRINCIPAL,'Max.share 1' : MAX_SHARE,
                  'Von mises 1' : VON_MISES,'Energy 1' : 0,
                  'x 2' : X + SHIFT,'y 2' : Y + SHIFT,'z 2' : Z + SHIFT,
                  'xy 2' : XY + SHIFT,'yz 2' : YZ + SHIFT,'zx 2' : ZX + SHIFT,
                  'Max.prin. 2' : MAX_PRINCIPAL + SHIFT,
                  'Min.prin. 2' : MIN_PRINCIPAL + SHIFT,
                  'Mid.prin. 2' : MID_PRINCIPAL + SHIFT,
                  'Max.share 2' : MAX_SHARE + SHIFT,
                  'Von mises 2' : VON_MISES + SHIFT,'Energy 2' : 1 };
    var EIG_EPS = 1e-10;		// �ŗL�l�v�Z�̎���臒l
    var NODE_DATA = 0;		// �ߓ_�f�[�^
    var ELEMENT_DATA = 1;		// �v�f�f�[�^
    var VIBRATION = 'Vibration';	// �U�����
    var BUCKLING = 'Buckling';	// �������
    */

public:
    Result();
    void clear();
};

