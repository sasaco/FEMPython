#include "ShellElement.h";
#include "numeric.h";


class TriElement1 : public ShellElement {

private:
    // 三角形1次要素の節点のξ,η座標
    vector<vector<double>> TRI1_NODE = { {0, 0}, {1, 0}, {0, 1} };
    // 三角形1次要素の積分点のξ,η座標,重み係数
    vector<vector<double>> TRI1_INT = { {C1_3, C1_3, 0.5} };

    // 三角形2次要素の積分点のξ,η座標,重み係数
    vector<vector<double>> TRI2_INT = { {GTRI2[0], GTRI2[0], C1_6}, {GTRI2[1], GTRI2[0], C1_6},
        {GTRI2[0], GTRI2[1], C1_6} };

    // 三角形1次要素の質量マトリックス係数
    vector<vector<double>> TRI1_MASS1 = { {1, 0.5, 0.5}, {0.5, 1, 0.5}, {0.5, 0.5, 1} };


public:
    TriElement1(int label, int material, int param, vector<int> nodes);

    string getName() override;

    int nodeCount() override;

    vector<vector<double>> shapeFunction(double xsi, double eta) override;

    vector<vector<double>> shapeFunction2(double xsi, double eta);

    vector<vector<double>> shapeFunction3(vector<FENode> p, vector<vector<double>> d, double xsi, double eta);

    double jacobian(vector<FENode> p);

