#pragma once
#include "RectSection.h";
#include "CircleSection.h";




//--------------------------------------------------------------------//
// 梁パラメータ（円形断面）
// label - パラメータ番号
// type - 断面種類
// ss - データ文字列
class BarParameter {

private:
    int label;
    string type;
    RectSection* rectSection;
    CircleSection* circleSection;

public:
    BarParameter(int label, string type, double* ss);
    ~BarParameter();

    string toString();
};