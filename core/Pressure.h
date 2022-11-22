#pragma once
#include "ElementBorderBound.h";

#include <string>
#include <vector>
using namespace std;         //  ���O��Ԏw��


class Pressure : public ElementBorderBound {

private:


public:
    int element;
    double press;
    string face;

    Pressure(int _element, string _face, double _press);
    string toString(vector<T> elems);
};