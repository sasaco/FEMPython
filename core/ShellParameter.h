#pragma once
#include <string>
using namespace std;         //  ���O��Ԏw��


//--------------------------------------------------------------------//
// �V�F���p�����[�^
// label - �p�����[�^�ԍ�
// thickness - ����
class ShellParameter {

private:
    int label;
    double thickness;

public:
    ShellParameter(int _label, double _thickness);

    string toString();
};