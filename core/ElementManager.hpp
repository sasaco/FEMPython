#pragma once
#include "BorderManager.hpp"

#include "HexaElement1.h"

#include <vector>
#include <stdexcept>
using namespace std;
using std::vector;

class ElementManager {
private:
    string key;

    HexaElement1 MyHexaElement1;



public:
    ElementManager(string key, vector<string> columns);
    
    int label();
    vector<int> nodes;


    BorderManager border(int element, int index);
    BorderManager borderEdge(int element, int index);

};


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


int ElementManager::label() {
    return 0;
};

