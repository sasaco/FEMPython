#include "ElementManager.h"


ElementManager::ElementManager(string _key, vector<string> columns) {

    if (_key == "BEBarElement") {
        //_BEBarElement = new BEBarElement();
    }
    else if (_key == "TBarElement") {
        //_TBarElement = new TBarElement();;
    }
    else if (_key == "TetraElement1") {
        //_TetraElement1 = new TetraElement1();
    }
    else if (_key == "TetraElement2") {
        //_TetraElement2 = new TetraElement2();
    }
    else if (_key == "HexaElement1") {
        int label = stoi(columns[1]);
        int material = stoi(columns[2]);
        vector<int> nodes;
        for (int i = 3; i < 11; i++) {
            int no = stoi(columns[i]);
            nodes.push_back(no);
        }
        _HexaElement1 = HexaElement1(label, material, nodes);
    }
    else if (_key == "HexaElement2") {
        //_HexaElement2 new HexaElement2();
    }
    else if (_key == "HexaElement1WT") {
        //_HexaElement1WT = new HexaElement1WT();
    }
    else if (_key == "WedgeElement1") {
        //_WedgeElement1 = new WedgeElement1();
    }
    else if (_key == "WedgeElement2") {
        //_WedgeElement2 = new WedgeElement2();
    }
    else if (key == "QuadElement1") {
        //_QuadElement1.nodes = new QuadElement1();
    }
    else if (key == "TriElement1") {
        //_TriElement1.nodes = new TriElement1();
    }
    key = _key;

}



vector<int> ElementManager::nodes() {

    if (key == "BEBarElement") {
        //return __BEBarElement.nodes;
    }
    else if (key == "TBarElement") {
        //return _TBarElement.nodes;
    }
    else if (key == "TetraElement1") {
        //return _TetraElement1.nodes;
    }
    else if (key == "TetraElement2") {
        //return _TetraElement2.nodes;
    }
    else if (key == "HexaElement1") {
        return _HexaElement1.nodes;
    }
    else if (key == "HexaElement2") {
        //return _HexaElement2.nodes;
    }
    else if (key == "HexaElement1WT") {
        //return _HexaElement1WT.nodes;
    }
    else if (key == "WedgeElement1") {
        //return _WedgeElement1.nodes;
    }
    else if (key == "WedgeElement2") {
        //return _WedgeElement2.nodes;
    }
    else if (key == "QuadElement1") {
        //return _QuadElement1.nodes;
    }
    else if (key == "TriElement1") {
        //return _TriElement1.nodes;
    }

    vector<int> empty;
    return empty;
}

int ElementManager::label() {
    if (key == "BEBarElement") {
        //return __BEBarElement.label;
    }
    else if (key == "TBarElement") {
        //return _TBarElement.label;
    }
    else if (key == "TetraElement1") {
        //return _TetraElement1.label;
    }
    else if (key == "TetraElement2") {
        //return _TetraElement2.label;
    }
    else if (key == "HexaElement1") {
        return _HexaElement1.label;
    }
    else if (key == "HexaElement2") {
        //return _HexaElement2.label;
    }
    else if (key == "HexaElement1WT") {
        //return _HexaElement1WT.label;
    }
    else if (key == "WedgeElement1") {
        //return _WedgeElement1.label;
    }
    else if (key == "WedgeElement2") {
        //return _WedgeElement2.label;
    }
    else if (key == "QuadElement1") {
        //return _QuadElement1.label;
    }
    else if (key == "TriElement1") {
        //return _TriElement1.label;
    }
    return -1;
}

int ElementManager::material() {
    if (key == "BEBarElement") {
        //return __BEBarElement.material;
    }
    else if (key == "TBarElement") {
        //return _TBarElement.material;
    }
    else if (key == "TetraElement1") {
        //return _TetraElement1.material;
    }
    else if (key == "TetraElement2") {
        //return _TetraElement2.material;
    }
    else if (key == "HexaElement1") {
        return _HexaElement1.material;
    }
    else if (key == "HexaElement2") {
        //return _HexaElement2.material;
    }
    else if (key == "HexaElement1WT") {
        //return _HexaElement1WT.material;
    }
    else if (key == "WedgeElement1") {
        //return _WedgeElement1.material;
    }
    else if (key == "WedgeElement2") {
        //return _WedgeElement2.material;
    }
    else if (key == "QuadElement1") {
        //return _QuadElement1.material;
    }
    else if (key == "TriElement1") {
        //return _TriElement1.material;
    }
    return -1;
}

