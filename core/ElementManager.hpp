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

    // �v�f���E��Ԃ�
    // element - �v�f���x��
    // index - �v�f���E�̃C���f�b�N�X
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

    // �v�f���E�ӂ�Ԃ� ���v�f�ł͗v�f���E�Ɠ���
    // element - �v�f���x��
    // index - �v�f���E�ӂ̃C���f�b�N�X
    BorderManager borderEdge(int element, int index) {
        return border(element, index);
    };



};
