#include "TriElement1.h";

//--------------------------------------------------------------------//
// 三角形1次要素 (薄肉シェル)
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
TriElement1::TriElement1(int label, int material, int param, vector<int> nodes) :
                ShellElement(label, material, param, nodes, TRI1_NODE, TRI1_INT){
};


// 要素名称を返す
string TriElement1::getName() {
    return "TriElement1";
};


// １次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
vector<vector<double>> TriElement1::shapeFunction(double xsi, double eta) {
    vector<vector<double>> result = {
        { 1 - xsi - eta, -1, -1 }, 
        { xsi,            1,  0 }, 
        { eta,            0,  1 }
    };
    return result;
};


// ２次の形状関数行列 [ Ni dNi/dξ dNi/dη ] を返す
// xsi,eta - 要素内部ξ,η座標
vector<vector<double>> TriElement1::shapeFunction2(double xsi, double eta) {
    double xe = 1 - xsi - eta;
    vector<vector<double>> result = {
        { xe * (2 * xe - 1),    1 - 4 * xe,     1 - 4 * xe      }, 
        { xsi * (2 * xsi - 1),  4 * xsi - 1,    0               },
        { eta * (2 * eta - 1),  0,              4 * eta - 1     }, 
        { 4 * xe * xsi,         4 * (xe - xsi), -4 * xsi        },
        { 4 * xsi * eta,        4 * eta,        4 * xsi         }, 
        { 4 * xe * eta,         -4 * eta,       4 * (xe - eta)  }
    };
    return result;
};


