#include "ElementBorderBound.h"

//--------------------------------------------------------------------//
// �v�f���E����
// element - �v�f���x��
// face - �v�f���E��
ElementBorderBound::ElementBorderBound(int _element, string _face) {
    element = _element;
    face = _face;
};


//// �v�f���E��Ԃ�
//// elem - �v�f
//BorderManager ElementBorderBound::getBorder(ElementManager elem) {
//
//    if (face.length() == 2) {
//        if (face.substr(0, 1) == "F") {
//
//            string s = face.substr(1, 1);
//            int i = stoi(s);
//            int j = i - 1;
//
//            return BorderManager();
//            // return elem.border(element, j);
//        }
//        else if (face.substr(0, 1) ==  "E") {
//
//            string s = face.substr(1, 1);
//            int i = stoi(s);
//            int j = i - 1;
//
//            return BorderManager();
//            // return elem.borderEdge(element, j);
//        }
//    }
//
//    return BorderManager();
//};

