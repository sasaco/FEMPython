#include "ElementManager.h"

ElementManager::ElementManager(string _key, vector<string> columns) {
    key = _key;

    if (key == "HexaElement1") {
        int label = stoi(columns[1]);
        int material = stoi(columns[2]);
        vector<int> nodes;
        for (int i = 3; i < 11; i++) {
            int no = stoi(columns[i]);
            nodes.push_back(no);
        }
        MyHexaElement1 = HexaElement1(label, material, nodes);
    }


}



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


int ElementManager::label() {
    return 0;
};

