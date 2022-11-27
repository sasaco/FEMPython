#pragma once
#include <string>
#include <any>
#include <vector>
#include <algorithm>
using namespace std;
using std::vector;

class ElementBorderBound {

private:
    int element;
    string face;

public:
    ElementBorderBound(int _element, string _face);

    template <typename T>
    any getBorder(T elem);
};
