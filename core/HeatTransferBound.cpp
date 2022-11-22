#include "HeatTransferBound.h";

#include <format>

//--------------------------------------------------------------------//
// 熱伝達境界条件
// element - 要素ラベル
// face - 要素境界面
// htc - 熱伝達率
// outTemp - 外部温度
HeatTransferBound::HeatTransferBound(int _element, string face, double _htc, double _outTemp) 
    : ElementBorderBound(_element, _face){
    htc = _htc;
    outTemp = _outTemp;
}

// 熱伝達境界条件を表す文字列を返す
// elems - 要素
string HeatTransferBound::toString(vecor<T> elems) {

    return format("HTC\t{}\t{}\t{}\t{}\t{}", elems[element].label, face, htc, outTemp);

};