void ElementManager::setMaterial(int mat) {
    if (key == "BEBarElement") {
        //_BEBarElement.material = mat;
    }
    else if (key == "TBarElement") {
        //_TBarElement.material = mat;
    }
    else if (key == "TetraElement1") {
        //_TetraElement1.material = mat;
    }
    else if (key == "TetraElement2") {
        //_TetraElement2.material =mat;
    }
    else if (key == "HexaElement1") {
        _HexaElement1.material = mat;
    }
    else if (key == "HexaElement2") {
        //_HexaElement2.material = mat;
    }
    else if (key == "HexaElement1WT") {
        //_HexaElement1WT.material = mat;
    }
    else if (key == "WedgeElement1") {
        //_WedgeElement1.material = mat;
    }
    else if (key == "WedgeElement2") {
        //_WedgeElement2.material = mat;
    }
    else if (key == "QuadElement1") {
        //_QuadElement1.material = mat;
    }
    else if (key == "TriElement1") {
        //_TriElement1.material = mat;
    }

}
int ElementManager::param() {
    if (key == "BEBarElement") {
        //return __BEBarElement.param;
    }
    else if (key == "TBarElement") {
        //return _TBarElement.param;
    }
    else if (key == "QuadElement1") {
        //return _QuadElement1.param;
    }
    else if (key == "TriElement1") {
        //return _TriElement1.param;
    }
    return -1;
}

void ElementManager::setParam(int param) {
    if (key == "BEBarElement") {
        //_BEBarElement.param = param;
    }
    else if (key == "TBarElement") {
        //_TBarElement.param = param;
    }
    else if (key == "QuadElement1") {
        //_QuadElement1.param = param;
    }
    else if (key == "TriElement1") {
        //_TriElement1.param = param;
    }
}

bool ElementManager::isShell() {
    if (key == "QuadElement1") {
        return true;
    }
    else if (key == "TriElement1") {
        return true;
    }
    return false;
}

bool ElementManager::isBar() {
    if (key == "BEBarElement") {
        return true;
    }
    else if (key == "TBarElement") {
        return true;
    }
    return false;
}

int ElementManager::nodeCount() {
    auto node = nodes();
    int result = (int)node.size();
    return result;
}

/// <summary> Shell用 </summary>
MatrixXd ElementManager::gradMatrix(vector<FENode>nodes, double h, ShellParameter sp) {
    if (key == "QuadElement1") {
        // return _QuadElement1.gradMatrix(nodes, h, sp);
    }
    else if (key == "TriElement1") {
        // return _TriElement1.gradMatrix(nodes, h, sp);
    }

    return MatrixXd(1, 1); // 仮
}

/// <summary> Bar用 </summary>
MatrixXd ElementManager::gradMatrix(vector<FENode>nodes, double h, Section sp) {
    if (key == "BEBarElement") {
        // return _BEBarElement.gradMatrix(nodes, h, sp);
    }
    else if (key == "TBarElement") {
        // return _TBarElement.gradMatrix(nodes, h, sp);
    }

    return MatrixXd(1, 1); // 仮
}

/// <summary> Solid用 </summary>
MatrixXd ElementManager::gradMatrix(vector<FENode>nodes, double h) {
    
    if (key == "TetraElement1") {
        // return _TetraElement1.gradMatrix(nodes, h);
    }
    else if (key == "TetraElement2") {
        // return _TetraElement2.material =mat;
    }
    else if (key == "HexaElement1") {
        return _HexaElement1.gradMatrix(nodes, h);
    }
    else if (key == "HexaElement2") {
        // return _HexaElement2.gradMatrix(nodes, h);
    }
    else if (key == "HexaElement1WT") {
        // return _HexaElement1WT.gradMatrix(nodes, h);
    }
    else if (key == "WedgeElement1") {
        // return _WedgeElement1.gradMatrix(nodes, h);
    }
    else if (key == "WedgeElement2") {
        // return _WedgeElement2.gradMatrix(nodes, h);
    }

    return MatrixXd(1, 1); // 仮
}

