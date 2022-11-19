#include "FElement.h";
#include "FENode.h";
#include "Material.h";
#include "numeric.h";

#include <string>
#include <vector>

class ShellElement : public FElement {

private:
    // 三角形1次要素の節点のξ,η座標
    vector<vector<double>> TRI1_NODE = {{0, 0}, {1, 0}, {0, 1}};
    // 三角形1次要素の積分点のξ,η座標,重み係数
    vector<vector<double>> TRI1_INT = {{C1_3, C1_3, 0.5}};
    // 三角形2次要素の積分点のξ,η座標,重み係数
    vector<vector<double>> TRI2_INT = {{GTRI2[0], GTRI2[0], C1_6}, {GTRI2[1], GTRI2[0], C1_6},
        {GTRI2[0], GTRI2[1], C1_6}};
    // 四角形1次要素の節点のξ,η座標
    vector<vector<double>> QUAD1_NODE = {{-1, -1}, {1, -1}, {1, 1}, {-1, 1}};
    // 四角形1次要素の積分点のξ,η座標,重み係数
    vector<vector<double>> QUAD1_INT = {{GX2[0], GX2[0], 1}, {GX2[1], GX2[0], 1}, {GX2[0], GX2[1], 1},
        {GX2[1], GX2[1], 1}};
    // 三角形1次要素の質量マトリックス係数
    vector<vector<double>> TRI1_MASS1 = {{1, 0.5, 0.5}, {0.5, 1, 0.5}, {0.5, 0.5, 1}};


    int param;
    bool isShell;
    vector<vector<double>> nodeP;
    vector<vector<double>> intP;

public:
    ShellElement(int label, int material, int param, vector<int> nodes, vector<vector<double>> _nodeP, vector<vector<double>> _intP);

    void jacobianMatrix(vector<FENode> p, vector<vector<double>> sf, double n[3], double t, double out[9]);

    void jacobInv(double ja[9], vector<vector<double>> d, vector<double> out);

    void grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, vector<vector<double>> d, double t, vector<vector<double>> out);

    void strainMatrix1(double ja[9], vector<vector<double>> sf, vector<vector<double>> d, vector<vector<double>> out);

    void strainMatrix(double ja[9], vector<vector<double>> sf, vector<vector<double>> d, double zeta, double t, vector<vector<double>> out);

    void shapePart(vector<FENode> p, vector<double> x, double w, double t, vector<vector<double>> out);

};



//--------------------------------------------------------------------//
// シェル要素
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
// nodeP - 節点のξ,η座標
// intP - 積分点のξ,η座標,重み係数
ShellElement::ShellElement(int label, int material, int param, vector<int> nodes, 
    vector<vector<double>> _nodeP, vector<vector<double>> _intP) :
    FElement(label, material, nodes) {
    param = param;
    isShell = true;
    nodeP = _nodeP;
    intP = _intP;
}


// ヤコビ行列を返す
// p - 要素節点
// sf - 形状関数行列
// n - 法線ベクトル
// t - 要素厚さ
void ShellElement::jacobianMatrix(vector<FENode> p, vector<vector<double>> sf, 
    double n[3], double t, double out[9]) {
    
    int count = nodeCount();
    
    for (int i = 0; i < 9; i++) {
        out[i] = 0;

    }

    for (int i = 0; i < count; i++) {
        
        vector<double> sfi = sf[i];
        FENode ppi = p[i];
        double pix = ppi.x;
        double piy = ppi.y;
        double piz = ppi.z;

        for (int j = 0; j < 2; j++) {
            double sfij = sfi[j + 1];
            out[j] += sfij * pix;
            out[j + 3] += sfij * piy;
            out[j + 6] += sfij * piz;
        }
    }
    out[2] = 0.5 * t * n[0];
    out[5] = 0.5 * t * n[1];
    out[8] = 0.5 * t * n[2];
};


// 逆ヤコビ行列を返す
// ja - ヤコビ行列
// d - 方向余弦マトリックス
void ShellElement::jacobInv(double ja[9], vector<vector<double>> d, vector<double> out) {

    vector<double> e1;
    for (int i = 0; i < 9; i++) {
        e1.push_back(ja[i]);
    }

    vector<double> jd = {
        e1[0] * d[0][0] + e1[3] * d[1][0] + e1[6] * d[2][0],
        e1[0] * d[0][1] + e1[3] * d[1][1] + e1[6] * d[2][1],
        e1[0] * d[0][2] + e1[3] * d[1][2] + e1[6] * d[2][2],
        e1[1] * d[0][0] + e1[4] * d[1][0] + e1[7] * d[2][0],
        e1[1] * d[0][1] + e1[4] * d[1][1] + e1[7] * d[2][1],
        e1[1] * d[0][2] + e1[4] * d[1][2] + e1[7] * d[2][2],
        0, 
        0, 
        e1[2] * d[0][2] + e1[5] * d[1][2] + e1[8] * d[2][2] 
    };

    numeric::getInverse(jd, out);

};


