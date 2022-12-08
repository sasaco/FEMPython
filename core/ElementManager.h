#pragma once

//#include "BorderManager.hpp"
#include "ShellParameter.h"
#include "BarParameter.h"

#include <iostream>
#include <string>
#include <vector>
using namespace std;
using std::string;
using std::vector;

// -----------------------------------------------------------------------------
// BarElement
//#include "BEBarElement.h"
//#include "TBarElement.h"

// SolidElement
//#include "TetraElement1.h"
//#include "TetraElement2.h"
#include "HexaElement1.h"
//#include "HexaElement2.h"
//#include "HexaElement1WT.h"
//#include "WedgeElement1.h"
//#include "WedgeElement2.h"

// ShellElement
//#include "QuadElement1.h"
//#include "TriElement1.h"
// -----------------------------------------------------------------------------



class ElementManager {



private:
    string key;

    //// BarElement
    //BEBarElement _BEBarElement = NULL;
    //TBarElement  _TBarElement = NULL;
    //// SolidElement
    //TetraElement1 _TetraElement1 = NULL;
    //TetraElement2 _TetraElement2 = NULL;
    HexaElement1 _HexaElement1;
    //HexaElement2 _HexaElement2 = NULL;
    //HexaElement1WT _HexaElement1WT = NULL;
    //WedgeElement1 _WedgeElement1 = NULL;
    //WedgeElement2 _WedgeElement2 = NULL;
    //// ShellElement
    //QuadElement1 _QuadElement1 = NULL;
    //TriElement1 _TriElement1 = NULL;
    
public:
    ElementManager(string key, vector<string> columns);

    int label();
    vector<int> nodes();

    int material();
    void setMaterial(int mat);

    int param();
    void setParam(int param);

    bool isShell();
    bool isBar();

    int nodeCount();

    MatrixXd gradMatrix(vector<FENode>nodes, double h, ShellParameter sp);
    MatrixXd gradMatrix(vector<FENode>nodes, double h, BarParameter sp);
    MatrixXd gradMatrix(vector<FENode>nodes, double h);
    /*
    BorderManager border(int element, int index);
    BorderManager borderEdge(int element, int index);
    */

};

