#include "ElementManager.h"

ElementManager::ElementManager() {
    key = "";
}
ElementManager::ElementManager(string keyWord, vector<string> columns) {

    // keyWordは、小文字に変換する
    std::transform(keyWord.begin(), keyWord.end(), keyWord.begin(), ::tolower);

    if (keyWord == "sebarelement")  {
        // frameWeb で用いる棒要素

    }
    else if ((keyWord == "bebarelement") && (columns.size() > 5)) {

        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        string param = std::to_string(stoi(columns[3]));
        vector<string> nodes;
        for (int i = 4; i < 6; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }

        if (columns.size() < 8) {
            _BEBarElement = BEBarElement(label, material, param, nodes);
        }
        else {
            double x = stod(columns[6]);
            double y = stod(columns[7]);
            double z = stod(columns[8]);
            Vector3Dim axis = Vector3Dim(x, y, z);
            _BEBarElement = BEBarElement(label, material, param, nodes, axis);
        }
    }
    else if ((keyWord == "tbarelement") && (columns.size() > 5)) {
        
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        string param = std::to_string(stoi(columns[3]));
        vector<string> nodes;
        for (int i = 4; i < 6; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }

        if (columns.size() < 8) {
            _TBarElement = TBarElement(label, material, param, nodes);
        }
        else {
            double x = stod(columns[6]);
            double y = stod(columns[7]); 
            double z = stod(columns[8]);
            Vector3Dim axis = Vector3Dim(x, y, z);
            _TBarElement = TBarElement(label, material, param, nodes, axis);
        }
    }
    else if ((keyWord == "trielement1") && (columns.size() > 6)) {
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        string param = std::to_string(stoi(columns[3]));
        vector<string> nodes;
        for (int i = 4; i < 7; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }
        _TriElement1 = TriElement1(label, material, param, nodes);
    }
    else if ((keyWord == "quadelement1") && (columns.size() > 7)) {
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        string param = std::to_string(stoi(columns[3]));
        vector<string> nodes;
        for (int i = 4; i < 8; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }
        _QuadElement1 = QuadElement1(label, material, param, nodes);
    }
    else if ((keyWord == "tetraelement1") && (columns.size() > 6)) {
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        vector<string> nodes;
        for (int i = 3; i < 7; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }
        _TetraElement1 = TetraElement1(label, material, nodes);
    }
    else if ((keyWord == "tetraelement2") && (columns.size() > 12)) {
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        vector<string> nodes;
        for (int i = 3; i < 13; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }
        _TetraElement2 = TetraElement2(label, material, nodes);
    }
    else if ((keyWord == "wedgeelement1") && (columns.size() > 8)) {
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        vector<string> nodes;
        for (int i = 3; i < 9; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }
        _WedgeElement1 = WedgeElement1(label, material, nodes);
    }
    else if ((keyWord == "wedgeelement2") && (columns.size() > 17)) {
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        vector<string> nodes;
        for (int i = 3; i < 18; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }
        _WedgeElement2 = WedgeElement2(label, material, nodes);
    }
    else if ((keyWord == "hexaelement1") && (columns.size() > 10)) {
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        vector<string> nodes;
        for (int i = 3; i < 11; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }
        _HexaElement1 = HexaElement1(label, material, nodes);
    }    
    else if ((keyWord == "hexaelement2") && (columns.size() > 22)) {
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        vector<string> nodes;
        for (int i = 3; i < 23; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }
        _HexaElement2 = HexaElement2(label, material, nodes);
    }
    else if ((keyWord == "hexaelement1wt") && (columns.size() > 10)) {
        string label = std::to_string(stoi(columns[1]));
        string material = std::to_string(stoi(columns[2]));
        vector<string> nodes;
        for (int i = 3; i < 11; i++) {
            string no = std::to_string(stoi(columns[i]));
            nodes.push_back(no);
        }
        _HexaElement1WT = HexaElement1WT(label, material, nodes);
    }

    key = keyWord;
}

