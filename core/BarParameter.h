#pragma once
#include "RectSection.h"
#include "CircleSection.h"
#include "Section.h"

#include <algorithm>
#include <string>
//#include <format>
using namespace std;
using std::string;

//--------------------------------------------------------------------//
// 梁パラメータ（円形断面）
// label - パラメータ番号
// type - 断面種類
// ss - データ文字列
class BarParameter {

private:
    string type;
    RectSection* rectSection;
    CircleSection* circleSection;

public:
    int label;

    BarParameter(int label, string type, double* ss);
    ~BarParameter();

    Section section();
    string toString();
};