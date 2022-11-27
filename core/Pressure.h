#pragma once
#include "ElementBorderBound.h";

#include <vector>
using namespace std;         //  名前空間指定
using vector;

class Pressure : public ElementBorderBound {

private:


public:
    int element;
    double press;
    std::string face;

    Pressure(int _element, std::string _face, double _press);

    template <typename T>
    std::string toString(vector<T> elems);
};