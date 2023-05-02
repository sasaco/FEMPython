#pragma once
#include "BorderManager.hpp"
//#include "ElementManager.h"

#include <string>
#include <vector>
#include <algorithm>

using namespace std;
using std::string;
using std::vector;
;

class ElementBorderBound {

private:

public:
    string element;
    int elementIndex;
    string face;

    ElementBorderBound();
    ElementBorderBound(string _element, string _face);

    //BorderManager getBorder(ElementManager elem);

};

