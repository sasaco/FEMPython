#pragma once
#include "ElementBorderBound.h";

#include <string>
#include <vector>
using namespace std;         //  名前空間指定


class Pressure : public ElementBorderBound {

private:


public:
    int element;
    double press;
    string face;

    Pressure(int _element, string _face, double _press);

    template <typename T>
    string toString(vector<T> elems);
};