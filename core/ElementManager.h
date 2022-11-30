#pragma once

#include "BorderManager.hpp"
#include "HexaElement1.h"

#include <vector>
#include <stdexcept>
using namespace std;
using std::vector;


class ElementManager {

private:
    string key;

    HexaElement1 MyHexaElement1;



public:
    ElementManager(string key, vector<string> columns);
    
    int label();
    vector<int> nodes;


    BorderManager border(int element, int index);
    BorderManager borderEdge(int element, int index);

};

