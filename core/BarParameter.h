#pragma once
#include <string>
using namespace std;         //  ���O��Ԏw��


import "RectSection.h";
import "CircleSection.h";

//--------------------------------------------------------------------//
// ���p�����[�^�i�~�`�f�ʁj
// label - �p�����[�^�ԍ�
// type - �f�ʎ��
// ss - �f�[�^������
class BarParameter {

private:
    int label;
    string type;
    RectSection rectSection;
    CircleSection circleSection;

public:
    BarParameter(int label, string type, double* ss);
    ~BarParameter();

    string toString();
};