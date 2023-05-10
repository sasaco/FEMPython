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
    result(0, 1) = 0.125 * (1 - eta) * (1 - zeta) * (2 * xsi + eta + zeta + 1);
    result(0, 2) = 0.125 * (1 - xsi) * (1 - zeta) * (xsi + 2 * eta + zeta + 1);
    result(0, 3) = 0.125 * (1 - xsi) * (1 - eta) * (xsi + eta + 2 * zeta + 1);

    result(1, 0) = 0.125 * (1 + xsi) * (1 - eta) * (1 - zeta) * (xsi - eta - zeta - 2);
    result(1, 1) = 0.125 * (1 - eta) * (1 - zeta) * (2 * xsi - eta - zeta - 1);
    result(1, 2) = 0.125 * (1 + xsi) * (1 - zeta) * (-xsi + 2 * eta + zeta + 1);
    result(1, 3) = 0.125 * (1 + xsi) * (1 - eta) * (-xsi + eta + 2 * zeta + 1);

    result(2, 0) = 0.125 * (1 + xsi) * (1 + eta) * (1 - zeta) * (xsi + eta - zeta - 2);
    result(2, 1) = 0.125 * (1 + eta) * (1 - zeta) * (2 * xsi + eta - zeta - 1);
    result(2, 2) = 0.125 * (1 + xsi) * (1 - zeta) * (xsi + 2 * eta - zeta - 1);
    result(2, 3) = 0.125 * (1 + xsi) * (1 + eta) * (-xsi - eta + 2 * zeta + 1);

    result(3, 0) = 0.125 * (1 - xsi) * (1 + eta) * (1 - zeta) * (-xsi + eta - zeta - 2);
    result(3, 1) = 0.125 * (1 + eta) * (1 - zeta) * (2 * xsi - eta + zeta + 1);
    result(3, 2) = 0.125 * (1 - xsi) * (1 - zeta) * (-xsi + 2 * eta - zeta - 1);
    result(3, 3) = 0.125 * (1 - xsi) * (1 + eta) * (xsi - eta + 2 * zeta + 1);

    result(4, 0) = 0.125 * (1 - xsi) * (1 - eta) * (1 + zeta) * (-xsi - eta + zeta - 2);
    result(4, 1) = 0.125 * (1 - eta) * (1 + zeta) * (2 * xsi + eta - zeta + 1);
    result(4, 2) = 0.125 * (1 - xsi) * (1 + zeta) * (xsi + 2 * eta - zeta + 1);
    result(4, 3) = 0.125 * (1 - xsi) * (1 - eta) * (-xsi - eta + 2 * zeta - 1);

    result(0, 0) = 0.125 * (1 + xsi) * (1 - eta) * (1 + zeta) * (xsi - eta + zeta - 2);
    result(0, 1) = 0.125 * (1 - eta) * (1 + zeta) * (2 * xsi - eta + zeta - 1);
    result(0, 2) = 0.125 * (1 + xsi) * (1 + zeta) * (-xsi + 2 * eta - zeta + 1);
    result(0, 3) = 0.125 * (1 + xsi) * (1 - eta) * (xsi - eta + 2 * zeta - 1);

    result(0, 0) = 0.125 * (1 + xsi) * (1 + eta) * (1 + zeta) * (xsi + eta + zeta - 2);
    result(0, 1) = 0.125 * (1 + eta) * (1 + zeta) * (2 * xsi + eta + zeta - 1);
    result(0, 2) = 0.125 * (1 + xsi) * (1 + zeta) * (xsi + 2 * eta + zeta - 1);
    result(0, 3) = 0.125 * (1 + xsi) * (1 + eta) * (xsi + eta + 2 * zeta - 1);

    result(0, 0) = 0.125 * (1 - xsi) * (1 + eta) * (1 + zeta) * (-xsi + eta + zeta - 2);
    result(0, 1) = 0.125 * (1 + eta) * (1 + zeta) * (2 * xsi - eta - zeta + 1);
    result(0, 2) = 0.125 * (1 - xsi) * (1 + zeta) * (-xsi + 2 * eta + zeta - 1);
    result(0, 3) = 0.125 * (1 - xsi) * (1 + eta) * (-xsi + eta + 2 * zeta - 1);

    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 - eta) * (1 - zeta);
    result(0, 1) = -0.5 * xsi * (1 - eta) * (1 - zeta);
    result(0, 2) = -0.25 * (1 + xsi) * (1 - xsi) * (1 - zeta);
    result(0, 3) = -0.25 * (1 + xsi) * (1 - xsi) * (1 - eta);

    result(0, 0) = 0.25 * (1 + xsi) * (1 + eta) * (1 - eta) * (1 - zeta);
    result(0, 1) = 0.25 * (1 + eta) * (1 - eta) * (1 - zeta);
    result(0, 2) = -0.5 * (1 + xsi) * eta * (1 - zeta);
    result(0, 3) = -0.25 * (1 + xsi) * (1 + eta) * (1 - eta);

    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 + eta) * (1 - zeta);
    result(0, 1) = -0.5 * xsi * (1 + eta) * (1 - zeta);
    result(0, 2) = 0.25 * (1 + xsi) * (1 - xsi) * (1 - zeta);
    result(0, 3) = -0.25 * (1 + xsi) * (1 - xsi) * (1 + eta);

    result(0, 0) = 0.25 * (1 - xsi) * (1 + eta) * (1 - eta) * (1 - zeta);
    result(0, 1) = -0.25 * (1 + eta) * (1 - eta) * (1 - zeta);
    result(0, 2) = -0.5 * (1 - xsi) * eta * (1 - zeta);
    result(0, 3) = -0.25 * (1 - xsi) * (1 + eta) * (1 - eta);

    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 - eta) * (1 + zeta);
    result(0, 1) = -0.5 * xsi * (1 - eta) * (1 + zeta);
    result(0, 2) = -0.25 * (1 + xsi) * (1 - xsi) * (1 + zeta);
    result(0, 3) = 0.25 * (1 + xsi) * (1 - xsi) * (1 - eta);

    result(0, 0) = 0.25 * (1 + xsi) * (1 + eta) * (1 - eta) * (1 + zeta);
    result(0, 1) = 0.25 * (1 + eta) * (1 - eta) * (1 + zeta);
    result(0, 2) = -0.5 * (1 + xsi) * eta * (1 + zeta);
    result(0, 3) = 0.25 * (1 + xsi) * (1 + eta) * (1 - eta);

    result(0, 0) = 0.25 * (1 + xsi) * (1 - xsi) * (1 + eta) * (1 + zeta);
    result(0, 1) = -0.5 * xsi * (1 + eta) * (1 + zeta);
    result(0, 2) = 0.25 * (1 + xsi) * (1 - xsi) * (1 + zeta);
    result(0, 3) = 0.25 * (1 + xsi) * (1 - xsi) * (1 + eta);

    result(0, 0) = 0.25 * (1 - xsi) * (1 + eta) * (1 - eta) * (1 + zeta);
    result(0, 1) = -0.25 * (1 + eta) * (1 - eta) * (1 + zeta);
    result(0, 2) = -0.5 * (1 - xsi) * eta * (1 + zeta);
    result(0, 3) = 0.25 * (1 - xsi) * (1 + eta) * (1 - eta);

    result(0, 0) = 0.25 * (1 - xsi) * (1 - eta) * (1 + zeta) * (1 - zeta);
    result(0, 1) = -0.25 * (1 - eta) * (1 + zeta) * (1 - zeta);
    result(0, 2) = -0.25 * (1 - xsi) * (1 + zeta) * (1 - zeta);
    result(0, 3) = -0.5 * (1 - xsi) * (1 - eta) * zeta;

    result(0, 0) = 0.25 * (1 + xsi) * (1 - eta) * (1 + zeta) * (1 - zeta);
    result(0, 1) = 0.25 * (1 - eta) * (1 + zeta) * (1 - zeta);
    result(0, 2) = -0.25 * (1 + xsi) * (1 + zeta) * (1 - zeta);
    result(0, 3) = -0.5 * (1 + xsi) * (1 - eta) * zeta;

    result(0, 0) = 0.25 * (1 + xsi) * (1 + eta) * (1 + zeta) * (1 - zeta);
    result(0, 1) = 0.25 * (1 + eta) * (1 + zeta) * (1 - zeta);
    result(0, 2) = 0.25 * (1 + xsi) * (1 + zeta) * (1 - zeta);
    result(0, 3) = -0.5 * (1 + xsi) * (1 + eta) * zeta;

    result(0, 0) = 0.25 * (1 - xsi) * (1 + eta) * (1 + zeta) * (1 - zeta);
    result(0, 1) = -0.25 * (1 + eta) * (1 + zeta) * (1 - zeta);
    result(0, 2) = 0.25 * (1 - xsi) * (1 + zeta) * (1 - zeta);
    result(0, 3) = -0.5 * (1 - xsi) * (1 + eta) * zeta;

    return result;
}

// 要素節点の角度を返す
// p - 要素節点
VectorXd HexaElement2::angle(vector<FENode> p) {
    VectorXd th;
    th << solidAngle(p[0], p[8], p[11], p[16]),
        solidAngle(p[1], p[9], p[8], p[17]),
        solidAngle(p[2], p[10], p[9], p[18]),
        solidAngle(p[3], p[11], p[10], p[19]),
        solidAngle(p[4], p[12], p[15], p[16]),
        solidAngle(p[5], p[13], p[12], p[17]),
        solidAngle(p[6], p[14], p[13], p[18]),
        solidAngle(p[7], p[15], p[14], p[19]),
        planeAngle(p[8], p[12], p[10]), planeAngle(p[9], p[13], p[11]),
        planeAngle(p[10], p[14], p[8]), planeAngle(p[11], p[15], p[9]),
        planeAngle(p[12], p[8], p[14]), planeAngle(p[13], p[9], p[15]),
        planeAngle(p[14], p[10], p[12]), planeAngle(p[15], p[11], p[13]),
        planeAngle(p[16], p[17], p[19]), planeAngle(p[17], p[18], p[16]),
        planeAngle(p[18], p[19], p[17]), planeAngle(p[19], p[16], p[18]);

    return th;
}