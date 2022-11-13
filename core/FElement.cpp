#include "FElement.h";

#include <numeric>


//--------------------------------------------------------------------//
// 要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
FElement::FElement(int _label, int _material, vector<int> _nodes)
    : Nodes(_nodes) {

    label = label;
    material = _material;
    isShell = false;        // シェル要素ではない
    isBar = false;		    // 梁要素ではない
};

// 積分点の剛性マトリックスを返す
// d - 応力-歪マトリックス
// b - 歪-変位マトリックスの転置行列
// coef - 係数
void FElement::stiffPart(vector<vector<double>> d, vector<vector<double>> b, double coef, vector<vector<double>> out) {

    int size1 = b.size();
    int size2 = d.size();
    
    vector<double> a;

    for (int i = 0; i < size1; i++) {

        vector<double> bi = b[i];

        for (int j = 0; j < size2; j++) {
            a.push_back(
                coef * inner_product(bi.begin(), bi.end(), d[j].begin(), 0)
            );
        }

        vector<double> ki;

        for (int j = 0; j < size1; j++) {
            ki.push_back(
                inner_product(a.begin(), a.end(), b[j].begin(), 0)
            );
        }
        out.push_back(ki);
    }
}

// 節点変位を1次元配列に変換する
// u - 節点変位
// dof - 節点自由度
void FElement::toArray(vector<BoundaryCondition> u, int dof, vector<double> out) {

    int count = nodeCount();
    out.clear();

    for (int i = 0; i < count; i++) {
        vector<double> ux = u[i].x;
        for (int j = 0; j < dof; j++) {
            out.push_back(ux[j]);
        }
    }
};

// 節点変位を局所座標系・1次元配列に変換する
// u - 節点変位
// d - 方向余弦マトリックス
void FElement::toLocalArray(vector<BoundaryCondition> u, vector<vector<double>> d, vector<double> v) {

    v.clear();

    for (int i = 0; i < 2; i++) {
        vector<double> ux = u[i].x;
        for (int j = 0; j < 3; j++) {
            v.push_back(d[0][j] * ux[0] + d[1][j] * ux[1] + d[2][j] * ux[2]);
        }
        for (int j = 0; j < 3; j++) {
            v.push_back(d[0][j] * ux[3] + d[1][j] * ux[4] + d[2][j] * ux[5]);
        }
    }
}


