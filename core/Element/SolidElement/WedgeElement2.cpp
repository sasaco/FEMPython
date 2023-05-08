#include "WedgeElement2.h"

//--------------------------------------------------------------------//
// 楔形2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
WedgeElement2::WedgeElement2() : SolidElement() {};
WedgeElement2::WedgeElement2(string label, string material, vector<string> nodes) :
    SolidElement(label, material, nodes){

    // 楔形2次要素の節点のξ,η,ζ座標
    WEDGE2_NODE <<  0,   0, -1,
                    1,   0, -1,
                    0,   1, -1,
                    0,   0,  1,
                    1,   0,  1,
                    0,   1,  1,
                  0.5,   0, -1,
                  0.5, 0.5, -1,
                    0, 0.5, -1,
                  0.5,   0,  1,
                  0.5, 0.5,  1,
                    0, 0.5,  1,
                    0,   0,  0,
                    1,   0,  0,
                    0,   1,  0;
    SolidElement::nodeP = WEDGE2_NODE;


    // 楔形2次要素の積分点のξ,η,ζ座標,重み係数
    WEDGE2_INT << GTRI2[0], GTRI2[0], GX3[0], C1_6 * GW3[0],
                  GTRI2[1], GTRI2[0], GX3[0], C1_6 * GW3[0],
                  GTRI2[0], GTRI2[1], GX3[0], C1_6 * GW3[0],
                  GTRI2[0], GTRI2[0], GX3[1], C1_6 * GW3[1],
                  GTRI2[1], GTRI2[0], GX3[1], C1_6 * GW3[1],
                  GTRI2[0], GTRI2[1], GX3[1], C1_6 * GW3[1],
                  GTRI2[0], GTRI2[0], GX3[2], C1_6 * GW3[2],
                  GTRI2[1], GTRI2[0], GX3[2], C1_6 * GW3[2],
                  GTRI2[0], GTRI2[1], GX3[2], C1_6 * GW3[2];
    SolidElement::intP = WEDGE2_INT;

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
MatrixXd WedgeElement2::shapeFunction(double xsi, double eta, double zeta) {

    double xe = 1 - xsi - eta;

    MatrixXd result(15, 4);

    result(0, 0) = xe * (xe - 0.5 * zeta - 1) * (1 - zeta);
    result(0, 1) = -(2 * xe - 0.5 * zeta - 1) * (1 - zeta);
    result(0, 2) = -(2 * xe - 0.5 * zeta - 1) * (1 - zeta);
    result(0, 3) = xe * (zeta - xe + 0.5);

    result(1, 0) = xsi * (xsi - 0.5 * zeta - 1) * (1 - zeta);
    result(1, 1) = (2 * xsi - 0.5 * zeta - 1) * (1 - zeta);
    result(1, 2) = 0;
    result(1, 3) = xsi * (zeta - xsi + 0.5);

    result(2, 0) = eta * (eta - 0.5 * zeta - 1) * (1 - zeta);
    result(2, 1) = 0;
    result(2, 2) = (2 * eta - 0.5 * zeta - 1) * (1 - zeta);
    result(2, 3) = eta * (zeta - eta + 0.5);

    result(3, 0) = xe * (xe + 0.5 * zeta - 1) * (1 + zeta);
    result(3, 1) = -(2 * xe + 0.5 * zeta - 1) * (1 + zeta);
    result(3, 2) = -(2 * xe + 0.5 * zeta - 1) * (1 + zeta);
    result(3, 3) = xe * (zeta + xe - 0.5);

    result(4, 0) = xsi * (xsi + 0.5 * zeta - 1) * (1 + zeta);
    result(4, 1) = (2 * xsi + 0.5 * zeta - 1) * (1 + zeta);
    result(4, 2) = 0;
    result(4, 3) = xsi * (zeta + xsi - 0.5);

    result(5, 0) = eta * (eta + 0.5 * zeta - 1) * (1 + zeta);
    result(5, 1) = 0;
    result(5, 2) = (2 * eta + 0.5 * zeta - 1) * (1 + zeta);
    result(5, 3) = eta * (zeta + eta - 0.5);

    result(6, 0) = 2 * xe * xsi * (1 - zeta);
    result(6, 1) = 2 * (xe - xsi) * (1 - zeta);
    result(6, 2) = -2 * xsi * (1 - zeta);
    result(6, 3) = -2 * xe * xsi;

    result(7, 0) = 2 * xsi * eta * (1 - zeta);
    result(7, 1) = 2 * eta * (1 - zeta);
    result(7, 2) = 2 * xsi * (1 - zeta);
    result(7, 3) = -2 * xsi * eta;

    result(8, 0) = 2 * eta * xe * (1 - zeta);
    result(8, 1) = -2 * eta * (1 - zeta);
    result(8, 2) = 2 * (xe - eta) * (1 - zeta);
    result(8, 3) = -2 * eta * xe;

    result(9, 0) = 2 * xe * xsi * (1 + zeta);
    result(9, 1) = 2 * (xe - xsi) * (1 + zeta);
    result(9, 2) = -2 * xsi * (1 + zeta);
    result(9, 3) = 2 * xe * xsi;

    result(10, 0) = 2 * xsi * eta * (1 + zeta);
    result(10, 1) = 2 * eta * (1 + zeta);
    result(10, 2) = 2 * xsi * (1 + zeta);
    result(10, 3) = 2 * xsi * eta;

    result(11, 0) = 2 * eta * xe * (1 + zeta);
    result(11, 1) = -2 * eta * (1 + zeta);
    result(11, 2) = 2 * (xe - eta) * (1 + zeta);
    result(11, 3) = 2 * eta * xe;

    result(12, 0) = xe * (1 + zeta) * (1 - zeta);
    result(12, 1) = -(1 + zeta) * (1 - zeta);
    result(12, 2) = -(1 + zeta) * (1 - zeta);
    result(12, 3) = -2 * xe * zeta;

    result(13, 0) = xsi * (1 + zeta) * (1 - zeta);
    result(13, 1) = (1 + zeta) * (1 - zeta);
    result(13, 2) = 0;
    result(13, 3) = -2 * xsi * zeta;

    result(14, 0) = eta * (1 + zeta) * (1 - zeta);
    result(14, 1) = 0;
    result(14, 2) = (1 + zeta) * (1 - zeta);
    result(14, 3) = -2 * eta * zeta;

    return result;

}
