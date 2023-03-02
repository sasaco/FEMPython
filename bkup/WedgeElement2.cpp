#include "WedgeElement2.h"

//--------------------------------------------------------------------//
// 楔形2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
WedgeElement2::WedgeElement2(int label, int material, vector<int> nodes) :
    SolidElement(label, material, nodes, WEDGE2_NODE, WEDGE2_INT){
};

// 要素名称を返す
string WedgeElement2::getName() {
    return "'WedgeElement2";
};

// 節点数を返す
int WedgeElement2::nodeCount() {
    return 15;
};


// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
void WedgeElement2::shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) {

    double xe = 1 - xsi - eta;

    out = {
        {
            xe * (xe - 0.5 * zeta - 1) * (1 - zeta),
            -(2 * xe - 0.5 * zeta - 1) * (1 - zeta),
            -(2 * xe - 0.5 * zeta - 1) * (1 - zeta), 
            xe * (zeta - xe + 0.5)
        },
        {
            xsi * (xsi - 0.5 * zeta - 1) * (1 - zeta), 
            (2 * xsi - 0.5 * zeta - 1) * (1 - zeta),
            0, 
            xsi * (zeta - xsi + 0.5)
        },
        {
            eta * (eta - 0.5 * zeta - 1) * (1 - zeta), 
            0,
            (2 * eta - 0.5 * zeta - 1) * (1 - zeta), 
            eta * (zeta - eta + 0.5)
        },
        {
            xe * (xe + 0.5 * zeta - 1) * (1 + zeta), 
            -(2 * xe + 0.5 * zeta - 1) * (1 + zeta),
            -(2 * xe + 0.5 * zeta - 1) * (1 + zeta), 
            xe * (zeta + xe - 0.5)
        },
        {
            xsi * (xsi + 0.5 * zeta - 1) * (1 + zeta), 
            (2 * xsi + 0.5 * zeta - 1) * (1 + zeta),
            0, 
            xsi * (zeta + xsi - 0.5)
        },
        {
            eta * (eta + 0.5 * zeta - 1) * (1 + zeta), 
            0,
            (2 * eta + 0.5 * zeta - 1) * (1 + zeta), 
            eta * (zeta + eta - 0.5)
        },
        {
            2 * xe * xsi * (1 - zeta), 
            2 * (xe - xsi) * (1 - zeta), 
            -2 * xsi * (1 - zeta),
            -2 * xe * xsi
        },
        {
            2 * xsi * eta * (1 - zeta), 
            2 * eta * (1 - zeta), 
            2 * xsi * (1 - zeta),
            -2 * xsi * eta
        },
        {
            2 * eta * xe * (1 - zeta), 
            -2 * eta * (1 - zeta), 
            2 * (xe - eta) * (1 - zeta),
            -2 * eta * xe
        },
        {
            2 * xe * xsi * (1 + zeta), 
            2 * (xe - xsi) * (1 + zeta), 
            -2 * xsi * (1 + zeta),
            2 * xe * xsi
        },
        {
            2 * xsi * eta * (1 + zeta), 
            2 * eta * (1 + zeta), 
            2 * xsi * (1 + zeta),
            2 * xsi * eta
        },
        {
            2 * eta * xe * (1 + zeta), 
            -2 * eta * (1 + zeta), 
            2 * (xe - eta) * (1 + zeta),
            2 * eta * xe
        },
        {
            xe * (1 + zeta) * (1 - zeta), 
            -(1 + zeta) * (1 - zeta), 
            -(1 + zeta) * (1 - zeta),
            -2 * xe * zeta
        },
        {
            xsi * (1 + zeta) * (1 - zeta), 
            (1 + zeta) * (1 - zeta), 
            0, 
            -2 * xsi * zeta
        },
        {
            eta * (1 + zeta) * (1 - zeta), 
            0, 
            (1 + zeta) * (1 - zeta), 
            -2 * eta * zeta
        }
    };
};
