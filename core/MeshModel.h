#pragma once
#include "FENode.h"
#include "ElementManager.h"

#include <algorithm>
#include <vector>
using namespace std;
using std::vector;


class MeshModel {

private:

public:
    vector<FENode> nodes;   // êﬂì_
    vector<ElementManager> elements;   // óvëf

    MeshModel();
    vector<FENode> getNodes(ElementManager s);
    void clear();
    void init();
    void checkChirality();
    void getFreeFaces();
    void getFaceEdges();
};