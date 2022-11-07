#include "ShellParameter.h"

#include <format>


//--------------------------------------------------------------------//
// �V�F���p�����[�^
// label - �p�����[�^�ԍ�
// thickness - ����
ShellParameter::ShellParameter(int _label, double _thickness) {
    label = _label;
    thickness = _thickness;
};

// �V�F���p�����[�^��\���������Ԃ�
string ShellParameter::toString() {

    return format("ShellParameter\t{}\t{}",
        label, thickness);

};