// 角度の形状関数行列 [ Hxi Hyi dHxi/dξ dHyi/dξ dHxi/dη dHyi/dη ] を返す
// p - 要素節点
// d - 方向余弦マトリックス
// xsi,eta - 要素内部ξ,η座標
vector<vector<double>> TriElement1::shapeFunction3(vector<FENode> p, vector<vector<double>> d, double xsi, double eta) {

    int count = nodeCount();
    vector<vector<double>> m;
    numeric::rep(3 * count, 6, m);

    vector<vector<double>> sf2 = shapeFunction2(xsi, eta);

    Vector3 p1;
    p[1].clone(p1);
    Vector3 d12 = p1.sub(p[0]);

    FENode p2;
    p[2].clone(p2);
    Vector3 d23 = p2.sub(p[1]);

    FENode p0;
    p[0].clone(p0);
    Vector3 d31 = p0.sub(p[2]);

    vector<double> l = {
        1 / d12.lengthSq(),
        1 / d23.lengthSq(),
        1 / d31.lengthSq()
    };

    double x[] = {
        d[0][0] * d12.x + d[1][0] * d12.y + d[2][0] * d12.z,
        d[0][0] * d23.x + d[1][0] * d23.y + d[2][0] * d23.z,
        d[0][0] * d31.x + d[1][0] * d31.y + d[2][0] * d31.z
    };

    double y[] = {
        d[0][1] * d12.x + d[1][1] * d12.y + d[2][1] * d12.z,
        d[0][1] * d23.x + d[1][1] * d23.y + d[2][1] * d23.z,
        d[0][1] * d31.x + d[1][1] * d31.y + d[2][1] * d31.z
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

    for (int i = 0; i < 3; i++) {
        int i1 = (i + 2) % 3;
        int i3 = 3 * i;
        for (int j = 0; j < 3; j++) {
            int j2 = 2 * j;
            m[i3][j2] = a[i1] * sf2[3 + i1][j] - a[i] * sf2[3 + i][j];
            m[i3][j2 + 1] = b[i1] * sf2[3 + i1][j] - b[i] * sf2[3 + i][j];
            m[i3 + 1][j2] = sf2[i][j] - c[i1] * sf2[3 + i1][j] - c[i] * sf2[3 + i][j];
            double dn = d1[i1] * sf2[3 + i1][j] + d1[i] * sf2[3 + i][j];
            m[i3 + 1][j2 + 1] = dn;
            m[i3 + 2][j2] = dn;
            m[i3 + 2][j2 + 1] = sf2[i][j] - e[i1] * sf2[3 + i1][j] - e[i] * sf2[3 + i][j];
        }
    }
    return m;
};


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
vector<vector<double>> TriElement1::massMatrix(vector<FENode> p, double dens, double t) {
    
    int count = nodeCount();
    
    vector<vector<double>> m = numeric::rep(6 * count, 6 * count);

    vector<vector<double>> mb = numeric::rep(3 * count, 3 * count);
    
    vector<vector<double>> d = dirMatrix(p);

    double djt = dens * t * jacobian(p);
    
    double tt = C1_12 * t * t;
    
    double dm = C1_12 * djt;

    for (int k = 0; k < 3; k++) {

        vector<double> ipi = TRI2_INT[k];
        
        vector<vector<double>> sf3 = shapeFunction3(p, d, ipi[0], ipi[1]);

        vector<vector<double>> sf = shapeFunction(ipi[0], ipi[1]);

        vector<double> hz = {
            sf[0][0], 0, 0, sf[1][0], 0, 0, sf[2][0], 0, 0
        };

        double cfm = djt * ipi[2];

        for (int i = 0; i < 3 * count; i++) {
            for (int j = 0; j < 3 * count; j++) {
                double hxhy = sf3[i][0] * sf3[j][0] + sf3[i][1] * sf3[j][1];
                mb[i][j] += cfm * (tt * hxhy + hz[i] * hz[j]);
            }
        }
    }

    for (int i = 0; i < count; i++) {
        int i3 = 3 * i;
        int i6 = 6 * i;
        for (int j = 0; j < count; j++) {
            int j3 = 3 * j;
            int j6 = 6 * j;
            double cf1 = TRI1_MASS1[i][j];
            double dme = cf1 * dm;
            m[i6][j6] = dme;
            m[i6 + 1][j6 + 1] = dme;
            for (int i1 = 0; i1 < 3; i1++) {
                for (int j1 = 0; j1 < 3; j1++) {
                    m[i6 + 2 + i1][j6 + 2 + j1] = mb[i3 + i1][j3 + j1];
                }
            }
            m[i6 + 5][j6 + 5] = 1e-5 * dm * tt;
        }
    }
    toDir3(d, m);
    return m;
};


// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
vector<vector<double>> TriElement1::stiffnessMatrix(vector<FENode> p, vector<vector<double>> d1, ShellParameter sp) {

    vector<vector<double>> d = dirMatrix(p);

    double *n = normalVector(p);
    double t = sp.thickness;

    vector<vector<double>> sf1 = shapeFunction(C1_3, C1_3);
    vector<double> ja1 = jacobianMatrix(p, sf1, n, t);
    double jac1 = ja1.determinant();
    vector<double> jinv = jacobInv(ja1, d);
    vector<vector<double>> b1 = strainMatrix1(sf1, jinv);
    vector<vector<double>> k1 = stiffPart(d1, b1, abs(jac1));

    int count = nodeCount();
    vector<vector<double>> k2 = numeric::rep(3 * count, 3 * count);

    double coef = t * t * abs(jac1) / 36;

    for (int i = 0; i < 3; i++) {
        double ipi = TRI2_INT[i];
        vector<vector<double>> sf3 = shapeFunction3(p, d, ipi[0], ipi[1]);
        vector<vector<double>> b2 = strainMatrix2(sf3, jinv);
        addMatrix(k2, stiffPart(d1, b2, coef));
    }

    double ce1 = 1e-3 * t * t * abs(jac1) * d1[2][2];
    double ce2 = -ce1 / 2;

    vector<vector<double>> kk = numeric::rep(6 * count, 6 * count);
    vector<vector<double>> ks = numeric.rep(6, 6);
    vector<vector<double>> dir = numeric.rep(6, 6);

    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            dir[i][j] = d[i][j];
            dir[i + 3][j + 3] = d[i][j];
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
                    ks[ii][jj] = 0;
                }
            }
            ks[0][0] = k1[i2][j2];
            ks[0][1] = k1[i2][j2 + 1];
            ks[1][0] = k1[i2 + 1][j2];
            ks[1][1] = k1[i2 + 1][j2 + 1];
            for (int ii = 0; ii < 3; ii++) {
                for (int jj = 0; jj < 3; jj++) {
                    ks[2 + ii][2 + jj] = k2[i3 + ii][j3 + jj];
                }
            }
            if (i == j) ks[5][5] = ce1;
            else     ks[5][5] = ce2;
            toDir(dir, ks);
            for (int ii = 0; ii < 6; ii++) {
                for (int jj = 0; jj < 6; jj++) {
                    kk[i6 + ii][j6 + jj] = ks[ii][jj];
                }
            }
        }
    }
    return kk;
};


