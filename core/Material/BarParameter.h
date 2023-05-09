#pragma once
#include "RectSection.h"
#include "CircleSection.h"
#include "SectionManager.h"

#include <algorithm>
#include <string>
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
    string label;

    BarParameter();
    BarParameter(string label, string type, double* ss);
    ~BarParameter();

    SectionManager section();
    string toString();
};