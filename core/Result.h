#pragma once

#include "Vector3R.h"
#include "Strain.h"
#include "Stress.h"
#include "EigenValue.h"

#include <map> 
#include<string>
#include<vector>
using namespace std;
using std::string;
using std::vector;

class Result {

private:

    // �f�[�^�^
    const int NONE = -1;		    // ��f�[�^
    const int DISPLACEMENT = 0;	    // �ψ�
    const int STRAIN = 1;		    // �c
    const int STRESS = 2;		    // ����
    const int S_ENERGY = 3;		    // �c�G�l���M�[���x
    const int TEMPERATURE = 4;	    // ���x

    // ����
    const int X = 0;		        // x����
    const int Y = 1;		        // y����
    const int Z = 2;		        // z����
    const int RX = 3;		        // x����]����
    const int RY = 4;		        // y����]����
    const int RZ = 5;		        // z����]����
    const int XY = 3;		        // xy����f����
    const int YZ = 4;		        // yz����f����
    const int ZX = 5;		        // zx����f����
    const int MAGNITUDE = 6;	    // �傫��
    const int MAX_PRINCIPAL = 7;	// �ő�听��
    const int MIN_PRINCIPAL = 8;	// �ŏ��听��
    const int MID_PRINCIPAL = 9;	// ���Ԏ听��
    const int MAX_SHARE = 10;	    // �ő傹��f����
    const int VON_MISES = 11;	    // �~�[�[�X����
    const int SHIFT = 12;		    // �����V�t�g��

    // �ψʂ̐���
    const string DISP_COMPONENT[4] = { "Mag.", "x", "y", "z" };

    const string DISP2_COMPONENT[7] = { "Mag.", "x", "y", "z", "rotx", "roty", "rotz" };
    // �c�̐���
    const string STRAIN_COMPONENT[10] = { "Max.prin.", "Min.prin.", "Mid.prin.", "Max.share",
                                        "x", "y", "z", "xy", "yz", "zx" };
    // ���͂̐���
    const string STRESS_COMPONENT[11] = { "Max.prin.", "Min.prin.", "Mid.prin.",
                                        "Max.share", "Von mises",
                                        "x", "y", "z", "xy", "yz", "zx" };
    // �c�G�l���M�[���x�̐���
    const string  ENERGY_COMPONENT[1] = {"Energy"};

    map<string, int> COMP_MAP { 
        {"Mag." , MAGNITUDE},
        {"x" , X},
        {"y" , Y},
        {"z" , Z},
        {"rotx" , RX},
        {"roty" , RY},
        {"rotz" , RZ},
        {"xy" , XY},
        {"yz" , YZ},
        {"zx" , ZX},
        {"Max.prin." , MAX_PRINCIPAL},
        {"Min.prin." , MIN_PRINCIPAL},
        {"Mid.prin." , MID_PRINCIPAL},
        {"Max.share" , MAX_SHARE},
        {"Von mises" , VON_MISES},
        {"Energy" , 0},
        {"x 1" , X},
        {"y 1" , Y},
        {"z 1" , Z},
        {"xy 1" , XY},
        {"yz 1" , YZ},
        {"zx 1" , ZX},
        {"Max.prin. 1" , MAX_PRINCIPAL},
        {"Min.prin. 1" , MIN_PRINCIPAL},
        {"Mid.prin. 1" , MID_PRINCIPAL},
        {"Max.share 1" , MAX_SHARE},
        {"Von mises 1" , VON_MISES},
        {"Energy 1" , 0},
        {"x 2" , X + SHIFT},
        {"y 2" , Y + SHIFT},
        {"z 2" , Z + SHIFT},
        {"xy 2" , XY + SHIFT},
        {"yz 2" , YZ + SHIFT},
        {"zx 2" , ZX + SHIFT},
        {"Max.prin. 2" , MAX_PRINCIPAL + SHIFT},
        {"Min.prin. 2" , MIN_PRINCIPAL + SHIFT},
        {"Mid.prin. 2" , MID_PRINCIPAL + SHIFT},
        {"Max.share 2" , MAX_SHARE + SHIFT},
        {"Von mises 2" , VON_MISES + SHIFT},
        {"Energy 2" , 1}
    };

    const double EIG_EPS = 1e-10;           // �ŗL�l�v�Z�̎���臒l
    const int NODE_DATA = 0;		        // �ߓ_�f�[�^
    const int ELEMENT_DATA = 1;		        // �v�f�f�[�^
    const string VIBRATION = "Vibration";	// �U�����
    const string BUCKLING = "Buckling";	    // �������

    
    vector<Vector3R> displacement;  // �ψ�
    double dispMax;		            // �ψʂ̑傫���̍ő�l
    double angleMax;		        // ��]�p�̑傫���̍ő�l
    vector<Strain> strain1;		    // �ߓ_�c
    vector<Stress> stress1;		    // �ߓ_����
    vector<double> sEnergy1;		// �ߓ_�c�G�l���M�[���x
    vector<Strain> strain2;
    vector<Stress> stress2;
    vector<double> sEnergy2;
    vector<double> temperature;		// �ߓ_���x
    double tempMax;		            // ���x�̍ő�l
    vector<EigenValue> eigenValue;	// �ŗL�l�f�[�^
    bool calculated;	            // �v�Z�O���v�Z���ʖ���
    int type = NODE_DATA;		    // �f�[�^�ێ��`�ԁF�ߓ_�f�[�^
    vector<double> value;		    // �R���^�[�}�f�[�^
    double minValue;		        // �R���^�[�}�f�[�^�ŏ��l
    double maxValue;		        // �R���^�[�}�f�[�^�ő�l

public:
    Result();
    void clear();
};

