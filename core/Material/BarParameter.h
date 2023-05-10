#pragma once
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
    SectionManager _Section;

public:
    string label;

    BarParameter();
    BarParameter(string _label, string _type, vector<double> ss);

    SectionManager section();
    string toString();
};