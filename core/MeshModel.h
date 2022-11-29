#pragma once
#include "FENode.h"
#include "ElementManager.hpp"

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

    template <typename T>
    vector<FENode> getNodes(T s);

    void clear();

    void init();
};