    vector<vector<double>> massMatrix(vector<FENode> p, double dens, double t)

};

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
    var d = dirMatrix(p), n = normalVector(p), t = sp.thickness, i, j, ii, jj;

    var sf1 = this.shapeFunction(C1_3, C1_3);
    var ja1 = this.jacobianMatrix(p, sf1, n, t);
    var jac1 = ja1.determinant();
    var jinv = this.jacobInv(ja1, d);
    var b1 = this.strainMatrix1(sf1, jinv);
    var k1 = this.stiffPart(d1, b1, Math.abs(jac1));

    var count = this.nodeCount(), k2 = numeric.rep([3 * count, 3 * count], 0);
    var coef = t * t * Math.abs(jac1) / 36;
    for (i = 0; i < 3; i++) {
        var ipi = TRI2_INT[i], sf3 = this.shapeFunction3(p, d, ipi[0], ipi[1]);
        var b2 = this.strainMatrix2(sf3, jinv);
        addMatrix(k2, this.stiffPart(d1, b2, coef));
    }

    var ce1 = 1e-3 * t * t * Math.abs(jac1) * d1[2][2], ce2 = -ce1 / 2;
    var kk = numeric.rep([6 * count, 6 * count], 0), ks = numeric.rep([6, 6], 0);
    var dir = numeric.rep([6, 6], 0);
    for (i = 0; i < 3; i++) {
        for (j = 0; j < 3; j++) {
            dir[i][j] = d[i][j];
            dir[i + 3][j + 3] = d[i][j];
        }
    }
    for (i = 0; i < 3; i++) {
        var i2 = 2 * i;
        var i3 = 3 * i;
        var i6 = 6 * i;
        for (j = 0; j < count; j++) {
            var j2 = 2 * j;
            var j3 = 3 * j;
            var j6 = 6 * j;
            for (ii = 0; ii < 6; ii++) {
                for (jj = 0; jj < 6; jj++) {
                    ks[ii][jj] = 0;
                }
            }
            ks[0][0] = k1[i2][j2];
            ks[0][1] = k1[i2][j2 + 1];
            ks[1][0] = k1[i2 + 1][j2];
            ks[1][1] = k1[i2 + 1][j2 + 1];
            for (ii = 0; ii < 3; ii++) {
                for (jj = 0; jj < 3; jj++) {
                    ks[2 + ii][2 + jj] = k2[i3 + ii][j3 + jj];
                }
            }
            if (i == j) ks[5][5] = ce1;
            else     ks[5][5] = ce2;
            toDir(dir, ks);
            for (ii = 0; ii < 6; ii++) {
                for (jj = 0; jj < 6; jj++) {
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
TriElement1.prototype.strainMatrix1 = function(sf, jinv) {
    var count = this.nodeCount(), b = numeric.rep([2 * count, 3], 0);
    var ji = jinv.elements;
    for (var i = 0; i < count; i++) {
        var sfi = sf[i];
        var dndx = ji[0] * sfi[1] + ji[3] * sfi[2];
        var dndy = ji[1] * sfi[1] + ji[4] * sfi[2];
        var i2 = 2 * i;
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
TriElement1.prototype.strainMatrix2 = function(sf, jinv) {
    var count = 3 * this.nodeCount(), b = [];
    var ji = jinv.elements;
    for (var i = 0; i < count; i++) {
        var sfi = sf[i];
        var hxx = ji[0] * sfi[2] + ji[3] * sfi[4];
        var hxy = ji[1] * sfi[2] + ji[4] * sfi[4];
        var hyx = ji[0] * sfi[3] + ji[3] * sfi[5];
        var hyy = ji[1] * sfi[3] + ji[4] * sfi[5];
        b[i] = [hyx, -hxy, hyy - hxx];
    }
    return b;
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// t - 要素厚さ
TriElement1.prototype.shapeFunctionMatrix = function(p, coef, t) {
    var ds = coef * this.jacobian(p) / 12;
    var count = 3 * this.nodeCount(), s = numeric.rep([count], 0.5 * ds);
    for (var i = 0; i < count; i++) s[i][i] = ds;
    return s;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
TriElement1.prototype.geomStiffnessMatrix = function(p, u, d1, sp) {
    var count = this.nodeCount(), kk = numeric.rep([6 * count, 6 * count], 0);
    var d = dirMatrix(p), n = normalVector(p);
    var v = this.toArray(u, 6), t = sp.thickness;
    var ip = this.intP[0];
    var sf1 = this.shapeFunction(ip[0], ip[1]);
    var ja = this.jacobianMatrix(p, sf1, n, t);
    var gr = this.grad(p, ja, sf1, d, t);
    var jinv = this.jacobInv(ja, d);
    var sf3 = this.shapeFunction3(p, d, ip[0], ip[1]);
    var sm = this.strainMatrix(sf1, sf3, jinv, d, 0, t);
    var str = this.toStress(numeric.dotMV(d1, numeric.dotVM(v, sm)));
    var w = Math.abs(ja.determinant());
    for (var i1 = 0; i1 < count; i1++) {
        var i6 = 6 * i1, gri = gr[i1];
        for (var j1 = 0; j1 < count; j1++) {
            var j6 = 6 * j1, grj = gr[j1];
            var s = w * (gri[0] * (str.xx * grj[0] + str.xy * grj[1] + str.zx * grj[2]) +
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
TriElement1.prototype.strainPart = function(p, v, n, d, xsi, eta, zeta, t) {
    var sf1 = this.shapeFunction(xsi, eta);
    var ja = this.jacobianMatrix(p, sf1, n, t);
    var jinv = this.jacobInv(ja, d);
    var sf3 = this.shapeFunction3(p, d, xsi, eta);
    var sm = this.strainMatrix(sf1, sf3, jinv, d, zeta, t);
    return numeric.dotVM(v, sm);
};

// 歪 - 変位マトリックスの転置行列を返す
// ただし歪は要素面座標、変位は全体座標
// sf1 - 面内変形の形状関数行列
// sf3 - 面外変形の形状関数行列
// jinv - 逆ヤコビ行列
// d - 方向余弦マトリックス
// zeta - 節点のζ座標
// t - 要素厚さ
TriElement1.prototype.strainMatrix = function(sf1, sf3, jinv, d, zeta, t) {
    var b1 = this.strainMatrix1(sf1, jinv);
    var b2 = this.strainMatrix2(sf3, jinv);
    var count = this.nodeCount(), m1 = numeric.rep([3, 6], 0);
    var matrix = numeric.rep([6 * count, 3], 0), z = 0.5 * t * zeta, i1;
    for (var i = 0; i < count; i++) {
        var i2 = 2 * i;
        var i3 = 3 * i;
        var i6 = 6 * i;
        for (i1 = 0; i1 < 3; i1++) {
            m1[i1][0] = b1[i2][i1];
            m1[i1][1] = b1[i2 + 1][i1];
            m1[i1][2] = z * b2[i3][i1];
            m1[i1][3] = z * b2[i3 + 1][i1];
            m1[i1][4] = z * b2[i3 + 2][i1];
            m1[i1][5] = 0;
        }
        for (i1 = 0; i1 < 3; i1++) {
            var m1i = m1[i1];
            for (var j1 = 0; j1 < 3; j1++) {
                var dj = d[j1], s1 = 0, s2 = 0;
                for (var k1 = 0; k1 < 3; k1++) {
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
TriElement1.prototype.toStrain = function(s) {
    return new Strain([s[0], s[1], 0, s[2], 0, 0]);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
TriElement1.prototype.toStress = function(s) {
    return new Stress([s[0], s[1], 0, s[2], 0, 0]);
};
