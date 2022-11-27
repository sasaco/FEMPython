#pragma once
#include "FENode.h"

#include <any>
#include <vector>
#include <algorithm>
using namespace std;
using std::vector;


class MeshModel {

private:
    vector<FENode> nodes;   // �ߓ_
    vector<any> elements;   // �v�f

public:
    MeshModel();

    template <typename T>
    vector<FENode> getNodes(T s);

    void clear();

    void init()
};