#include "HexaElement2.h";


//--------------------------------------------------------------------//
// 六面体セレンディピティ族2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
HexaElement2::HexaElement2() : SolidElement() {}
HexaElement2::HexaElement2(string label, string material, vector<string> nodes) :
    SolidElement(label, material, nodes) {

    HEXA2_NODE << -1, -1, -1,
                   1, -1, -1,
                   1,  1, -1,
                  -1,  1, -1,
                  -1, -1,  1,
                   1, -1,  1,
                   1,  1,  1,
                  -1,  1,  1,
                   0, -1, -1,
                   1,  0, -1,
                   0,  1, -1,
                  -1,  0, -1,
                   0, -1,  1,
                   1,  0,  1,
                   0,  1,  1,
                  -1,  0,  1,
                  -1, -1,  0,
                   1, -1,  0,
                   1,  1,  0,
                  -1,  1,  0;

    SolidElement::nodeP = HEXA2_NODE;


    // 六面体2次要素の積分点のξ,η,ζ座標,重み係数
    for (int k = 0; k < 3; k++) {
        for (int j = 0; j < 3; j++) {
            int n = k * j;
            for (int i = 0; i < 3; i++) {
                HEXA2_INT(n, 0) = GX3[i];
                HEXA2_INT(n, 1) = GX3[j];
                HEXA2_INT(n, 2) = GX3[k];
                HEXA2_INT(n, 3) = GW3[i] * GW3[j] * GW3[k];
            }
        }
    }
    SolidElement::intP = HEXA2_INT;
}

// 要素名称を返す
string HexaElement2::getName() {
    return "HexaElement2";
}


// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
MatrixXd HexaElement2::shapeFunction(double xsi, double eta, double zeta) {

    MatrixXd result(20, 4);

    result(0, 0) = 0.125 * (1 - xsi) * (1 - eta) * (1 - zeta) * (-xsi - eta - zeta - 2);
    result(0, 0) = 0.125 * (1 - eta) * (1 - zeta) * (2 * xsi + eta + zeta + 1),
    result(0, 0) = 0.125 * (1 - xsi) * (1 - zeta) * (xsi + 2 * eta + zeta + 1),
    result(0, 0) = 0.125 * (1 - xsi) * (1 - eta) * (xsi + eta + 2 * zeta + 1)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.125 * (1 + xsi) * (1 - eta) * (1 - zeta) * (xsi - eta - zeta - 2),
    result(0, 0) = 0.125 * (1 - eta) * (1 - zeta) * (2 * xsi - eta - zeta - 1),
    result(0, 0) = 0.125 * (1 + xsi) * (1 - zeta) * (-xsi + 2 * eta + zeta + 1),
    result(0, 0) = 0.125 * (1 + xsi) * (1 - eta) * (-xsi + eta + 2 * zeta + 1)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.125 * (1 + xsi) * (1 + eta) * (1 - zeta) * (xsi + eta - zeta - 2),
    result(0, 0) = 0.125 * (1 + eta) * (1 - zeta) * (2 * xsi + eta - zeta - 1),
    result(0, 0) = 0.125 * (1 + xsi) * (1 - zeta) * (xsi + 2 * eta - zeta - 1),
    result(0, 0) = 0.125 * (1 + xsi) * (1 + eta) * (-xsi - eta + 2 * zeta + 1)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.125 * (1 - xsi) * (1 + eta) * (1 - zeta) * (-xsi + eta - zeta - 2),
    result(0, 0) = 0.125 * (1 + eta) * (1 - zeta) * (2 * xsi - eta + zeta + 1),
    result(0, 0) = 0.125 * (1 - xsi) * (1 - zeta) * (-xsi + 2 * eta - zeta - 1),
    result(0, 0) = 0.125 * (1 - xsi) * (1 + eta) * (xsi - eta + 2 * zeta + 1)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.125 * (1 - xsi) * (1 - eta) * (1 + zeta) * (-xsi - eta + zeta - 2),
    result(0, 0) = 0.125 * (1 - eta) * (1 + zeta) * (2 * xsi + eta - zeta + 1),
    result(0, 0) = 0.125 * (1 - xsi) * (1 + zeta) * (xsi + 2 * eta - zeta + 1),
    result(0, 0) = 0.125 * (1 - xsi) * (1 - eta) * (-xsi - eta + 2 * zeta - 1)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.125 * (1 + xsi) * (1 - eta) * (1 + zeta) * (xsi - eta + zeta - 2),
    result(0, 0) = 0.125 * (1 - eta) * (1 + zeta) * (2 * xsi - eta + zeta - 1),
    result(0, 0) = 0.125 * (1 + xsi) * (1 + zeta) * (-xsi + 2 * eta - zeta + 1),
    result(0, 0) = 0.125 * (1 + xsi) * (1 - eta) * (xsi - eta + 2 * zeta - 1)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.125 * (1 + xsi) * (1 + eta) * (1 + zeta) * (xsi + eta + zeta - 2),
    result(0, 0) = 0.125 * (1 + eta) * (1 + zeta) * (2 * xsi + eta + zeta - 1),
    result(0, 0) = 0.125 * (1 + xsi) * (1 + zeta) * (xsi + 2 * eta + zeta - 1),
    result(0, 0) = 0.125 * (1 + xsi) * (1 + eta) * (xsi + eta + 2 * zeta - 1)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.125 * (1 - xsi) * (1 + eta) * (1 + zeta) * (-xsi + eta + zeta - 2),
    result(0, 0) = 0.125 * (1 + eta) * (1 + zeta) * (2 * xsi - eta - zeta + 1),
    result(0, 0) = 0.125 * (1 - xsi) * (1 + zeta) * (-xsi + 2 * eta + zeta - 1),
    result(0, 0) = 0.125 * (1 - xsi) * (1 + eta) * (-xsi + eta + 2 * zeta - 1)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 - eta) * (1 - zeta),
    result(0, 0) = -0.5 * xsi * (1 - eta) * (1 - zeta),
    result(0, 0) = -0.25 * (1 + xsi) * (1 - xsi) * (1 - zeta),
    result(0, 0) = -0.25 * (1 + xsi) * (1 - xsi) * (1 - eta)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 + xsi) * (1 + eta) * (1 - eta) * (1 - zeta),
    result(0, 0) = 0.25 * (1 + eta) * (1 - eta) * (1 - zeta),
    result(0, 0) = -0.5 * (1 + xsi) * eta * (1 - zeta),
    result(0, 0) = -0.25 * (1 + xsi) * (1 + eta) * (1 - eta)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 + eta) * (1 - zeta),
    result(0, 0) = -0.5 * xsi * (1 + eta) * (1 - zeta),
    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 - zeta),
    result(0, 0) = -0.25 * (1 + xsi) * (1 - xsi) * (1 + eta)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 - xsi) * (1 + eta) * (1 - eta) * (1 - zeta),
    result(0, 0) = -0.25 * (1 + eta) * (1 - eta) * (1 - zeta),
    result(0, 0) = -0.5 * (1 - xsi) * eta * (1 - zeta),
    result(0, 0) = -0.25 * (1 - xsi) * (1 + eta) * (1 - eta)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 - eta) * (1 + zeta),
    result(0, 0) = -0.5 * xsi * (1 - eta) * (1 + zeta),
    result(0, 0) = -0.25 * (1 + xsi) * (1 - xsi) * (1 + zeta),
    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 - eta)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 + xsi) * (1 + eta) * (1 - eta) * (1 + zeta),
    result(0, 0) = 0.25 * (1 + eta) * (1 - eta) * (1 + zeta),
    result(0, 0) = -0.5 * (1 + xsi) * eta * (1 + zeta),
    result(0, 0) = 0.25 * (1 + xsi) * (1 + eta) * (1 - eta)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 + eta) * (1 + zeta),
    result(0, 0) = -0.5 * xsi * (1 + eta) * (1 + zeta),
    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 + zeta),
    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 + eta)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 - xsi) * (1 + eta) * (1 - eta) * (1 + zeta),
    result(0, 0) = -0.25 * (1 + eta) * (1 - eta) * (1 + zeta),
    result(0, 0) = -0.5 * (1 - xsi) * eta * (1 + zeta),
    result(0, 0) = 0.25 * (1 - xsi) * (1 + eta) * (1 - eta)
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 - xsi) * (1 - eta) * (1 + zeta) * (1 - zeta),
    result(0, 0) = -0.25 * (1 - eta) * (1 + zeta) * (1 - zeta),
    result(0, 0) = -0.25 * (1 - xsi) * (1 + zeta) * (1 - zeta),
    result(0, 0) = -0.5 * (1 - xsi) * (1 - eta) * zeta
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 + xsi) * (1 - eta) * (1 + zeta) * (1 - zeta),
    result(0, 0) = 0.25 * (1 - eta) * (1 + zeta) * (1 - zeta),
    result(0, 0) = -0.25 * (1 + xsi) * (1 + zeta) * (1 - zeta),
    result(0, 0) = -0.5 * (1 + xsi) * (1 - eta) * zeta
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 + xsi) * (1 + eta) * (1 + zeta) * (1 - zeta),
    result(0, 0) = 0.25 * (1 + eta) * (1 + zeta) * (1 - zeta),
    result(0, 0) = 0.25 * (1 + xsi) * (1 + zeta) * (1 - zeta),
    result(0, 0) = -0.5 * (1 + xsi) * (1 + eta) * zeta
    result(0, 0) = 
    result(0, 0) = 
    result(0, 0) = 0.25 * (1 - xsi) * (1 + eta) * (1 + zeta) * (1 - zeta),
    result(0, 0) = -0.25 * (1 + eta) * (1 + zeta) * (1 - zeta),
    result(0, 0) = 0.25 * (1 - xsi) * (1 + zeta) * (1 - zeta),
    result(0, 0) = -0.5 * (1 - xsi) * (1 + eta) * zeta
    result(0, 0) = 
    };
}