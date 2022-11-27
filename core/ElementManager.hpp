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

    // 要素境界を返す
    // element - 要素ラベル
    // index - 要素境界のインデックス
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

    // 要素境界辺を返す 梁要素では要素境界と同じ
    // element - 要素ラベル
    // index - 要素境界辺のインデックス
    BorderManager borderEdge(int element, int index) {
        return border(element, index);
    };



};
