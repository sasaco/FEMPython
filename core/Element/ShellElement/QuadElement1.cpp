#include "QuadElement1.h"

//--------------------------------------------------------------------//
// 四角形1次要素 (MITC4)
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
QuadElement1::QuadElement1() : ShellElement() {}
QuadElement1::QuadElement1(string label, string material, string param, vector<string> nodes) :
    ShellElement(label, material, param, nodes){ 

    // 四角形1次要素の節点のξ,η座標
    QUAD1_NODE << -1, -1,
                   1, -1,
                   1,  1,
                  -1,  1;
    ShellElement::nodeP = QUAD1_NODE;

    // 四角形1次要素の積分点のξ,η座標,重み係数
    QUAD1_INT << GX2[0], GX2[0], 1,
                 GX2[1], GX2[0], 1,
                 GX2[0], GX2[1], 1,
                 GX2[1], GX2[1], 1;
    ShellElement::intP = QUAD1_INT;
};

// 要素名称を返す
string QuadElement1::getName() {
    return "QuadElement1";
};


// 形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
MatrixXd QuadElement1::shapeFunction(double xsi, double eta) {

    MatrixXd result;

    result << 0.25 * (1 - xsi) * (1 - eta), -0.25 * (1 - eta), -0.25 * (1 - xsi),
              0.25 * (1 + xsi) * (1 - eta), 0.25 * (1 - eta), -0.25 * (1 + xsi),
              0.25 * (1 + xsi) * (1 + eta), 0.25 * (1 + eta), 0.25 * (1 + xsi),
              0.25 * (1 - xsi) * (1 + eta), -0.25 * (1 + eta), 0.25 * (1 - xsi);

    return result;
}

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
// t - 要素厚さ
MatrixXd QuadElement1::massMatrix(vector<FENode> p, double dens, double t) {

    int count = nodeCount();
    
    MatrixXd m = MatrixXd::Zero(6 * count, 6 * count);

    MatrixXd d = dirMatrix(p);
    
    auto n = normalVector(p);

    double tt = C1_12 * t * t;

    for (int i = 0; i < intP.rows(); i++) {

        MatrixXd sf = shapeFunction(intP(i, 0), intP(i, 1));

        VectorXd nn;
        nn << sf(0, 0), sf(1, 0), sf(2, 0), sf(3, 0);

        MatrixXd ja = jacobianMatrix(p, sf, n, t);

        double det = ja.determinant();

        double jac = abs(det);

        jac *= 2 * intP(i, 2);

        for (int i1 = 0; i1 < count; i1++) {

            int i6 = 6 * i1;
            
            double nja = nn[i1] * jac;

            for (int j1 = 0; j1 < count; j1++) {

                int j6 = 6 * j1;
                
                double nnja = nja * nn[j1];
                
                double dm = dens * nnja;
                
                double dii = tt * dm;

                m(i6, j6) += dm;
                m(i6 + 1, j6 + 1) += dm;
                m(i6 + 2, j6 + 2) += dm;
                m(i6 + 3, j6 + 3) += dii;
                m(i6 + 4, j6 + 4) += dii;
                m(i6 + 5, j6 + 5) += 1e-4 * dii;
            }
        }
    }
    toDir3(d, m);
    return m;
}

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
MatrixXd QuadElement1::stiffnessMatrix(vector<FENode> p, MatrixXd d1, ShellParameter sp) {
    
    int size = 6 * nodeCount();
    
    MatrixXd kk = MatrixXd::Zero(size, size);

    auto n = normalVector(p);
    
    double t = sp.thickness;

    for (int i = 0; i < intP.rows(); i++) {

        MatrixXd ks = stiffPart(p, d1, n, intP(i, 0), intP(i, 1), t);

        kk += ks;
    }
    return kk;
};

