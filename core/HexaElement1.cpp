#include "HexaElement1.h"

//--------------------------------------------------------------------//
// 六面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
HexaElement1::HexaElement1() : SolidElement() {}
HexaElement1::HexaElement1(int label, int material, vector<int> nodes) : 
    SolidElement(label, material, nodes) {

    // 六面体1次要素の節点のξ,η,ζ座標
    HEXA1_NODE << -1, -1, -1,
                   1, -1, -1,
                   1,  1, -1,
                  -1,  1, -1,
                  -1, -1,  1,
                   1, -1,  1,
                   1,  1,  1,
                  -1,  1,  1;
    SolidElement::nodeP = HEXA1_NODE;

    // 六面体1次要素の積分点のξ,η,ζ座標,重み係数
    HEXA1_INT << FElement::GX2[0], FElement::GX2[0], FElement::GX2[0], 1,
                 FElement::GX2[1], FElement::GX2[0], FElement::GX2[0], 1,
                 FElement::GX2[0], FElement::GX2[1], FElement::GX2[0], 1,
                 FElement::GX2[1], FElement::GX2[1], FElement::GX2[0], 1,
                 FElement::GX2[0], FElement::GX2[0], FElement::GX2[1], 1,
                 FElement::GX2[1], FElement::GX2[0], FElement::GX2[1], 1,
                 FElement::GX2[0], FElement::GX2[1], FElement::GX2[1], 1,
                 FElement::GX2[1], FElement::GX2[1], FElement::GX2[1], 1;
    SolidElement::intP = HEXA1_INT;

    // 六面体1次要素の質量マトリックス係数
    for (int i = 0; i < 8; i++) {
        for (int j = 0; j < 8; j++) {
            double s = abs(HEXA1_NODE(i, 0) - HEXA1_NODE(j, 0)) +
                abs(HEXA1_NODE(i, 1) - HEXA1_NODE(j, 1)) +
                abs(HEXA1_NODE(i, 2) - HEXA1_NODE(j, 2));
            HEXA1_MASS_BASE(i, j) = pow(0.5, 0.5 * s) / 27;
        }
    }

}


// 要素名称を返す
string HexaElement1::getName() {
    return "HexaElement1";
} 


// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
MatrixXd HexaElement1::shapeFunction(double xsi, double eta, double zeta) {

    MatrixXd result(8, 4);

    result(0, 0) = 0.125 * (1 - xsi) * (1 - eta) * (1 - zeta);
    result(0, 1) = -0.125 * (1 - eta) * (1 - zeta);
    result(0, 2) = -0.125 * (1 - xsi) * (1 - zeta);
    result(0, 3) = -0.125 * (1 - xsi) * (1 - eta);

    result(1, 0) = 0.125 * (1 + xsi) * (1 - eta) * (1 - zeta);
    result(1, 1) = 0.125 * (1 - eta) * (1 - zeta);
    result(1, 2) = -0.125 * (1 + xsi) * (1 - zeta);
    result(1, 3) = -0.125 * (1 + xsi) * (1 - eta);

    result(2, 0) = 0.125 * (1 + xsi) * (1 + eta) * (1 - zeta);
    result(2, 1) = 0.125 * (1 + eta) * (1 - zeta);
    result(2, 2) = 0.125 * (1 + xsi) * (1 - zeta);
    result(2, 3) = -0.125 * (1 + xsi) * (1 + eta);

    result(3, 0) = 0.125 * (1 - xsi) * (1 + eta) * (1 - zeta);
    result(3, 1) = -0.125 * (1 + eta) * (1 - zeta);
    result(3, 2) = 0.125 * (1 - xsi) * (1 - zeta);
    result(3, 3) = -0.125 * (1 - xsi) * (1 + eta);

    result(4, 0) = 0.125 * (1 - xsi) * (1 - eta) * (1 + zeta);
    result(4, 1) = -0.125 * (1 - eta) * (1 + zeta);
    result(4, 2) = -0.125 * (1 - xsi) * (1 + zeta);
    result(4, 3) = 0.125 * (1 - xsi) * (1 - eta);

    result(5, 0) = 0.125 * (1 + xsi) * (1 - eta) * (1 + zeta);
    result(5, 1) = 0.125 * (1 - eta) * (1 + zeta);
    result(5, 2) = -0.125 * (1 + xsi) * (1 + zeta);
    result(5, 3) = 0.125 * (1 + xsi) * (1 - eta);

    result(6, 0) = 0.125 * (1 + xsi) * (1 + eta) * (1 + zeta);
    result(6, 1) = 0.125 * (1 + eta) * (1 + zeta);
    result(6, 2) = 0.125 * (1 + xsi) * (1 + zeta);
    result(6, 3) = 0.125 * (1 + xsi) * (1 + eta);

    result(7, 0) = 0.125 * (1 - xsi) * (1 + eta) * (1 + zeta);
    result(7, 1) = -0.125 * (1 + eta) * (1 + zeta);
    result(7, 2) = 0.125 * (1 - xsi) * (1 + zeta);
    result(7, 3) = 0.125 * (1 - xsi) * (1 + eta);


    return result;
}


// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
MatrixXd HexaElement1::massMatrix(vector<FENode> p, double dens) {

    double ja = 0;
    for (int i = 0; i < 8; i++) {
        MatrixXd sf = shapeFunction(HEXA1_INT(i, 0), HEXA1_INT(i, 1),
            HEXA1_INT(i, 2));
        ja += abs(jacobianMatrix(p, sf).determinant());
    }

    double coef = dens * ja;
    MatrixXd m = MatrixXd::Zero(24, 24);

    for (int i = 0; i < 8; i++) {
        int i3 = 3 * i;
        for (int j = 0; j < 8; j++) {
            double value = coef * HEXA1_MASS_BASE(i, j);
            int j3 = 3 * j;
            m(i3, j3) += value;
            m(i3 + 1, j3 + 1) += value;
            m(i3 + 2, j3 + 2) += value;
        }
    }
    return m;
}

