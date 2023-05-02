#include "HeatTransferBound.h"

//--------------------------------------------------------------------//
// 熱伝達境界条件
// element - 要素ラベル
// face - 要素境界面
// htc - 熱伝達率
// outTemp - 外部温度
HeatTransferBound::HeatTransferBound()
    : ElementBorderBound() {
    htc = 0;
    outTemp = 0;
}
HeatTransferBound::HeatTransferBound(string element, string face, double _htc, double _outTemp)
    : ElementBorderBound(element, face){
    htc = _htc;
    outTemp = _outTemp;
}


// 熱伝達境界条件を表す文字列を返す
// elems - 要素
//string HeatTransferBound::toString(vector<ElementManager> elems) {
//
//    return format("HTC\t{}\t{}\t{}\t{}", 
//        elems[element].label(), face, htc, outTemp);
//
//};