vector<string> ElementManager::nodes() {

    if (key == "bebarelement") {
        return _BEBarElement.nodes;
    }
    else if (key == "tbarelement") {
        return _TBarElement.nodes;
    }
    else if (key == "tetraelement1") {
        return _TetraElement1.nodes;
    }
    else if (key == "tetraelement2") {
        return _TetraElement2.nodes;
    }
    else if (key == "hexaelement1") {
        return _HexaElement1.nodes;
    }
    else if (key == "hexaelement2") {
        return _HexaElement2.nodes;
    }
    else if (key == "hexaelement1wt") {
        return _HexaElement1WT.nodes;
    }
    else if (key == "wedgeelement1") {
        return _WedgeElement1.nodes;
    }
    else if (key == "wedgeelement2") {
        return _WedgeElement2.nodes;
    }
    else if (key == "quadelement1") {
        return _QuadElement1.nodes;
    }
    else if (key == "trielement1") {
        return _TriElement1.nodes;
    }

    vector<string> empty;
    return empty;
}

void ElementManager::setIndexs(vector<int> tmp) {
    nodeIndexs = tmp;
}

void ElementManager::setNodes(vector<string> tmp) {

    if (key == "bebarelement") {
        _BEBarElement.nodes = tmp;
    }
    else if (key == "tbarelement") {
        _TBarElement.nodes = tmp;
    }
    else if (key == "tetraelement1") {
        _TetraElement1.nodes = tmp;
    }
    else if (key == "tetraelement2") {
        _TetraElement2.nodes = tmp;
    }
    else if (key == "hexaelement1") {
        _HexaElement1.nodes = tmp;
    }
    else if (key == "hexaelement2") {
        _HexaElement2.nodes = tmp;
    }
    else if (key == "hexaelement1wt") {
        _HexaElement1WT.nodes = tmp;
    }
    else if (key == "wedgeelement1") {
        _WedgeElement1.nodes = tmp;
    }
    else if (key == "wedgeelement2") {
        _WedgeElement2.nodes = tmp;
    }
    else if (key == "quadelement1") {
        _QuadElement1.nodes = tmp;
    }
    else if (key == "trielement1") {
        _TriElement1.nodes = tmp;
    }
}


string ElementManager::label() {
    if (key == "bebarelement") {
        return _BEBarElement.label;
    }
    else if (key == "tbarelement") {
        return _TBarElement.label;
    }
    else if (key == "tetraelement1") {
        return _TetraElement1.label;
    }
    else if (key == "tetraelement2") {
        return _TetraElement2.label;
    }
    else if (key == "hexaelement1") {
        return _HexaElement1.label;
    }
    else if (key == "hexaelement2") {
        return _HexaElement2.label;
    }
    else if (key == "hexaelement1wt") {
        return _HexaElement1WT.label;
    }
    else if (key == "wedgeelement1") {
        return _WedgeElement1.label;
    }
    else if (key == "wedgeelement2") {
        return _WedgeElement2.label;
    }
    else if (key == "quadelement1") {
        return _QuadElement1.label;
    }
    else if (key == "trielement1") {
        return _TriElement1.label;
    }
    return "";
}


string ElementManager::material() {
    if (key == "bebarelement") {
        return _BEBarElement.material;
    }
    else if (key == "tbarelement") {
        return _TBarElement.material;
    }
    else if (key == "tetraelement1") {
        return _TetraElement1.material;
    }
    else if (key == "tetraelement2") {
        return _TetraElement2.material;
    }
    else if (key == "hexaelement1") {
        return _HexaElement1.material;
    }
    else if (key == "hexaelement2") {
        return _HexaElement2.material;
    }
    else if (key == "hexaelement1wt") {
        return _HexaElement1WT.material;
    }
    else if (key == "wedgeelement1") {
        return _WedgeElement1.material;
    }
    else if (key == "wedgeelement2") {
        return _WedgeElement2.material;
    }
    else if (key == "quadelement1") {
        return _QuadElement1.material;
    }
    else if (key == "trielement1") {
        return _TriElement1.material;
    }
    return "";
}

