﻿#pragma once

#include "ElementBorderBound.h"

#include <string>
//#include <fmt/core.h>
#include <vector>
using namespace std;         //  名前空間指定
using std::string;
using std::vector;

class Pressure : public ElementBorderBound {

private:


public:
    double press;

    Pressure();
    Pressure(string _element, std::string _face, double _press);
    //string toString(vector<ElementManager> elems);
};