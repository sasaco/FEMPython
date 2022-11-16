#include "SolidElement.h";

/// <summary>
/// 四面体2次要素
/// 開発途中...
/// </summary>
class TetraElement2 : SolidElement {

private:
    // 四面体2次要素の節点のξ,η,ζ座標
    const vector<vector<double>> TETRA2_NODE = {
        {0, 0, 0},
        {1, 0, 0},
        {0, 1, 0},
        {0, 0, 1},
        {0.5, 0, 0},
        {0.5, 0.5, 0},
        {0, 0.5, 0},
        {0, 0, 0.5},
        {0.5, 0, 0.5},
        {0, 0.5, 0.5}
    };

    // 四面体2次要素の積分点のξ,η,ζ座標,重み係数
    const vector<vector<double>> TETRA2_INT = {
        {GTETRA2[0], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[1], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[1], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[0], GTETRA2[1], C1_24}
    };

public:
    TetraElement2(int label, int material, vector<int> nodes);

    string getName() override;

    void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) override;

};

//--------------------------------------------------------------------//
// 四面体2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
TetraElement2::TetraElement2(int label, int material, vector<int> nodes) :
    SolidElement(label, material, nodes, TETRA2_NODE, TETRA2_INT) {
};


// 要素名称を返す
string TetraElement2::getName() {
    return "TetraElement2";
}



// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
void TetraElement2::shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) {
    
    double xez = 1 - xsi - eta - zeta;

    return [[xez * (2 * xez - 1), 1 - 4 * xez, 1 - 4 * xez, 1 - 4 * xez],
        [xsi * (2 * xsi - 1), 4 * xsi - 1, 0, 0],
        [eta * (2 * eta - 1), 0, 4 * eta - 1, 0],
        [zeta * (2 * zeta - 1), 0, 0, 4 * zeta - 1],
        [4 * xez * xsi, 4 * (xez - xsi), -4 * xsi, -4 * xsi],
        [4 * xsi * eta, 4 * eta, 4 * xsi, 0],
        [4 * xez * eta, -4 * eta, 4 * (xez - eta), -4 * eta],
        [4 * xez * zeta, -4 * zeta, -4 * zeta, 4 * (xez - zeta)],
        [4 * xsi * zeta, 4 * zeta, 0, 4 * xsi],
        [4 * eta * zeta, 0, 4 * zeta, 4 * eta]];
};