// 形状関数の勾配 [ dNi/dx dNi/dy ] を返す
// p - 要素節点
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
// t - 要素厚さ
void ShellElement::grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, 
    vector<vector<double>> d, double t, vector<vector<double>> out) {
    
    int count = nodeCount();
    
    vector<double> gr;

    vector<double> ji;
    jacobInv(ja, d, ji);

    for (int i = 0; i < count; i++) {
        vector<double> sfi = sf[i];
        double dndxsi = sfi[1];
        double dndeta = sfi[2];
        gr.push_back(ji[0] * dndxsi + ji[3] * dndeta);
        gr.push_back(ji[1] * dndxsi + ji[4] * dndeta);
        gr.push_back(ji[2] * dndxsi + ji[5] * dndeta);
        out.push_back(gr);
    }
};


// 歪 - 変位マトリックスの転置行列を返す
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
void ShellElement::strainMatrix1(double ja[9], vector<vector<double>> sf, 
    vector<vector<double>> d, vector<vector<double>> out) {
    int count = nodeCount();
    
    int count = nodeCount();
    for (int i = 0; i < count; ++i) {
        vector<double> m;
        for (int j = 0; j < 4; ++j) {
            m.push_back(0);
        }
        out.push_back(m);
    }

    vector<double> ji;
    jacobInv(ja, d, ji);

    for (int i = 0; i < count; i++) {
        vector<double> mi = out[i];
        vector<double> sfi = sf[i];
        for (int j = 0; j < 3; j++) {
            mi[j] = ji[j] * sfi[1] + ji[j + 3] * sfi[2];
        }
        mi[3] = ji[8] * sfi[0];
    }
};

// 歪 - 変位マトリックスの転置行列を返す
// ただし歪は要素面座標、変位は全体座標
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
// zeta - 節点のζ座標
// t - 要素厚さ
void ShellElement::strainMatrix(double ja[9], vector<vector<double>> sf, 
    vector<vector<double>> d, double zeta, double t, vector<vector<double>> out) {
    
    vector<vector<double>> b;
    strainMatrix1(ja, sf, d, b);
    double z = 0.5 * t * zeta;

    int count = nodeCount();
    
    vector <vector<double>> m1;
    for (int i = 0; i < 5; i++) {
        vector<double> m2;
        for (int j = 0; j < 6; j++) {
            m2.push_back(0);
        }
        m1.push_back(m2);
    }

    for (int i = 0; i < 6 * count; i++) {
        vector<double> matrix;
        for (int j = 0; j < 5; j++) {
            matrix.push_back(0);
        }
        out.push_back(matrix);
    }


    for (int i = 0; i < count; i++) {
        vector<double> bi = b[i];
        m1[0][0] = bi[0];
        m1[0][4] = z * bi[0];
        m1[1][1] = bi[1];
        m1[1][3] = -z * bi[1];
        m1[2][0] = bi[1];
        m1[2][1] = bi[0];
        m1[2][3] = -z * bi[0];
        m1[2][4] = z * bi[1];
        m1[3][1] = bi[2];
        m1[3][2] = bi[1];
        m1[3][3] = -0.5 * t * bi[3] - z * bi[2];
        m1[4][0] = bi[2];
        m1[4][2] = bi[0];
        m1[4][4] = 0.5 * t * bi[3] + z * bi[2];
        int ib = 6 * i;
        for (int i1 = 0; i1 < 5; i1++) {
            vector<double> m1i = m1[i1];
            for (int j1 = 0; j1 < 3; j1++) {
                vector<double> dj = d[j1];
                double s1 = 0;
                double s2 = 0;
                for (int k1 = 0; k1 < 3; k1++) {
                    s1 += m1i[k1] * dj[k1];
                    s2 += m1i[k1 + 3] * dj[k1];
                }
                out[ib + j1][i1] += s1;
                out[ib + 3 + j1][i1] += s2;
            }
        }
    }
};

