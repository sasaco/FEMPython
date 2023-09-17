﻿#include "ShellParameter.h"


//--------------------------------------------------------------------//
// シェルパラメータ
// label - パラメータ番号
// thickness - 厚さ
ShellParameter::ShellParameter() {
    label = "";
    thickness = 0;
};
ShellParameter::ShellParameter(string _label, double _thickness) {
    label = _label;
    thickness = _thickness;
};

// シェルパラメータを表す文字列を返す
string ShellParameter::toString() {

    //return format("ShellParameter\t{}\t{}",
    //    label, thickness);

    return "ShellParamater";

};
