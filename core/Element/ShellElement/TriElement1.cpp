#include "TriElement1.h";

//--------------------------------------------------------------------//
// 三角形1次要素 (薄肉シェル)
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
TriElement1::TriElement1() : ShellElement() {}
TriElement1::TriElement1(string label, string material, string param, vector<string> nodes) :
                ShellElement(label, material, param, nodes){

    // 三角形1次要素の節点のξ,η座標
    TRI1_NODE << 0, 0,
                 1, 0,
                 0, 1;
    ShellElement::nodeP = TRI1_NODE;

    // 三角形1次要素の積分点のξ,η座標,重み係数
    TRI1_INT << C1_3, C1_3, 0.5;
    ShellElement::intP = TRI1_INT;


    // 三角形2次要素の積分点のξ,η座標,重み係数
    TRI2_INT << GTRI2[0], GTRI2[0], C1_6,
                GTRI2[1], GTRI2[0], C1_6,
                GTRI2[0], GTRI2[1], C1_6;

    // 三角形1次要素の質量マトリックス係数
    TRI1_MASS1 << 1, 0.5, 0.5,
                  0.5, 1, 0.5,
                  0.5, 0.5, 1;

}


// 要素名称を返す
string TriElement1::getName() {
    return "TriElement1";
};


// １次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
MatrixXd TriElement1::shapeFunction(double xsi, double eta) {
    MatrixXd result;
    result << 1 - xsi - eta, -1, -1,
              xsi,            1,  0, 
              eta,            0,  1;
    return result;
}


// ２次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
MatrixXd TriElement1::shapeFunction2(double xsi, double eta) {
    double xe = 1 - xsi - eta;

    MatrixXd result;
    result <<  xe * (2 * xe - 1),    1 - 4 * xe,     1 - 4 * xe,
               xsi * (2 * xsi - 1),  4 * xsi - 1,    0,
               eta * (2 * eta - 1),  0,              4 * eta - 1, 
               4 * xe * xsi,         4 * (xe - xsi), -4 * xsi,
               4 * xsi * eta,        4 * eta,        4 * xsi, 
               4 * xe * eta,         -4 * eta,       4 * (xe - eta);
    return result;
}


// 角度の形状関数行列 [ Hxi Hyi dHxi/dξ dHyi/dξ dHxi/dη dHyi/dη ] を返す
// p - 要素節点
// d - 方向余弦マトリックス
// xsi,eta - 要素内部ξ,η座標
MatrixXd TriElement1::shapeFunction3(vector<FENode> p, MatrixXd d, double xsi, double eta) {

    int count = nodeCount();
    auto sf2 = shapeFunction2(xsi, eta);

    FENode p1;
    p[1].clone(p1);
    auto d12 = p1.sub(p[0]);

    FENode p2;
    p[2].clone(p2);
    auto d23 = p2.sub(p[1]);

    FENode p0;
    p[0].clone(p0);
    auto d31 = p0.sub(p[2]);

    vector<double> l = {
        1 / d12.lengthSq(),
        1 / d23.lengthSq(),
        1 / d31.lengthSq()
    };

    double x[] = {
        d(0, 0) * d12.x + d(1, 0) * d12.y + d(2, 0) * d12.z,
        d(0, 0) * d23.x + d(1, 0) * d23.y + d(2, 0) * d23.z,
        d(0, 0) * d31.x + d(1, 0) * d31.y + d(2, 0) * d31.z
    };

    double y[] = {
        d(0, 1) * d12.x + d(1, 1) * d12.y + d(2, 1) * d12.z,
        d(0, 1) * d23.x + d(1, 1) * d23.y + d(2, 1) * d23.z,
        d(0, 1) * d31.x + d(1, 1) * d31.y + d(2, 1) * d31.z
    };

    double a[] = {
        1.5 * l[0] * y[0],
        1.5 * l[1] * y[1],
        1.5 * l[2] * y[2]
    };

    double b[] = {
        -1.5 * l[0] * x[0],
        -1.5 * l[1] * x[1],
        -1.5 * l[2] * x[2]
    };

    double c[] = {
        0.75 * l[0] * y[0] * y[0] - 0.5,
        0.75 * l[1] * y[1] * y[1] - 0.5,
        0.75 * l[2] * y[2] * y[2] - 0.5
    };

    double d1[] = {
        0.75 * l[0] * x[0] * y[0],
        0.75 * l[1] * x[1] * y[1],
        0.75 * l[2] * x[2] * y[2]
    };

    double e[] = {
        0.25 - 0.75 * l[0] * y[0] * y[0],
        0.25 - 0.75 * l[1] * y[1] * y[1],
        0.25 - 0.75 * l[2] * y[2] * y[2]
    };


    MatrixXd m(3 * count, 6);

    for (int i = 0; i < 3; i++) {
        int i1 = (i + 2) % 3;
        int i3 = 3 * i;

        for (int j = 0; j < 3; j++) {
            int j2 = 2 * j;
            m(i3, j2) = a[i1] * sf2(3 + i1, j) - a[i] * sf2(3 + i, j);
            m(i3, j2 + 1) = b[i1] * sf2(3 + i1, j) - b[i] * sf2(3 + i, j);
            m(i3 + 1, j2) = sf2(i, j) - c[i1] * sf2(3 + i1, j) - c[i] * sf2(3 + i, j);
            double dn = d1[i1] * sf2(3 + i1, j) + d1[i] * sf2(3 + i, j);
            m(i3 + 1, j2 + 1) = dn;
            m(i3 + 2, j2) = dn;
            m(i3 + 2, j2 + 1) = sf2(i, j) - e[i1] * sf2(3 + i1, j) - e[i] * sf2(3 + i, j);
        }
    }
    return m;
}