// 積分点の形状関数マトリックス [ NiNj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
// t - 要素厚さ
void ShellElement::shapePart(vector<FENode> p, vector<double> x, double w, double t, vector<vector<double>> out) {

    vector<vector<double>> sf;
    shapeFunction(x[0], x[1] ,sf);


    double ja[9];
    jacobianMatrix(p, sf, normalVector(p), t);

    var count = this.nodeCount(), matrix = [];
    var coef = 2 * w * Math.abs(ja.determinant());
    for (var i = 0; i < count; i++) {
        var matr = [], cf2 = coef * sf[i][0];
        for (var j = 0; j < count; j++) {
            matr[j] = cf2 * sf[j][0];
        }
        matrix[i] = matr;
    }
    return matrix;
};

// 積分点の拡散マトリックス [ ∇Ni・∇Nj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
// t - 要素厚さ
ShellElement.prototype.gradPart = function(p, x, w, t) {
    var sf = this.shapeFunction(x[0], x[1]);
    var ja = this.jacobianMatrix(p, sf, normalVector(p), t);
    var gr = this.grad(p, ja, sf, dirMatrix(p), t);
    var count = this.nodeCount(), matrix = [];
    var coef = 2 * w * Math.abs(ja.determinant());
    for (var i = 0; i < count; i++) {
        var matr = [], gri = gr[i];
        var c1 = coef * gri[0], c2 = coef * gri[1], c3 = coef * gri[2];
        for (var j = 0; j < count; j++) {
            var grj = gr[j];
            matr[j] = c1 * grj[0] + c2 * grj[1] + c3 * grj[2];
        }
        matrix[i] = matr;
    }
    return matrix;
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
ShellElement.prototype.shapeFunctionMatrix = function(p, coef, sp) {
    var count = this.nodeCount(), s = numeric.rep([count, count], 0);
    var t = sp.thickness;
    for (var i = 0; i < this.intP.length; i++) {
        addMatrix(s, this.shapePart(p, this.intP[i], coef * this.intP[i][2], t));
    }
    return s;
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
ShellElement.prototype.gradMatrix = function(p, coef, sp) {
    var count = this.nodeCount(), g = numeric.rep([count, count], 0);
    var t = sp.thickness;
    for (var i = 0; i < this.intP.length; i++) {
        addMatrix(g, this.gradPart(p, this.intP[i], coef * this.intP[i][2], t));
    }
    return g;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
ShellElement.prototype.geomStiffnessMatrix = function(p, u, d1, sp) {
    var count = this.nodeCount(), kk = numeric.rep([6 * count, 6 * count], 0);
    var d = dirMatrix(p), n = normalVector(p);
    var v = this.toArray(u, 6), t = sp.thickness;
    for (var i = 0; i < this.intP.length; i++) {
        var ip = this.intP[i];
        var sf = this.shapeFunction(ip[0], ip[1]);
        var ja = this.jacobianMatrix(p, sf, n, t);
        var gr = this.grad(p, ja, sf, d, t);
        var sm = this.strainMatrix(ja, sf, d, 0, t);
        var str = this.toStress(numeric.dotMV(d1, numeric.dotVM(v, sm)));
        var w = 2 * ip[2] * Math.abs(ja.determinant());
        for (var i1 = 0; i1 < count; i1++) {
            var i6 = 6 * i1, gri = gr[i1];
            for (var j1 = 0; j1 < count; j1++) {
                var j6 = 6 * j1, grj = gr[j1];
                var s = w * (gri[0] * (str.xx * grj[0] + str.xy * grj[1] + str.zx * grj[2]) +
                    gri[1] * (str.xy * grj[0] + str.yy * grj[1] + str.yz * grj[2]) +
                    gri[2] * (str.zx * grj[0] + str.yz * grj[1] + str.zz * grj[2]));
                kk[i6][j6] += s;
                kk[i6 + 1][j6 + 1] += s;
                kk[i6 + 2][j6 + 2] += s;
            }
        }
    }
    toDir3(d, kk);
    return kk;
};

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
ShellElement.prototype.strainStress = function(p, u, d1, sp) {
    var count = this.nodeCount(), d = dirMatrix(p), n = normalVector(p);
    var v = this.toArray(u, 6), t = sp.thickness;
    var strain1 = [], stress1 = [], energy1 = [], strain2 = [], stress2 = [], energy2 = [];
    for (var i = 0; i < count; i++) {
        var np = this.nodeP[i];
        var eps1 = this.strainPart(p, v, n, d, np[0], np[1], 1, t);
        var eps2 = this.strainPart(p, v, n, d, np[0], np[1], -1, t);
        strain1[i] = this.toStrain(eps1);
        stress1[i] = this.toStress(numeric.dotMV(d1, eps1));
        strain2[i] = this.toStrain(eps2);
        stress2[i] = this.toStress(numeric.dotMV(d1, eps2));
        strain1[i].rotate(d);
        stress1[i].rotate(d);
        strain2[i].rotate(d);
        stress2[i].rotate(d);
        energy1[i] = 0.5 * strain1[i].innerProduct(stress1[i]);
        energy2[i] = 0.5 * strain2[i].innerProduct(stress2[i]);
    }
    return[strain1, stress1, energy1, strain2, stress2, energy2];
};

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// n - 法線ベクトル
// d - 方向余弦マトリックス
// xsi,eta,zeta - ξ,η,ζ座標
// t - 要素厚さ
ShellElement.prototype.strainPart = function(p, v, n, d, xsi, eta, zeta, t) {
    var sf = this.shapeFunction(xsi, eta);
    var ja = this.jacobianMatrix(p, sf, n, t);
    var sm = this.strainMatrix(ja, sf, d, zeta, t);
    return numeric.dotVM(v, sm);
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
ShellElement.prototype.elementStrainStress = function(p, u, d1, sp) {
    var d = dirMatrix(p), n = normalVector(p), v = this.toArray(u, 6);
    var t = sp.thickness, cf = 1 / this.intP.length;
    var strain1 = [0, 0, 0, 0, 0, 0], stress1 = [0, 0, 0, 0, 0, 0], energy1 = 0;
    var strain2 = [0, 0, 0, 0, 0, 0], stress2 = [0, 0, 0, 0, 0, 0], energy2 = 0;
    for (var i = 0; i < this.intP.length; i++) {
        var ip = this.intP[i];
        var eps1 = this.strainPart(p, v, n, d, ip[0], ip[1], 1, t);
        var eps2 = this.strainPart(p, v, n, d, ip[0], ip[1], -1, t);
        strain1 = numeric.add(strain1, eps1);
        strain2 = numeric.add(strain2, eps2);
        var str1 = numeric.dotMV(d1, eps1);
        var str2 = numeric.dotMV(d1, eps2);
        stress1 = numeric.add(stress1, str1);
        stress2 = numeric.add(stress2, str2);
        energy1 += numeric.dotVV(eps1, str1);
        energy2 += numeric.dotVV(eps2, str2);
    }
    strain1 = numeric.mul(strain1, cf);
    stress1 = numeric.mul(stress1, cf);
    energy1 *= 0.5 * cf;
    strain2 = numeric.mul(strain1, cf);
    stress2 = numeric.mul(stress1, cf);
    energy2 *= 0.5 * cf;
    return[this.toStrain(strain1), this.toStress(stress1), energy1,
        this.toStrain(strain2), this.toStress(stress2), energy2];
};

// ベクトルを歪に変換する
// s - 歪ベクトル
ShellElement.prototype.toStrain = function(s) {
    return new Strain([s[0], s[1], 0, s[2], s[3], s[4]]);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
ShellElement.prototype.toStress = function(s) {
    return new Stress([s[0], s[1], 0, s[2], s[3], s[4]]);
};

// 要素を表す文字列を返す
// materials - 材料
// params - シェルパラメータ
// p - 節点
ShellElement.prototype.toString = function(materials, params, p) {
    var s = this.getName() + '\t' + this.label.toString(10) + '\t' +
        materials[this.material].label.toString(10) + '\t' +
        params[this.param].label.toString(10);
    for (var i = 0; i < this.nodes.length; i++) {
        s += '\t' + p[this.nodes[i]].label.toString(10);
    }
    return s;
};



// 節点座標を局所座標系に変換する
// d - 方向余弦マトリックス
// p - 要素節点
function toLocal(d, p) {
    var x = [];
    for (var i = 0; i < p.length; i++) {
        x[i] = new THREE.Vector3().set
        (d[0][0] * p[i].x + d[1][0] * p[i].y + d[2][0] * p[i].z,
            d[0][1] * p[i].x + d[1][1] * p[i].y + d[2][1] * p[i].z,
            d[0][2] * p[i].x + d[1][2] * p[i].y + d[2][2] * p[i].z);
    }
    return x;
}