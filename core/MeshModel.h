#pragma once
#include "FENode.h"
#include "ElementManager.hpp"

#include <vector>
using namespace std;
using std::vector;


class MeshModel {

private:
    vector<FENode> nodes;   // �ߓ_
    vector<ElementManager> elements;   // �v�f

public:
    MeshModel();

    template <typename T>
    vector<FENode> getNodes(T s);

    void clear();

    void init();
};