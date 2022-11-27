#pragma once
#include <iostream>
#include <algorithm>

using namespace std;
using std::string;

class ElementBorderBound {

private:
    int element;

public:
    string face;

    ElementBorderBound(int _element, string _face);

    template <typename T>
    any getBorder(T elem);
};