// 積分点の剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// n - 法線ベクトル
// xsi,eta - 要素内部ξ,η座標
// t - 要素厚さ
MatrixXd QuadElement1::stiffPart(vector<FENode> p, MatrixXd d1, Vector3Dim n,
                                            double xsi, double eta, double t) {

    MatrixXd d = dirMatrix(p);

    MatrixXd sf = shapeFunction(xsi, eta);

    MatrixXd ja = jacobianMatrix(p, sf, n, t);

    MatrixXd bc0 = strainMatrix1(ja, sf, d);

    MatrixXd sf1 = shapeFunction(xsi, 0);

    MatrixXd ja1 = jacobianMatrix(p, sf1, n, t);

    MatrixXd sf2 = shapeFunction(0, eta);

    MatrixXd ja2 = jacobianMatrix(p, sf2, n, t);

    vector<MatrixXd> bc = {
        strainMatrix1(ja1, sf1, d), 
        strainMatrix1(ja2, sf2, d)
    };

    int count = nodeCount();

    MatrixXd kk = MatrixXd::Zero(6 * count, 6 * count);

    double det = ja.determinant();

    double jacob = abs(det);

    double tt6 = t * t / 6.0;
    
    double ce1 = 1e-3 * t * t * d1(3, 3);
    
    double ce2 = -ce1 / (count - 1), j1, j2;

    for (int i = 0; i < count; i++) {
        for (int j = 0; j < count; j++) {

            MatrixXd k1 = MatrixXd::Zero(3, 3);
            MatrixXd k2 = MatrixXd::Zero(3, 3);
            MatrixXd k3 = MatrixXd::Zero(3, 3);
            MatrixXd k4 = MatrixXd::Zero(3, 3);

            for (int j1 = 0; j1 < 2; j1++) {
                for (int j2 = 0; j2 < 2; j2++) {
                    k1(j1, j2) = bc0(i, j1) * d1(j1, j2) * bc0(j, j2) +
                        bc0(i, 1 - j1) * d1(2, 2) * bc0(j, 1 - j2);
                }
                double dd = d1(4 - j1, 4 - j1);
                k1(j1, j1) += bc[1 - j1](i, 2)  * dd * bc[1 - j1](j, 2);
                k1(j1, 2)   = bc[1 - j1](i, 2)  * dd * bc[j1](j, j1);
                k1(2, j1)   = bc[j1](i, j1)     * dd * bc[1 - j1](j, 2);
                k2(j1, j1)  = bc[1 - j1](i, 2)  * dd * bc[1 - j1](j, 3);
                k2(2, j1)   = bc[1 - j1](i, j1) * dd * bc[1 - j1](j, 3);
                k3(j1, j1)  = bc[1 - j1](i, 3)  * dd * bc[1 - j1](j, 2);
                k3(j1, 2)   = bc[1 - j1](i, 3)  * dd * bc[1 - j1](j, j1);
            }
            k1(2, 2) = bc[0](i, 1) * d1(3, 3) * bc[0](j, 1) +
                bc[1](i, 0)* d1(4, 4)* bc[1](j, 0);
            k4(0, 0) = k1(1, 1) + 3 * bc[0](i, 3)* d1(3, 3)* bc[0](j, 3);
            k4(0, 1) = -k1(1, 0);
            k4(1, 0) = -k1(0, 1);
            k4(1, 1) = k1(0, 0) + 3 * bc[1](i, 3)* d1(4, 4)* bc[1](j, 3);
            for (j1 = 0; j1 < 3; j1++) {
                double kt = k2(j1, 0);
                k2(j1, 0) = -k2(j1, 1);
                k2(j1, 1) = kt;
                kt = k3(0, j1);
                k3(0, j1) = -k3(1, j1);
                k3(1, j1) = kt;
            }

            if (i == j) k4(2, 2) = ce1;
            else     k4(2, 2) = ce2;
            toDir(d, k1);
            toDir(d, k2);
            toDir(d, k3);
            toDir(d, k4);
            int i0 = 6 * i;
            int j0 = 6 * j;
            for (j1 = 0; j1 < 3; j1++) {
                for (j2 = 0; j2 < 3; j2++) {
                    kk(i0 + j1, j0 + j2) = 2 * jacob * k1(j1, j2);
                    kk(i0 + j1, j0 + 3 + j2) = t * jacob * k2(j1, j2);
                    kk(i0 + 3 + j1, j0 + j2) = t * jacob * k3(j1, j2);
                    kk(i0 + 3 + j1, j0 + 3 + j2) = tt6 * jacob * k4(j1, j2);
                }
            }
        }
    }
    return kk;
};
