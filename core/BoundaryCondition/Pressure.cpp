﻿#include "Pressure.h"


//--------------------------------------------------------------------//
// 面圧条件
// element - 要素ラベル
// face - 要素境界面
// press - 面圧
Pressure::Pressure() :
    ElementBorderBound() {
    press = 0;
}
Pressure::Pressure(string _element, string _face, double _press) :
    ElementBorderBound(_element, _face) {
    press = _press;
}

// 面圧条件を表す文字列を返す
// elems - 要素
//string Pressure::toString(vector<ElementManager> elems) {
//    return format("Pressure\t{}\t{}\t{}", elems[element].label(), face, press);
//};