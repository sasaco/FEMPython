#include "ElementBorderBound.h"

//--------------------------------------------------------------------//
// �v�f���E����
// element - �v�f���x��
// face - �v�f���E��
ElementBorderBound::ElementBorderBound(int _element, string _face) {
    element = _element;
    face = _face;
};


// �v�f���E��Ԃ�
// elem - �v�f
template <typename T>
any ElementBorderBound::getBorder(T elem) {

    if (face.length() == = 2) {
        if (face.substr(0, 1) == = 'F') {

            char s = face.substr(1, 1);
            int i = atoi(s);
            j = i - 1;

            return elem.border(element, j);
        }
        else if (face.substr(0, 1) == = 'E') {

            char s = face.substr(1, 1);
            int i = atoi(s);
            j = i - 1;

            return elem.borderEdge(element, j);
        }
    }
    return null;
};