void ElementManager::setMaterial(string mat) {
    if (key == "bebarelement") {
        _BEBarElement.material = mat;
    }
    else if (key == "tbarelement") {
        _TBarElement.material = mat;
    }
    else if (key == "tetraelement1") {
        _TetraElement1.material = mat;
    }
    else if (key == "tetraelement2") {
        _TetraElement2.material =mat;
    }
    else if (key == "hexaelement1") {
        _HexaElement1.material = mat;
    }
    else if (key == "hexaelement2") {
        _HexaElement2.material = mat;
    }
    else if (key == "hexaelement1wt") {
        _HexaElement1WT.material = mat;
    }
    else if (key == "wedgeelement1") {
        _WedgeElement1.material = mat;
    }
    else if (key == "wedgeelement2") {
        _WedgeElement2.material = mat;
    }
    else if (key == "quadelement1") {
        _QuadElement1.material = mat;
    }
    else if (key == "trielement1") {
        _TriElement1.material = mat;
    }
}

void ElementManager::setMaterialIndex(int mat) {
    materialIndex = mat;
}


string ElementManager::param() {
    if (key == "bebarelement") {
        return _BEBarElement.param;
    }
    else if (key == "tbarelement") {
        return _TBarElement.param;
    }
    else if (key == "quadelement1") {
        return _QuadElement1.param;
    }
    else if (key == "trielement1") {
        return _TriElement1.param;
    }
    return "";
}

void ElementManager::setParam(string param) {
    if (key == "bebarelement") {
        _BEBarElement.param = param;
    }
    else if (key == "tbarelement") {
        _TBarElement.param = param;
    }
    else if (key == "quadelement1") {
        _QuadElement1.param = param;
    }
    else if (key == "trielement1") {
        _TriElement1.param = param;
    }
}

void ElementManager::setParamIndex(int param) {
    paramIndex = param;
}


bool ElementManager::isShell() {
    if (key == "quadelement1") {
        return true;
    }
    else if (key == "trielement1") {
        return true;
    }
    return false;
}

bool ElementManager::isBar() {
    if (key == "bebarelement") {
        return true;
    }
    else if (key == "tbarelement") {
        return true;
    }
    return false;
}

int ElementManager::nodeCount() {
    auto node = nodes();
    int result = (int)node.size();
    return result;
}


/// <summary> Shell�p </summary>
MatrixXd ElementManager::gradMatrix(vector<FENode>nodes, double h, ShellParameter sp) {
    if (key == "quadelement1") {
        return _QuadElement1.gradMatrix(nodes, h, sp);
    }
    else if (key == "trielement1") {
        return _TriElement1.gradMatrix(nodes, h, sp);
    }

    return MatrixXd(1, 1); // ��
}

/// <summary> Bar�p </summary>
MatrixXd ElementManager::gradMatrix(vector<FENode>nodes, double h, SectionManager sp) {
    if (key == "bebarelement") {
         return _BEBarElement.gradMatrix(nodes, h, sp);
    }
    else if (key == "tbarelement") {
        return _TBarElement.gradMatrix(nodes, h, sp);
    }
    return MatrixXd(1, 1); // ��
}

/// <summary> Solid�p </summary>
MatrixXd ElementManager::gradMatrix(vector<FENode>nodes, double h) {
    
    if (key == "tetraelement1") {
        return _TetraElement1.gradMatrix(nodes, h);
    }
    else if (key == "tetraelement2") {
        return _TetraElement2.gradMatrix(nodes, h);
    }
    else if (key == "hexaelement1") {
        return _HexaElement1.gradMatrix(nodes, h);
    }
    else if (key == "hexaelement2") {
        return _HexaElement2.gradMatrix(nodes, h);
    }
    else if (key == "hexaelement1wt") {
        return _HexaElement1WT.gradMatrix(nodes, h);
    }
    else if (key == "wedgeelement1") {
        return _WedgeElement1.gradMatrix(nodes, h);
    }
    else if (key == "wedgeelement2") {
        return _WedgeElement2.gradMatrix(nodes, h);
    }

    return MatrixXd(1, 1); // ��
}

