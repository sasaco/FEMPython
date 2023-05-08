#include "WedgeElement1.h";

//--------------------------------------------------------------------//
// 楔形1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
WedgeElement1::WedgeElement1() : SolidElement(){}
WedgeElement1::WedgeElement1(string label, string material, vector<string> nodes) :
    SolidElement(label, material, nodes) {

    // 楔形1次要素の節点のξ,η,ζ座標
    WEDGE1_NODE << 0, 0, -1,
                   1, 0, -1,
                   0, 1, -1,
                   0, 0,  1,
                   1, 0,  1,
                   0, 1,  1;
    SolidElement::nodeP = WEDGE1_NODE;

    // 楔形1次要素の積分点のξ,η,ζ座標,重み係数
    WEDGE1_INT << C1_3, C1_3, GX2[0], 0.5,
                  C1_3, C1_3, GX2[1], 0.5;
    SolidElement::intP = WEDGE1_INT;
};

// 要素名称を返す
string WedgeElement1::getName() {
    return "WedgeElement1";
};


// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
MatrixXd WedgeElement1::shapeFunction(double xsi, double eta, double zeta) {

    MatrixXd result(6, 4);

    result(0, 0) = 0.5 * (1 - xsi - eta) * (1 - zeta);
    result(0, 1) = -0.5 * (1 - zeta);
    result(0, 2) = -0.5 * (1 - zeta); 
    result(0, 3) = -0.5 * (1 - xsi - eta);

    result(1, 0) = 0.5 * xsi * (1 - zeta);
    result(1, 1) = 0.5 * (1 - zeta);
    result(1, 2) = 0;
    result(1, 3) = -0.5 * xsi;

    result(2, 0) = 0.5 * eta * (1 - zeta);
    result(2, 1) = 0;
    result(2, 2) = 0.5 * (1 - zeta);
    result(2, 3) = -0.5 * eta;

    result(3, 0) = 0.5 * (1 - xsi - eta) * (1 + zeta);
    result(3, 1) = -0.5 * (1 + zeta);
    result(3, 2) = -0.5 * (1 + zeta);
    result(3, 3) = 0.5 * (1 - xsi - eta);

    result(4, 0) = 0.5 * xsi * (1 + zeta);
    result(4, 1) = 0.5 * (1 + zeta);
    result(4, 2) = 0;
    result(4, 3) = 0.5 * xsi;

    result(5, 0) = 0.5 * eta * (1 + zeta);
    result(5, 1) = 0;
    result(5, 2) = 0.5 * (1 + zeta);
    result(5, 3) = 0.5 * eta;

    return result;
}


// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
MatrixXd WedgeElement1::massMatrix(vector<FENode> p, double dens) {
    
    double ja = 0;

    for (int i = 0; i < 2; i++) {
        auto sf = shapeFunction(WEDGE1_INT(i, 0), WEDGE1_INT(i, 1), WEDGE1_INT(i, 2));
        auto jm = jacobianMatrix(p, sf);
        double det = jm.determinant();
        ja += det;
    }

    MatrixXd result = MatrixXd::Zero(18, 18);

    double v0 = dens * ja / 36;
    for (int i = 0; i < 6; i++) {
        for (int j = 0; j < 6; j++) {
            double value = v0;
            int i3 = 3 * i;
            int j3 = 3 * j;

            if (i % 3 != j % 3) value *= 0.5;

            if (floor(i / 3) != floor(j / 3)) value *= 0.5;

            result(i3, j3) += value;
            result(i3 + 1, j3 + 1) += value;
            result(i3 + 2, j3 + 2) += value;
        }
    }

    return result;
}
