#pragma once
#include "BorderManager.hpp"

#include <vector>
#include <stdexcept>
using namespace std;
using std::vector;

class ElementManager
{
private:
    vector<int> nodes;

public:
    int label() {
        return 0;
    };

    // —v‘f‹«ŠE‚ð•Ô‚·
    // element - —v‘fƒ‰ƒxƒ‹
    // index - —v‘f‹«ŠE‚ÌƒCƒ“ƒfƒbƒNƒX
    BorderManager border(int element, int index) {
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
    BorderManager borderEdge(int element, int index) {
        return border(element, index);
    };



};