// 歪 - 変位マトリックスの転置行列を返す
// sf - 形状関数行列
// jinv - 逆ヤコビ行列
vector<vector<double>> TriElement1::strainMatrix1(vector<vector<double>> sf, vector<vector<double>> jinv) {

    int count = nodeCount();
    vector<vector<double>> b = numeric::rep(2 * count, 3);

    vector<vector<double>> ji = jinv.elements;

    for (int i = 0; i < count; i++) {
        vector<double> sfi = sf[i];
        double dndx = ji[0] * sfi[1] + ji[3] * sfi[2];
        double dndy = ji[1] * sfi[1] + ji[4] * sfi[2];
        int i2 = 2 * i;
        b[i2][0] = dndx;
        b[i2 + 1][1] = dndy;
        b[i2][2] = dndy;
        b[i2 + 1][2] = dndx;
    }
    return b;
};


// 面外歪 - 変位マトリックスの転置行列を返す
// sf - 形状関数行列
// jinv - 逆ヤコビ行列
vector<vector<double>> TriElement1::strainMatrix2(vector<vector<double>> sf, vector<double> jinv) {

    int count = 3 * nodeCount();
    vector<vector<double>> b;
    vector<double> ji = jinv.elements;

    for (int i = 0; i < count; i++) {
        vector<double> sfi = sf[i];
        double hxx = ji[0] * sfi[2] + ji[3] * sfi[4];
        double hxy = ji[1] * sfi[2] + ji[4] * sfi[4];
        double hyx = ji[0] * sfi[3] + ji[3] * sfi[5];
        double hyy = ji[1] * sfi[3] + ji[4] * sfi[5];

        vector<double> a = { hyx, -hxy, hyy - hxx };
        b.push_back(a);
    }
    return b;
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// t - 要素厚さ
vector<vector<double>> TriElement1::shapeFunctionMatrix(vector<FENode> p, double coef, double t) {

    double ds = coef * jacobian(p) / 12;

    int count = 3 * nodeCount();
    vector<vector<double>> s;
    for (int i = 0; i < count, i++) {
        s.push_back(0.5 * ds);
    }

    for (int i = 0; i < count; i++) 
        s[i][i] = ds;
    return s;
};


// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
vector<vector<double>> TriElement1::geomStiffnessMatrix(vector<FENode> p, u, d1, ShellParameter sp) {
    
    intcount = nodeCount();
    vector<vector<double>> kk = numeric::rep(6 * count, 6 * count);

    vector<vector<double>> d = dirMatrix(p);

    double* n = normalVector(p);

    vector<double> v = toArray(u, 6);
    
    double t = sp.thickness;

    vector<double> ip = intP[0];

    vector<vector<double>> sf1 = shapeFunction(ip[0], ip[1]);

    vector<vector<double>> ja = jacobianMatrix(p, sf1, n, t);

    vector<vector<double>> gr = grad(p, ja, sf1, d, t);

    var jinv = this.jacobInv(ja, d);

    vector<vector<double>> sf3 = this.shapeFunction3(p, d, ip[0], ip[1]);

    vector<vector<double>> sm = strainMatrix(sf1, sf3, jinv, d, 0, t);

    vector<double> vm = numeric::dotVM(v, sm);

    vector<double> mv = numeric::dotMV(d1, vm);

    Stress str = toStress(mv);

    double det = determinant(ja);

    double w = abs(det);

    for (int i1 = 0; i1 < count; i1++) {

        int i6 = 6 * i1;
        vector<double> gri = gr[i1];

        for (int j1 = 0; j1 < count; j1++) {

            int j6 = 6 * j1;
            vector<double> grj = gr[j1];

            double s = w * (gri[0] * (str.xx * grj[0] + str.xy * grj[1] + str.zx * grj[2]) +
                gri[1] * (str.xy * grj[0] + str.yy * grj[1] + str.yz * grj[2]) +
                gri[2] * (str.zx * grj[0] + str.yz * grj[1] + str.zz * grj[2]));

            kk[i6][j6] = s;
            kk[i6 + 1][j6 + 1] = s;
            kk[i6 + 2][j6 + 2] = s;
        }
    }

    toDir3(d, kk);

    return kk;
};

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// n - 法線ベクトル
// d - 方向余弦マトリックス
// xsi,eta,zeta - ξ,η,ζ座標
// t - 要素厚さ
vector<double> TriElement1::strainPart(vector<FENode> p, vector<double> v, double n[3], vector<vector<double>> d, double xsi, double eta, double zeta, double t) {

    vector<vector<double>> sf1 = shapeFunction(xsi, eta);

    vector<vector<double>> ja = jacobianMatrix(p, sf1, n, t);

    vector<vector<double>> jinv = jacobInv(ja, d);

    vector<vector<double>> sf3 = shapeFunction3(p, d, xsi, eta);

    vector<vector<double>> sm = strainMatrix(sf1, sf3, jinv, d, zeta, t);

    vector<double> vm = numeric::dotVM(v, sm);

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
vector<vector<double>> TriElement1::strainMatrix(vector<vector<double>> sf1, vector<vector<double>>  sf3, vector<vector<double>> jinv, vector<vector<double>> d, double zeta, double t) {
    
    vector<vector<double>> b1 = strainMatrix1(sf1, jinv);

    vector<vector<double>> b2 = strainMatrix2(sf3, jinv);

    int count = nodeCount();

    vector<vector<double>> m1 = numeric::rep(3, 6);

    vector<vector<double>> matrix = numeric::rep(6 * count, 3);

    double z = 0.5 * t * zeta;

    for (int i = 0; i < count; i++) {

        int i2 = 2 * i;
        int i3 = 3 * i;
        int i6 = 6 * i;

        for (int i1 = 0; i1 < 3; i1++) {
            m1[i1][0] = b1[i2][i1];
            m1[i1][1] = b1[i2 + 1][i1];
            m1[i1][2] = z * b2[i3][i1];
            m1[i1][3] = z * b2[i3 + 1][i1];
            m1[i1][4] = z * b2[i3 + 2][i1];
            m1[i1][5] = 0;
        }

        for (int i1 = 0; i1 < 3; i1++) {

            vector<double> m1i = m1[i1];

            for (int j1 = 0; j1 < 3; j1++) {

                vector<double> dj = d[j1];
                
                double s1 = 0, s2 = 0;

                for (int k1 = 0; k1 < 3; k1++) {
                    s1 += m1i[k1] * dj[k1];
                    s2 += m1i[k1 + 3] * dj[k1];
                }
                matrix[i6 + j1][i1] += s1;
                matrix[i6 + 3 + j1][i1] += s2;
            }
        }
    }
    return matrix;
};


// ベクトルを歪に変換する
// s - 歪ベクトル
Strain TriElement1::toStrain(double s[]) {
    return Strain([s[0], s[1], 0, s[2], 0, 0]);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
Stress TriElement1::toStress(double s[]) {
    return Stress([s[0], s[1], 0, s[2], 0, 0]);
};
