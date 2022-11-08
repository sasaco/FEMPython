#include "Pressure.h";

#include <format>


//--------------------------------------------------------------------//
// �ʈ�����
// element - �v�f���x��
// face - �v�f���E��
// press - �ʈ�
Pressure::Pressure(int _element, string _face, double _press) : ElementBorderBound(_element, _face) {

    element = _element;
    press = _press;
    face = _face;

};

// �ʈ�������\���������Ԃ�
// elems - �v�f
string Pressure::toString(vector<T> elems) {
    return format("Pressure\t{}\t{}\t{}", elems[element].label, face press);
};