// ヤコビアンを返す
// p - 要素節点
double TriElement1::jacobian(vector<FENode> p) {

    double p0x = p[0].x;
    double p0y = p[0].y;
    double p0z = p[0].z;

    double j1 = (p[1].y - p0y) * (p[2].z - p0z) - (p[1].z - p0z) * (p[2].y - p0y);
    double j2 = (p[1].z - p0z) * (p[2].x - p0x) - (p[1].x - p0x) * (p[2].z - p0z);
    double j3 = (p[1].x - p0x) * (p[2].y - p0y) - (p[1].y - p0y) * (p[2].x - p0x);

    return sqrt(j1 * j1 + j2 * j2 + j3 * j3);
};


// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
// t - 要素厚さ
MatrixXd TriElement1::massMatrix(vector<FENode> p, double dens, double t) {
    
    int count = nodeCount();
    
    MatrixXd m(6 * count, 6 * count);

    MatrixXd mb(3 * count, 3 * count);
    
    MatrixXd d = dirMatrix(p);

    double djt = dens * t * jacobian(p);
    
    double tt = C1_12 * t * t;
    
    double dm = C1_12 * djt;

    for (int k = 0; k < 3; k++) {

        MatrixXd sf3 = shapeFunction3(p, d, TRI2_INT(k, 0), TRI2_INT(k, 1));

        MatrixXd sf = shapeFunction(TRI2_INT(k, 0), TRI2_INT(k, 1));

        vector<double> hz = {
            sf(0, 0), 0, 0, sf(1, 0), 0, 0, sf(2, 0), 0, 0
        };

        double cfm = djt * TRI2_INT(k, 2);

        for (int i = 0; i < 3 * count; i++) {
            for (int j = 0; j < 3 * count; j++) {
                double hxhy = sf3(i, 0) * sf3(j, 0) + sf3(i, 1) * sf3(j, 1);
                mb(i, j) += cfm * (tt * hxhy + hz[i] * hz[j]);
            }
        }
    }

    for (int i = 0; i < count; i++) {
        int i3 = 3 * i;
        int i6 = 6 * i;
        for (int j = 0; j < count; j++) {
            int j3 = 3 * j;
            int j6 = 6 * j;
            double cf1 = TRI1_MASS1(i, j);
            double dme = cf1 * dm;
            m(i6, j6) = dme;
            m(i6 + 1, j6 + 1) = dme;
            for (int i1 = 0; i1 < 3; i1++) {
                for (int j1 = 0; j1 < 3; j1++) {
                    m(i6 + 2 + i1, j6 + 2 + j1) = mb(i3 + i1, j3 + j1);
                }
            }
            m(i6 + 5, j6 + 5) = 1e-5 * dm * tt;
        }
    }
    toDir3(d, m);
    return m;
}


// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
MatrixXd TriElement1::stiffnessMatrix(vector<FENode> p, MatrixXd d1, ShellParameter sp) {

    MatrixXd d = dirMatrix(p);

    auto n = normalVector(p);
    double t = sp.thickness;

    MatrixXd sf1 = shapeFunction(C1_3, C1_3);
    MatrixXd ja1 = jacobianMatrix(p, sf1, n, t);
    double jac1 = ja1.determinant();
    MatrixXd jinv = jacobInv(ja1, d);
    MatrixXd b1 = strainMatrix1(sf1, jinv);
    MatrixXd k1 = stiffPart(d1, b1, abs(jac1));

    int count = nodeCount();
    MatrixXd k2 = MatrixXd::Zero(3 * count, 3 * count);

    double coef = t * t * abs(jac1) / 36;

    for (int i = 0; i < 3; i++) {
        MatrixXd sf3 = shapeFunction3(p, d, TRI2_INT(i, 0), TRI2_INT(i, 1));
        MatrixXd b2 = strainMatrix2(sf3, jinv);
        k2 += stiffPart(d1, b2, coef);
    }

    double ce1 = 1e-3 * t * t * abs(jac1) * d1(2, 2);
    double ce2 = -ce1 / 2;

    MatrixXd kk(6 * count, 6 * count);
    MatrixXd ks(6, 6);
    MatrixXd dir(6, 6);

    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            dir(i, j) = d(i, j);
            dir(i + 3, j + 3) = d(i, j);
        }
    }
    for (int i = 0; i < 3; i++) {
        int i2 = 2 * i;
        int i3 = 3 * i;
        int i6 = 6 * i;
        for (int j = 0; j < count; j++) {
            int j2 = 2 * j;
            int j3 = 3 * j;
            int j6 = 6 * j;
            for (int ii = 0; ii < 6; ii++) {
                for (int jj = 0; jj < 6; jj++) {
                    ks(ii, jj) = 0;
                }
            }
            ks(0, 0) = k1(i2, j2);
            ks(0, 1) = k1(i2, j2 + 1);
            ks(1, 0) = k1(i2 + 1, j2);
            ks(1, 1) = k1(i2 + 1, j2 + 1);
            for (int ii = 0; ii < 3; ii++) {
                for (int jj = 0; jj < 3; jj++) {
                    ks(2 + ii, 2 + jj) = k2(i3 + ii, j3 + jj);
                }
            }
            if (i == j) ks(5, 5) = ce1;
            else     ks(5, 5) = ce2;

            toDir(dir, ks);
            for (int ii = 0; ii < 6; ii++) {
                for (int jj = 0; jj < 6; jj++) {
                    kk(i6 + ii, j6 + jj) = ks(ii, jj);
                }
            }
        }
    }
    return kk;
};


// 歪 - 変位マトリックスの転置行列を返す
// sf - 形状関数行列
// jinv - 逆ヤコビ行列
MatrixXd TriElement1::strainMatrix1(MatrixXd sf, VectorXd jinv) {

    int count = nodeCount();
    MatrixXd b(2 * count, 3);

    auto ji = jinv;

    for (int i = 0; i < count; i++) {
        double dndx = ji[0] * sf(i, 1) + ji[3] * sf(i, 2);
        double dndy = ji[1] * sf(i, 1) + ji[4] * sf(i, 2);
        int i2 = 2 * i;
        b(i2, 0) = dndx;
        b(i2 + 1, 1) = dndy;
        b(i2, 2) = dndy;
        b(i2 + 1, 2) = dndx;
    }
    return b;
}