/// <summary> Shell用 </summary>
MatrixXd ElementManager::stiffnessMatrix(vector<FENode> p, MatrixXd d1, ShellParameter sp) {
    if (key == "QuadElement1") {
        // return _QuadElement1.stiffnessMatrix(p, d1, sp);
    }
    else if (key == "TriElement1") {
        // return _TriElement1.stiffnessMatrix(p, d1, sp);
    }

    return MatrixXd(1, 1); // 仮
}

/// <summary> Bar用 </summary>
MatrixXd ElementManager::stiffnessMatrix(vector<FENode> p, Material d1, Section sp) {
    if (key == "BEBarElement") {
        // return _BEBarElement.stiffnessMatrix(p, d1, sp);
    }
    else if (key == "TBarElement") {
        // return _TBarElement.stiffnessMatrix(p, d1, sp);
    }

    return MatrixXd(1, 1); // 仮
}

/// <summary> Solid用 </summary>
MatrixXd ElementManager::stiffnessMatrix(vector<FENode> p, MatrixXd d1) {

    if (key == "TetraElement1") {
        // return _TetraElement1.stiffnessMatrix(p, d1);
    }
    else if (key == "TetraElement2") {
        // return _TetraElement2.stiffnessMatrix(p, d1);
    }
    else if (key == "HexaElement1") {
        return _HexaElement1.stiffnessMatrix(p, d1);
    }
    else if (key == "HexaElement2") {
        // return _HexaElement2.stiffnessMatrix(p, d1);
    }
    else if (key == "HexaElement1WT") {
        // return _HexaElement1WT.stiffnessMatrix(p, d1);
    }
    else if (key == "WedgeElement1") {
        // return _WedgeElement1.stiffnessMatrix(p, d1);
    }
    else if (key == "WedgeElement2") {
        // return _WedgeElement2.stiffnessMatrix(p, d1);
    }
    return MatrixXd(1, 1);
}



// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
tuple<Strain, Stress, double, Strain, Stress, double>
ElementManager::elementStrainStress(
    vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp) {
    if (key == "QuadElement1") {
        // return _QuadElement1.elementStrainStress(p, u, d1, sp);
    }
    else if (key == "TriElement1") {
        // return _TriElement1.elementStrainStress(p, u, d1, sp);
    }

    Strain strain;
    Stress stress;
    double energy;
    auto result = make_tuple(strain, stress, energy, strain, stress, energy);
    return result;
}

tuple<Strain, Stress, double, Strain, Stress, double>
ElementManager::elementStrainStress(
    vector<FENode> p, vector<Vector3R> u, Material material, Section sect) {
    if (key == "BEBarElement") {
        // return _BEBarElement.elementStrainStress(p, u, material, sect);
    }
    else if (key == "TBarElement") {
        // return _TBarElement.elementStrainStress(p, u, material, sect);
    }

    Strain strain;
    Stress stress;
    double energy;
    auto result = make_tuple(strain, stress, energy, strain, stress, energy);
    return result;
}

tuple<Strain, Stress, double> 
ElementManager::elementStrainStress(
    vector<FENode> p, vector<Vector3R> u, MatrixXd d1) {
    if (key == "TetraElement1") {
        // return _TetraElement1.elementStrainStress(p, u, d1);
    }
    else if (key == "TetraElement2") {
        // return _TetraElement2.elementStrainStress(p, u, d1);
    }
    else if (key == "HexaElement1") {
        return _HexaElement1.elementStrainStress(p, u, d1);
    }
    else if (key == "HexaElement2") {
        // return _HexaElement2.elementStrainStress(p, u, d1);
    }
    else if (key == "HexaElement1WT") {
        // return _HexaElement1WT.elementStrainStress(p, u, d1);
    }
    else if (key == "WedgeElement1") {
        // return _WedgeElement1.elementStrainStress(p, u, d1);
    }
    else if (key == "WedgeElement2") {
        // return _WedgeElement2.elementStrainStress(p, u, d1);
    }

    Strain strain;
    Stress stress;
    double energy;
    auto result = make_tuple(strain, stress, energy);
    return result;
}


tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
ElementManager::strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp) {
    if (key == "QuadElement1") {
        // return _QuadElement1.strainStress(p, u, d1);
    }
    else if (key == "TriElement1") {
        // return _TriElement1.strainStress(p, u, d1);
    }

    vector<Strain> strain;
    vector<Stress> stress;
    vector<double> energy;
    auto result = make_tuple(strain, stress, energy, strain, stress, energy);
    return result;
}

tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
ElementManager::strainStress(vector<FENode> p, vector<Vector3R> u, Material material, Section sect) {
    if (key == "BEBarElement") {
        // return _BEBarElement.strainStress(p, u, material, sect);
    }
    else if (key == "TBarElement") {
        // return _TBarElement.strainStress(p, u, material, sect);
    }

    vector<Strain> strain;
    vector<Stress> stress;
    vector<double> energy;
    auto result = make_tuple(strain, stress, energy, strain, stress, energy);
    return result;
}


tuple<vector<Strain>, vector<Stress>, vector<double>>
ElementManager::strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) {

    if (key == "TetraElement1") {
        // return _TetraElement1.strainStress(p, u, d1);
    }
    else if (key == "TetraElement2") {
        // return _TetraElement2.material =mat;
    }
    else if (key == "HexaElement1") {
        _HexaElement1.strainStress(p, u, d1);
    }
    else if (key == "HexaElement2") {
        // return _HexaElement2.strainStress(p, u, d1);
    }
    else if (key == "HexaElement1WT") {
        // return _HexaElement1WT.strainStress(p, u, d1);
    }
    else if (key == "WedgeElement1") {
        // return _WedgeElement1.strainStress(p, u, d1);
    }
    else if (key == "WedgeElement2") {
        // return _WedgeElement2.strainStress(p, u, d1);
    }

    vector<Strain> strain;
    vector<Stress> stress;
    vector<double> energy;
    auto result = make_tuple(strain, stress, energy);
    return result;
}

VectorXd ElementManager::angle(vector<FENode> p) {
    if (key == "BEBarElement") {
        // return _BEBarElement.angle(p);
    }
    else if (key == "TBarElement") {
        // return _TBarElement.angle(p);
    }
    else if (key == "TetraElement1") {
        // return _TetraElement1.angle(p);
    }
    else if (key == "TetraElement2") {
        // return _TetraElement2.material =mat;
    }
    else if (key == "HexaElement1") {
       return _HexaElement1.angle(p);
    }
    else if (key == "HexaElement2") {
        // return _HexaElement2.angle(p);
    }
    else if (key == "HexaElement1WT") {
        // return _HexaElement1WT.angle(p);
    }
    else if (key == "WedgeElement1") {
        // return _WedgeElement1.angle(p);
    }
    else if (key == "WedgeElement2") {
        // return _WedgeElement2.angle(p);
    }
    else if (key == "QuadElement1") {
        // return _QuadElement1.angle(p);
    }
    else if (key == "TriElement1") {
        // return _TriElement1.angle(p);
    }

    return VectorXd(1);

}

/*

// 要素境界を返す
// element - 要素ラベル
// index - 要素境界のインデックス
BorderManager ElementManager::border(int element, int index) {
    switch (index) {
    default:
        return BorderManager();
    case 0:
        vector<int> p = { nodes[0], nodes[1] };
        auto eb1 = BorderManager("EdgeBorder1", element, p);
        return eb1;
    }
}


// 要素境界辺を返す 梁要素では要素境界と同じ
// element - 要素ラベル
// index - 要素境界辺のインデックス
BorderManager ElementManager::borderEdge(int element, int index) {
    return border(element, index);
};
*/


string ElementManager::getName() {
    return key;
}

