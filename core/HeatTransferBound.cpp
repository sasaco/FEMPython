#include "HeatTransferBound.h";

#include <format>

//--------------------------------------------------------------------//
// �M�`�B���E����
// element - �v�f���x��
// face - �v�f���E��
// htc - �M�`�B��
// outTemp - �O�����x
HeatTransferBound::HeatTransferBound(int _element, string face, double _htc, double _outTemp) 
    : ElementBorderBound(_element, _face){
    htc = _htc;
    outTemp = _outTemp;
}

// �M�`�B���E������\���������Ԃ�
// elems - �v�f
string HeatTransferBound::toString(vecor<T> elems) {

    return format("HTC\t{}\t{}\t{}\t{}\t{}", elems[element].label, face, htc, outTemp);

};