/// <summary> Shell�p </summary>
MatrixXd ElementManager::stiffnessMatrix(vector<FENode> p, MatrixXd d1, ShellParameter sp) {
    if (key == "quadelement1") {
        return _QuadElement1.stiffnessMatrix(p, d1, sp);
    }
    else if (key == "trielement1") {
        return _TriElement1.stiffnessMatrix(p, d1, sp);
    }
    return MatrixXd(1, 1); // ��
}

/// <summary> Bar�p </summary>
MatrixXd ElementManager::stiffnessMatrix(vector<FENode> p, Material d1, SectionManager sp) {
    if (key == "bebarelement") {
        return _BEBarElement.stiffnessMatrix(p, d1, sp);
    }
    else if (key == "tbarelement") {
        return _TBarElement.stiffnessMatrix(p, d1, sp);
    }
    return MatrixXd(1, 1); // ��
}

/// <summary> Solid�p </summary>
MatrixXd ElementManager::stiffnessMatrix(vector<FENode> p, MatrixXd d1) {

    if (key == "tetraelement1") {
        return _TetraElement1.stiffnessMatrix(p, d1);
    }
    else if (key == "tetraelement2") {
        return _TetraElement2.stiffnessMatrix(p, d1);
    }
    else if (key == "hexaelement1") {
        return _HexaElement1.stiffnessMatrix(p, d1);
    }
    else if (key == "hexaelement2") {
        return _HexaElement2.stiffnessMatrix(p, d1);
    }
    else if (key == "hexaelement1wt") {
        return _HexaElement1WT.stiffnessMatrix(p, d1);
    }
    else if (key == "wedgeelement1") {
        return _WedgeElement1.stiffnessMatrix(p, d1);
    }
    else if (key == "wedgeelement2") {
        return _WedgeElement2.stiffnessMatrix(p, d1);
    }
    return MatrixXd(1, 1);
}



// �v�f�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
tuple<Strain, Stress, double, Strain, Stress, double>
ElementManager::elementStrainStress(
    vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp) {
    if (key == "quadelement1") {
        return _QuadElement1.elementStrainStress(p, u, d1, sp);
    }
    else if (key == "trielement1") {
        return _TriElement1.elementStrainStress(p, u, d1, sp);
    }
    // err -> ダミーを返す
    Strain strain;
    Stress stress;
    double energy;
    auto result = make_tuple(strain, stress, energy, strain, stress, energy);
    return result;
}

tuple<Strain, Stress, double, Strain, Stress, double>
ElementManager::elementStrainStress(
    vector<FENode> p, vector<Vector3R> u, Material material, SectionManager sect) {
    if (key == "bebarelement") {
        return _BEBarElement.elementStrainStress(p, u, material, sect);
    }
    else if (key == "tbarelement") {
        return _TBarElement.elementStrainStress(p, u, material, sect);
    }
    // err -> ダミーを返す
    Strain strain;
    Stress stress;
    double energy;
    auto result = make_tuple(strain, stress, energy, strain, stress, energy);
    return result;
}

