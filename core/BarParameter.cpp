import "BarParameter.h";

#include <format>
using namespace std;         //  名前空間指定


//--------------------------------------------------------------------//
// 梁パラメータ（円形断面）
// label - パラメータ番号
// type - 断面種類
// ss - データ文字列
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

// 梁パラメータを表す文字列を返す
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
