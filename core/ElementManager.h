#pragma once

//#include "BorderManager.hpp"

#include <string>
#include <vector>
using namespace std;
using std::string;
using std::vector;

// -----------------------------------------------------------------------------
// 循環参照になるため下記のように書いて .h ファイルの include は .cpp ファイルで行う
// https://cpp-lang.sevendays-study.com/ex-day2.html

// BarElement
class BEBarElement;
class TBarElement;

// SolidElement
class TetraElement1;
class TetraElement2;
class HexaElement1;
class HexaElement2;
class HexaElement1WT;
class WedgeElement1;
class WedgeElement2;

// ShellElement
class QuadElement1;
class TriElement1;


// -----------------------------------------------------------------------------



class ElementManager {



private:
    string key;

    // BarElement
    BEBarElement* pBEBarElement = NULL;
    TBarElement*  pTBarElement = NULL;
    // SolidElement
    TetraElement1* pTetraElement1 = NULL;
    TetraElement2* pTetraElement2 = NULL;
    HexaElement1* pHexaElement1 = NULL;
    HexaElement2* pHexaElement2 = NULL;
    HexaElement1WT* pHexaElement1WT = NULL;
    WedgeElement1* pWedgeElement1 = NULL;
    WedgeElement2* pWedgeElement2 = NULL;
    // ShellElement
    QuadElement1* pQuadElement1 = NULL;
    TriElement1* pTriElement1 = NULL;
    
public:
    
    ElementManager(string key, vector<string> columns);
    ~ElementManager();


    int label();

    /*
    vector<int> nodes;

    BorderManager border(int element, int index);
    BorderManager borderEdge(int element, int index);
    */

};

