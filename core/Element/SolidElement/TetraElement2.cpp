#include "TetraElement2.h";

//--------------------------------------------------------------------//
// 四面体2次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
TetraElement2::TetraElement2() : SolidElement() {}
TetraElement2::TetraElement2(string label, string material, vector<string> nodes) :
    SolidElement(label, material, nodes) {

    // 四面体2次要素の節点のξ,η,ζ座標
    TETRA2_NODE << 0, 0, 0,
                   1, 0, 0,
                   0, 1, 0,
                   0, 0, 1,
                   0.5, 0, 0,
                   0.5, 0.5, 0,
                   0, 0.5, 0,
                   0, 0, 0.5,
                   0.5, 0, 0.5,
                   0, 0.5, 0.5;

    SolidElement::nodeP = TETRA2_NODE;

    // 四面体2次要素の積分点のξ,η,ζ座標,重み係数
    TETRA2_INT << GTETRA2[0], GTETRA2[0], GTETRA2[0], C1_24,
        GTETRA2[1], GTETRA2[0], GTETRA2[0], C1_24,
        GTETRA2[0], GTETRA2[1], GTETRA2[0], C1_24,
        GTETRA2[0], GTETRA2[0], GTETRA2[1], C1_24;

    SolidElement::intP = TETRA2_INT;

};


// 要素名称を返す
string TetraElement2::getName() {
    return "TetraElement2";
}


// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
MatrixXd TetraElement2::shapeFunction(double xsi, double eta, double zeta) {
    
    double xez = 1 - xsi - eta - zeta;

    MatrixXd result(10, 4);

    result(0, 0) = xez * (2 * xez - 1);
    result(0, 1) = 1 - 4 * xez;
    result(0, 2) = 1 - 4 * xez;
    result(0, 3) = 1 - 4 * xez;

    result(1, 0) = xsi * (2 * xsi - 1);
    result(1, 1) = 4 * xsi - 1;
    result(1, 2) = 0;
    result(1, 3) = 0;

    result(2, 0) = eta * (2 * eta - 1);
    result(2, 1) = 0;
    result(2, 2) = 4 * eta - 1;
    result(2, 3) = 0;

    result(3, 0) = zeta * (2 * zeta - 1);
    result(3, 1) = 0;
    result(3, 2) = 0;
    result(3, 3) = 4 * zeta - 1;

    result(4, 0) = 4 * xez * xsi;
    result(4, 1) = 4 * (xez - xsi);
    result(4, 2) = -4 * xsi;
    result(4, 3) = -4 * xsi;

    result(5, 0) = 4 * xsi * eta;
    result(5, 1) = 4 * eta;
    result(5, 2) = 4 * xsi;
    result(5, 3) = 0;

    result(6, 0) = 4 * xez * eta;
    result(6, 1) = -4 * eta;
    result(6, 2) = 4 * (xez - eta);
    result(6, 3) = -4 * eta;

    result(7, 0) = 4 * xez * zeta;
    result(7, 1) = -4 * zeta;
    result(7, 2) = -4 * zeta;
    result(7, 3) = 4 * (xez - zeta);

    result(8, 0) = 4 * xsi * zeta;
    result(8, 1) = 4 * zeta;
    result(8, 2) = 0;
    result(8, 3) = 4 * xsi;

    result(9, 0) = 4 * eta * zeta;
    result(9, 1) = 0;
    result(9, 2) = 4 * zeta;
    result(9, 3) = 4 * eta;

    return result;
}

// 要素節点の角度を返す
// p - 要素節点
VectorXd TetraElement2::angle(vector<FENode> p) {

    VectorXd th;
    th << solidAngle(p[0], p[4], p[6], p[7]),
          solidAngle(p[1], p[5], p[4], p[8]),
          solidAngle(p[2], p[6], p[5], p[9]),
          solidAngle(p[3], p[7], p[8], p[9]),
          planeAngle(p[4], p[2], p[3]),
          planeAngle(p[5], p[0], p[3]),
          planeAngle(p[6], p[1], p[3]), 
          planeAngle(p[7], p[1], p[2]),
          planeAngle(p[8], p[2], p[0]),
          planeAngle(p[9], p[0], p[1]);

    return th;
}