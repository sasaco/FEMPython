import "BarParameter.h";

#include <format>
using namespace std;         //  ���O��Ԏw��


//--------------------------------------------------------------------//
// ���p�����[�^�i�~�`�f�ʁj
// label - �p�����[�^�ԍ�
// type - �f�ʎ��
// ss - �f�[�^������
BarParameter::BarParameter(int _label, string _type, double *ss) {
    
    label = _label;
    type = _type;

    transform(type.begin(), type.end(), type.begin(), tolower);

    if (type == "circle") {
        circleSection = new CircleSection(ss);
    }
    else if (type == "rectangle") {
        rectSection = new RectSection(ss);
    }
}

BarParameter::~BarParameter() {
    delete circleSection;
    delete rectSection;
}

// ���p�����[�^��\���������Ԃ�
string BarParameter::toString() {

    string result;

    if (type == "circle") {
        result = format("BarParameter\t{}\t{}\t{}",
            label, type, circleSection.toString());
    }
    else if (type == "rectangle") {
        result = format("BarParameter\t{}\t{}\t{}",
            label, type, rectSection.toString());
    }

    return result;

}
