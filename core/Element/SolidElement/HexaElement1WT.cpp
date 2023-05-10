#include "HexaElement1WT.h";

//--------------------------------------------------------------------//
// Wilson-Taylor非適合六面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
HexaElement1WT::HexaElement1WT() : HexaElement1() {}
HexaElement1WT::HexaElement1WT(string label, string material, vector<string> nodes) :
    HexaElement1(label, material, nodes) {
    // te.clear();		// 非適合モードの変換マトリックス
}


// 要素境界名称を返す
string HexaElement1WT::getName() {
    return "HexaElement1WT";
}


// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
MatrixXd HexaElement1WT::stiffnessMatrix(vector<FENode> p, MatrixXd d1) {
    int size = 3 * nodeCount();
    MatrixXd kk = MatrixXd::Zero(size, size);
    MatrixXd k2 = MatrixXd::Zero(size, 9);
    MatrixXd k3 = MatrixXd::Zero(9, 9);

    auto sf0 = shapeFunction(0, 0, 0);
    auto ja0 = jacobianMatrix(p, sf0);
    auto ji0 = ja0.inverse();
    double jj0 = abs(ja0.determinant());

    for (int i = 0; i < intP.rows(); i++) {
        VectorXd sf = shapeFunction(intP(i, 0), intP(i, 1), intP(i, 2));
        MatrixXd ja = jacobianMatrix(p, sf);
        MatrixXd gr = grad(p, ja, sf);
        MatrixXd b = strainMatrix(gr);
        MatrixXd b0 = strainMatrix2(ji0, intP.row(i));
        MatrixXd ks = stiffPart(d1, b, intP(i, 3) * abs(ja.determinant()));
        double cf0 = intP(i, 3) * jj0;
        kk += ks;
        k2 += stiffPart2(d1, b, b0, cf0);
        k3 += stiffPart(d1, b0, cf0);
    }
    auto k4 = k3.inverse() * k2.transpose();
    kk += k2 * k4;
    te = k4.transpose();
    return kk;
};


// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// x - ξ,η,ζ座標
VectorXd HexaElement1WT::strainPart(vector<FENode> p, VectorXd v, VectorXd x) {
    auto sf = shapeFunction(x[0], x[1], x[2]);
    auto ja = jacobianMatrix(p, sf);
    auto ji = ja.inverse();
    auto gr = grad(p, ja, sf);
    auto sm = strainMatrix(gr);
    auto sm0 = te * strainMatrix2(ji, x);
    int count = 3 * nodeCount();

    VectorXd eps(6);
    for (int j = 0; j < 6; j++) {
        double s = 0;
        for (int k = 0; k < count; k++) {
            s += v[k] * (sm(k, j) + sm0(k, j));
        }
        eps[j] = s;
    }
    return eps;
};

// 非適合モードの歪 - 変位マトリックスの転置行列を返す
// ji - ヤコビ逆行列の要素配列
// x - ξ,η,ζ座標
MatrixXd HexaElement1WT::strainMatrix2(VectorXd ji, VectorXd x){

    MatrixXd m = MatrixXd::Zero(9, 6);

    double d1 = -2 * x[0];
    double d2 = -2 * x[1];
    double d3 = -2 * x[2];

    MatrixXd gr;
    gr << ji[0] * d1, ji[1] * d1, ji[2] * d1, 
          ji[3] * d2, ji[4] * d2, ji[5] * d2,
          ji[6] * d3, ji[7] * d3, ji[8] * d3;

    for (int i = 0; i < 3; i++) {
        int i3 = 3 * i;

        auto gx = gr(i, 0);
        auto gy = gr(i, 1);
        auto gz = gr(i, 2);

        m(i3,     0) = gx;
        m(i3 + 1, 1) = gy;
        m(i3 + 2, 2) = gz;
        m(i3,     3) = gy;
        m(i3 + 1, 3) = gx;
        m(i3 + 1, 4) = gz;
        m(i3 + 2, 4) = gy;
        m(i3,     5) = gz;
        m(i3 + 2, 5) = gx;
    }
    return m;
};

// 積分点の剛性マトリックス [B1t][D][B2]を返す
// d - 応力-歪マトリックス
// b1 - 歪-変位マトリックスの転置行列 B1t
// b2 - 歪-変位マトリックスの転置行列 B2t
// coef - 係数
VectorXd HexaElement1WT::stiffPart2(MatrixXd d, MatrixXd b1, MatrixXd b2, double coef){

    int size1 = b1.rows();
    int size2 = d.rows();
    int size3 = b2.rows();

    MatrixXd k(size1, size1);
    for (int i = 0; i < size1; i++) {

        MatrixXd a(size2, size2);
        for (int j = 0; j < size2; j++) {
            a.row(j) = b1.row(i) * d.row(j);
        }

        MatrixXd ki(size3, size3);
        for (int j = 0; j < size3; j++) {
            ki.row(j) = a * b2.row(j);
        }
        k.row(i) = ki;
    }
    return k;
}