tuple<Strain, Stress, double> 
ElementManager::elementStrainStress(
    vector<FENode> p, vector<Vector3R> u, MatrixXd d1) {
    if (key == "tetraelement1") {
        return _TetraElement1.elementStrainStress(p, u, d1);
    }
    else if (key == "tetraelement2") {
        return _TetraElement2.elementStrainStress(p, u, d1);
    }
    else if (key == "hexaelement1") {
        return _HexaElement1.elementStrainStress(p, u, d1);
    }
    else if (key == "hexaelement2") {
        return _HexaElement2.elementStrainStress(p, u, d1);
    }
    else if (key == "hexaelement1wt") {
        return _HexaElement1WT.elementStrainStress(p, u, d1);
    }
    else if (key == "wedgeelement1") {
        return _WedgeElement1.elementStrainStress(p, u, d1);
    }
    else if (key == "wedgeelement2") {
        return _WedgeElement2.elementStrainStress(p, u, d1);
    }
    // err -> ダミーを返す
    Strain strain;
    Stress stress;
    double energy;
    auto result = make_tuple(strain, stress, energy);
    return result;
}


tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
ElementManager::strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp) {
    if (key == "quadelement1") {
        return _QuadElement1.strainStress(p, u, d1, sp);
    }
    else if (key == "trielement1") {
        return _TriElement1.strainStress(p, u, d1, sp);
    }
    // err -> ダミーを返す
    vector<Strain> strain;
    vector<Stress> stress;
    vector<double> energy;
    auto result = make_tuple(strain, stress, energy, strain, stress, energy);
    return result;
}

tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
ElementManager::strainStress(vector<FENode> p, vector<Vector3R> u, Material material, SectionManager sect) {
    if (key == "bebarelement") {
        return _BEBarElement.strainStress(p, u, material, sect);
    }
    else if (key == "tbarelement") {
        return _TBarElement.strainStress(p, u, material, sect);
    }
    // err -> ダミーを返す
    vector<Strain> strain;
    vector<Stress> stress;
    vector<double> energy;
    auto result = make_tuple(strain, stress, energy, strain, stress, energy);
    return result;
}


tuple<vector<Strain>, vector<Stress>, vector<double>>
ElementManager::strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) {

    if (key == "tetraelement1") {
        return _TetraElement1.strainStress(p, u, d1);
    }
    else if (key == "tetraelement2") {
        return _TetraElement2.strainStress(p, u, d1);
    }
    else if (key == "hexaelement1") {
        return _HexaElement1.strainStress(p, u, d1);
    }
    else if (key == "hexaelement2") {
        return _HexaElement2.strainStress(p, u, d1);
    }
    else if (key == "hexaelement1wt") {
        return _HexaElement1WT.strainStress(p, u, d1);
    }
    else if (key == "wedgeelement1") {
        return _WedgeElement1.strainStress(p, u, d1);
    }
    else if (key == "wedgeelement2") {
        return _WedgeElement2.strainStress(p, u, d1);
    }
    // err -> ダミーを返す
    vector<Strain> strain;
    vector<Stress> stress;
    vector<double> energy;
    auto result = make_tuple(strain, stress, energy);
    return result;
}

VectorXd ElementManager::angle(vector<FENode> p) {
    if (key == "tetraelement1") {
        return _TetraElement1.angle(p);
    }
    else if (key == "tetraelement2") {
        return _TetraElement2.angle(p);
    }
    else if (key == "hexaelement1") {
       return _HexaElement1.angle(p);
    }
    else if (key == "hexaelement2") {
        return _HexaElement2.angle(p);
    }
    else if (key == "hexaelement1wt") {
        return _HexaElement1WT.angle(p);
    }
    else if (key == "wedgeelement1") {
        return _WedgeElement1.angle(p);
    }
    else if (key == "wedgeelement2") {
        return _WedgeElement2.angle(p);
    }
    return VectorXd(1);
}

/*

// �v�f���E��Ԃ�
// element - �v�f���x��
// index - �v�f���E�̃C���f�b�N�X
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


// �v�f���E�ӂ�Ԃ� ���v�f�ł͗v�f���E�Ɠ���
// element - �v�f���x��
// index - �v�f���E�ӂ̃C���f�b�N�X
BorderManager ElementManager::borderEdge(int element, int index) {
    return border(element, index);
};
*/


string ElementManager::getName() {
    return key;
}

