//#pragma once

//#include "BorderManager.hpp"
#include "ShellParameter.h"
#include "BarParameter.h"
#include "Section.h"

#include <iostream>
#include <string>
#include <vector>
using namespace std;
using std::string;
using std::vector;

// -----------------------------------------------------------------------------
// BarElement
#include "SEBarElement.h"
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
    SEBarElement _SEBarElement;
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
    ElementManager();
    ElementManager(string key, vector<string> columns);

    string label();

    vector<string> nodes();
    void setNodes(vector<string> tmp);

    vector<int> nodeIndexs; // 節点の合成マトリクス上のインデックス
    void setIndexs(vector<int> tmp);

    string material();
    int materialIndex;         // 合成マトリクス上のインデックス
    void setMaterial(string mat);
    void setMaterialIndex(int mat);

    string param();
    void setParam(string param);
    int paramIndex;         // 合成マトリクス上のインデックス
    void setParamIndex(int param);

    bool isShell();
    bool isBar();

    int nodeCount();

    MatrixXd gradMatrix(vector<FENode>nodes, double h, ShellParameter sp);
    MatrixXd gradMatrix(vector<FENode>nodes, double h, Section sp);
    MatrixXd gradMatrix(vector<FENode>nodes, double h);

    MatrixXd stiffnessMatrix(vector<FENode> p, MatrixXd d1, ShellParameter sp);
    MatrixXd stiffnessMatrix(vector<FENode> p, Material d1, Section sp);
    MatrixXd stiffnessMatrix(vector<FENode> p, MatrixXd d1);


    tuple<Strain, Stress, double, Strain, Stress, double>
        elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);
    tuple<Strain, Stress, double, Strain, Stress, double>
        elementStrainStress(vector<FENode> p, vector<Vector3R> u, Material material, Section sect);
    tuple<Strain, Stress, double> 
        elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1);


    tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
        strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);
    tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
        strainStress(vector<FENode> p, vector<Vector3R> u, Material material, Section sect);
    tuple<vector<Strain>, vector<Stress>, vector<double>>
        strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1);

    VectorXd angle(vector<FENode> p);

    /*
    BorderManager border(int element, int index);
    BorderManager borderEdge(int element, int index);
    */

    string getName();

};

