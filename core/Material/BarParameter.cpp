#include "BarParameter.h"

//--------------------------------------------------------------------//
// 梁パラメータ（円形断面）
// label - パラメータ番号
// type - 断面種類
// ss - データ文字列
BarParameter::BarParameter() {
    label = "";
}
BarParameter::BarParameter(string _label, string _type, vector<double> ss) {
    
    label = _label;

    string type = _type;

    std::transform(type.begin(), type.end(), type.begin(), ::tolower);

    _Section = SectionManager(type, ss);
}

SectionManager BarParameter::section() {
    return _Section;
}

// 梁パラメータを表す文字列を返す
string BarParameter::toString() {

    //string result;

    //if (type == "circle") {
    //    result = format("BarParameter\t{}\t{}\t{}",
    //        label, type, circleSection->toString());
    //}
    //else if (type == "rectangle") {
    //    result = format("BarParameter\t{}\t{}\t{}",
    //        label, type, rectSection->toString());
    //}

    //return result;

    return "BarParamater";

}

