#pragma once
#include "FENode.h"
#include "ElementManager.hpp"

#include <vector>
using namespace std;
using std::vector;


class MeshModel {

private:
    vector<FENode> nodes;   // êﬂì_
    vector<ElementManager> elements;   // óvëf

public:
    MeshModel();

    template <typename T>
    vector<FENode> getNodes(T s);

    void clear();

    void init();
};