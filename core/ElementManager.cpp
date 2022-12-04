#include "ElementManager.h"


ElementManager::ElementManager(string _key, vector<string> columns) {

    if (_key == "BEBarElement") {
        //__BEBarElement = new BEBarElement();
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

    key = _key;

}


/*

// —v‘f‹«ŠE‚ð•Ô‚·
// element - —v‘fƒ‰ƒxƒ‹
// index - —v‘f‹«ŠE‚ÌƒCƒ“ƒfƒbƒNƒX
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


// —v‘f‹«ŠE•Ó‚ð•Ô‚· —À—v‘f‚Å‚Í—v‘f‹«ŠE‚Æ“¯‚¶
// element - —v‘fƒ‰ƒxƒ‹
// index - —v‘f‹«ŠE•Ó‚ÌƒCƒ“ƒfƒbƒNƒX
BorderManager ElementManager::borderEdge(int element, int index) {
    return border(element, index);
};
*/

int ElementManager::label() {
    return 0;
};

