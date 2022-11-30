#pragma once
#include <string>
using namespace std;         //  名前空間指定


//--------------------------------------------------------------------//
// シェルパラメータ
// label - パラメータ番号
// thickness - 厚さ
class ShellParameter {

private:

public:
    int label;
    double thickness;

    ShellParameter(int _label, double _thickness);

    string toString();
};