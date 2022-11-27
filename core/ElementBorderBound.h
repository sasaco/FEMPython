#pragma once
#include "BorderManager.hpp"
#include "ElementManager.hpp"

#include <string>
#include <vector>
#include <algorithm>

using namespace std;
using std::string;
using std::vector;
;

class ElementBorderBound {

private:
    int element;

public:
    string face;

    ElementBorderBound(int _element, string _face);

    BorderManager getBorder(ElementManager elem);

};