// 面外歪 - 変位マトリックスの転置行列を返す
// sf - 形状関数行列
// jinv - 逆ヤコビ行列
MatrixXd TriElement1::strainMatrix2(MatrixXd sf, VectorXd jinv) {

    int count = 3 * nodeCount();
    MatrixXd b(count, 3);

    auto ji = jinv;

    for (int i = 0; i < count; i++) {
        double hxx = ji[0] * sf(i, 2) + ji[3] * sf(i, 4);
        double hxy = ji[1] * sf(i, 2) + ji[4] * sf(i, 4);
        double hyx = ji[0] * sf(i, 3) + ji[3] * sf(i, 5);
        double hyy = ji[1] * sf(i, 3) + ji[4] * sf(i, 5);

        b(i, 0) = hyx;
        b(i, 1) = -hxy;
        b(i, 2) = hyy - hxx;
    }
    return b;
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// t - 要素厚さ
MatrixXd TriElement1::shapeFunctionMatrix(vector<FENode> p, double coef, double t) {

    double ds = coef * jacobian(p) / 12;

    int count = 3 * nodeCount();
    MatrixXd s(count, count);

    for (int i = 0; i < count; i++) {
        for (int j = 0; j < count; j++) {
            s(i, j) = 0.5 * ds;
        }
    }
    for (int i = 0; i < count; i++) 
        s(i, i) = ds;

    return s;
}


// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
MatrixXd TriElement1::geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, VectorXd d1, ShellParameter sp) {
    
    int count = nodeCount();
    MatrixXd kk(6 * count, 6 * count);
    MatrixXd d = dirMatrix(p);
    auto n = normalVector(p);
    auto v = toArray(u, 6);
    double t = sp.thickness;
    MatrixXd sf1 = shapeFunction(intP(0, 0), intP(0, 1));
    MatrixXd ja = jacobianMatrix(p, sf1, n, t);
    MatrixXd gr = grad(p, ja, sf1, d, t);
    MatrixXd jinv = jacobInv(ja, d);
    MatrixXd sf3 = shapeFunction3(p, d, intP(0, 0), intP(0, 1));
    MatrixXd sm = strainMatrix(sf1, sf3, jinv, d, 0, t);
    VectorXd vm = v * sm;
    VectorXd mv = d1 * vm;
    Stress str = toStress(mv);
    double det = ja.determinant();
    double w = abs(det);
    for (int i1 = 0; i1 < count; i1++) {
        int i6 = 6 * i1;
        for (int j1 = 0; j1 < count; j1++) {
            int j6 = 6 * j1;
            double s = w * (gr(i1, 0) * (str.xx * gr(j1, 0) + str.xy * gr(j1, 1) + str.zx * gr(j1, 2)) +
                gr(i1, 1) * (str.xy * gr(j1, 0) + str.yy * gr(j1, 1) + str.yz * gr(j1, 2)) +
                gr(i1, 2) * (str.zx * gr(j1, 0) + str.yz * gr(j1, 1) + str.zz * gr(j1, 2)));
            kk(i6, j6) = s;
            kk(i6 + 1, j6 + 1) = s;
            kk(i6 + 2, j6 + 2) = s;
        }
    }

    toDir3(d, kk);
    return kk;
}

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// n - 法線ベクトル
// d - 方向余弦マトリックス
// xsi,eta,zeta - ξ,η,ζ座標
// t - 要素厚さ
VectorXd TriElement1::strainPart(vector<FENode> p, VectorXd v, Vector3Dim n, MatrixXd d, double xsi, double eta, double zeta, double t) {

    MatrixXd sf1 = shapeFunction(xsi, eta);
    MatrixXd ja = jacobianMatrix(p, sf1, n, t);
    MatrixXd jinv = jacobInv(ja, d);
    MatrixXd sf3 = shapeFunction3(p, d, xsi, eta);
    MatrixXd sm = strainMatrix(sf1, sf3, jinv, d, zeta, t);
    VectorXd vm = v * sm;

    return vm;
};

// 歪 - 変位マトリックスの転置行列を返す
// ただし歪は要素面座標、変位は全体座標
// sf1 - 面内変形の形状関数行列
// sf3 - 面外変形の形状関数行列
// jinv - 逆ヤコビ行列
// d - 方向余弦マトリックス
// zeta - 節点のζ座標
// t - 要素厚さ
MatrixXd TriElement1::strainMatrix(MatrixXd sf1, MatrixXd  sf3, MatrixXd jinv, MatrixXd d, double zeta, double t) {
    
    MatrixXd b1 = strainMatrix1(sf1, jinv);

    MatrixXd b2 = strainMatrix2(sf3, jinv);

    int count = nodeCount();

    MatrixXd m1(3, 6);

    MatrixXd matrix = MatrixXd::Zero(6 * count, 3);

    double z = 0.5 * t * zeta;

    for (int i = 0; i < count; i++) {

        int i2 = 2 * i;
        int i3 = 3 * i;
        int i6 = 6 * i;

        for (int i1 = 0; i1 < 3; i1++) {
            m1(i1, 0) = b1(i2, i1);
            m1(i1, 1) = b1(i2 + 1, i1);
            m1(i1, 2) = z * b2(i3, i1);
            m1(i1, 3) = z * b2(i3 + 1, i1);
            m1(i1, 4) = z * b2(i3 + 2, i1);
            m1(i1, 5) = 0;
        }

        for (int i1 = 0; i1 < 3; i1++) {

            for (int j1 = 0; j1 < 3; j1++) {

                double s1 = 0, s2 = 0;

                for (int k1 = 0; k1 < 3; k1++) {
                    s1 += m1(i1, k1) * d(j1, k1);
                    s2 += m1(i1, k1 + 3) * d(j1, k1);
                }
                matrix(i6 + j1, i1) += s1;
                matrix(i6 + 3 + j1, i1) += s2;
            }
        }
    }
    return matrix;
}


// ベクトルを歪に変換する
// s - 歪ベクトル
Strain TriElement1::toStrain(VectorXd s) {
    VectorXd _s;
    _s << s[0], s[1], 0, s[2], 0, 0;
    return Strain(_s);
}

// ベクトルを歪に変換する
// s - 歪ベクトル
Stress TriElement1::toStress(VectorXd s) {
    VectorXd _s;
    _s << s[0], s[1], 0, s[2], 0, 0;
    return Stress(_s);
}
