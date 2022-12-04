#include "ElementManager.h"

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


/// <summary>
/// �f�X�g���N�^, �|�C���^���폜
/// </summary>
ElementManager::~ElementManager() {

    if (pBEBarElement != NULL) {
        delete pBEBarElement;
    } else if (pTBarElement != NULL) {
        delete pTBarElement;
    } else if (pTetraElement1 != NULL) {
        delete pTetraElement1;
    } else if (pTetraElement2 != NULL) {
        delete pTetraElement2;
    } else if (pHexaElement1 != NULL) {
        delete pHexaElement1;
    } else if (pHexaElement2 != NULL) {
        delete pHexaElement2;
    } else if (pHexaElement1WT != NULL) {
        delete pHexaElement1WT;
    } else if (pWedgeElement1 != NULL) {
        delete pWedgeElement1;
    } else if (pWedgeElement2 != NULL) {
        delete pWedgeElement2;
    }

}

ElementManager::ElementManager(string _key, vector<string> columns) {

    key = _key;

    if (key == "BEBarElement") {
        //pBEBarElement = new BEBarElement();
    }
    else if (key == "TBarElement") {
        //pTBarElement = new TBarElement();;
    }
    else if (key == "TetraElement1") {
        //pTetraElement1 = new TetraElement1();
    }
    else if (key == "TetraElement2") {
        //pTetraElement2 = new TetraElement2();
    }
    else if (key == "HexaElement1") {
        int label = stoi(columns[1]);
        int material = stoi(columns[2]);
        vector<int> nodes;
        for (int i = 3; i < 11; i++) {
            int no = stoi(columns[i]);
            nodes.push_back(no);
        }
        pHexaElement1 = new HexaElement1(label, material, nodes);
    }
    else if (key == "HexaElement2") {
        //pHexaElement2 new HexaElement2();
    }
    else if (key == "HexaElement1WT") {
        //pHexaElement1WT = new HexaElement1WT();
    }
    else if (key == "WedgeElement1") {
        //pWedgeElement1 = new WedgeElement1();
    }
    else if (key == "WedgeElement2") {
        //pWedgeElement2 = new WedgeElement2();
    }
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

int ElementManager::label() {
    return 0;
};

