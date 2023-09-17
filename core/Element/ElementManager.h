//#pragma once

//#include "BorderManager.hpp"
#include "ShellParameter.h"
#include "BarParameter.h"
#include "SectionManager.h"

#include <iostream>
#include <string>
#include <vector>
using namespace std;
using std::string;
using std::vector;

// -----------------------------------------------------------------------------
// BarElement
#include "BEBarElement.h"
#include "TBarElement.h"

// SolidElement
#include "TetraElement1.h"
#include "TetraElement2.h"
#include "HexaElement1.h"
#include "HexaElement2.h"
#include "HexaElement1WT.h"
#include "WedgeElement1.h"
#include "WedgeElement2.h"

// ShellElement
#include "QuadElement1.h"
#include "TriElement1.h"
// -----------------------------------------------------------------------------



class ElementManager {



private:
    string key;

    // BarElement
    BEBarElement _BEBarElement;
    TBarElement  _TBarElement;
    // SolidElement
    TetraElement1 _TetraElement1;
    TetraElement2 _TetraElement2;
    HexaElement1 _HexaElement1;
    HexaElement2 _HexaElement2;
    HexaElement1WT _HexaElement1WT;
    WedgeElement1 _WedgeElement1;
    WedgeElement2 _WedgeElement2;
    // ShellElement
    QuadElement1 _QuadElement1;
    TriElement1 _TriElement1;
    
public:
    ElementManager();
    ElementManager(string key, vector<string> columns);

    string label();

    vector<string> nodes();
    void setNodes(vector<string> tmp);

    vector<int> nodeIndexs; // �ߓ_�̍����}�g���N�X��̃C���f�b�N�X
    void setIndexs(vector<int> tmp);

    string material();
    int materialIndex;         // �����}�g���N�X��̃C���f�b�N�X
    void setMaterial(string mat);
    void setMaterialIndex(int mat);

    string param();
    void setParam(string param);
    int paramIndex;         // �����}�g���N�X��̃C���f�b�N�X
    void setParamIndex(int param);

    bool isShell();
    bool isBar();

    int nodeCount();

    MatrixXd gradMatrix(vector<FENode>nodes, double h, ShellParameter sp);
    MatrixXd gradMatrix(vector<FENode>nodes, double h, SectionManager sp);
    MatrixXd gradMatrix(vector<FENode>nodes, double h);

    MatrixXd stiffnessMatrix(vector<FENode> p, MatrixXd d1, ShellParameter sp);
    MatrixXd stiffnessMatrix(vector<FENode> p, Material d1, SectionManager sp);
    MatrixXd stiffnessMatrix(vector<FENode> p, MatrixXd d1);


    tuple<Strain, Stress, double, Strain, Stress, double>
        elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);
    tuple<Strain, Stress, double, Strain, Stress, double>
        elementStrainStress(vector<FENode> p, vector<Vector3R> u, Material material, SectionManager sect);
    tuple<Strain, Stress, double> 
        elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1);


    tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
        strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp);
    tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
        strainStress(vector<FENode> p, vector<Vector3R> u, Material material, SectionManager sect);
    tuple<vector<Strain>, vector<Stress>, vector<double>>
        strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1);

    VectorXd angle(vector<FENode> p);

    /*
    BorderManager border(int element, int index);
    BorderManager borderEdge(int element, int index);
    */

    string getName();

};
