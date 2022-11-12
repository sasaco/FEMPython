#include "FElement.h";
#include "FENode.h";

#include <string>
#include <vector>
using namespace std;


class SolidElement : public FElement {

private:

    // 四面体2次要素の節点のξ,η,ζ座標
    const double TETRA2_NODE[10][3] = {
        {0, 0, 0},
        {1, 0, 0},
        {0, 1, 0}, 
        {0, 0, 1}, 
        {0.5, 0, 0}, 
        {0.5, 0.5, 0},
        {0, 0.5, 0},
        {0, 0, 0.5}, 
        {0.5, 0, 0.5}, 
        {0, 0.5, 0.5}
    };

    // 四面体2次要素の積分点のξ,η,ζ座標,重み係数
    const double TETRA2_INT[4][4] = {
        {GTETRA2[0], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[1], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[1], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[0], GTETRA2[1], C1_24}
    };

    // 楔形1次要素の節点のξ,η,ζ座標
    const double WEDGE1_NODE[6][3] = {
        {0, 0, -1}, 
        {1, 0, -1}, 
        {0, 1, -1}, 
        {0, 0, 1}, 
        {1, 0, 1}, 
        {0, 1, 1}
    };

    // 楔形1次要素の積分点のξ,η,ζ座標,重み係数
    const double WEDGE1_INT[2][4] = {
        {C1_3, C1_3, GX2[0], 0.5}, 
        {C1_3, C1_3, GX2[1], 0.5}
    };

    // 楔形2次要素の節点のξ,η,ζ座標
    const double WEDGE2_NODE[15][3] = {
        {0, 0, -1}, 
        {1, 0, -1}, 
        {0, 1, -1}, 
        {0, 0, 1}, 
        {1, 0, 1}, 
        {0, 1, 1},
        {0.5, 0, -1},
        {0.5, 0.5, -1},
        {0, 0.5, -1},
        {0.5, 0, 1},
        {0.5, 0.5, 1}, 
        {0, 0.5, 1},
        {0, 0, 0}, 
        {1, 0, 0}, 
        {0, 1, 0}
    };


    // 楔形2次要素の積分点のξ,η,ζ座標,重み係数
    const double WEDGE2_INT[9][4] = {
        {GTRI2[0], GTRI2[0], GX3[0], C1_6 * GW3[0]},
        {GTRI2[1], GTRI2[0], GX3[0], C1_6 * GW3[0]},
        {GTRI2[0], GTRI2[1], GX3[0], C1_6 * GW3[0]},
        {GTRI2[0], GTRI2[0], GX3[1], C1_6 * GW3[1]},
        {GTRI2[1], GTRI2[0], GX3[1], C1_6 * GW3[1]},
        {GTRI2[0], GTRI2[1], GX3[1], C1_6 * GW3[1]},
        {GTRI2[0], GTRI2[0], GX3[2], C1_6 * GW3[2]},
        {GTRI2[1], GTRI2[0], GX3[2], C1_6 * GW3[2]},
        {GTRI2[0], GTRI2[1], GX3[2], C1_6 * GW3[2]}
    };

    // 六面体1次要素の節点のξ,η,ζ座標
    const double HEXA1_NODE[8][3] = {
        {-1, -1, -1}, 
        {1, -1, -1}, 
        {1, 1, -1},
        {-1, 1, -1},
        {-1, -1, 1},
        {1, -1, 1},
        {1, 1, 1},
        {-1, 1, 1}
    };

    // 六面体1次要素の積分点のξ,η,ζ座標,重み係数
    const double HEXA1_INT[8][4] = {
        {GX2[0], GX2[0], GX2[0], 1},
        {GX2[1], GX2[0], GX2[0], 1},
        {GX2[0], GX2[1], GX2[0], 1},
        {GX2[1], GX2[1], GX2[0], 1},
        {GX2[0], GX2[0], GX2[1], 1},
        {GX2[1], GX2[0], GX2[1], 1},
        {GX2[0], GX2[1], GX2[1], 1},
        {GX2[1], GX2[1], GX2[1], 1}
    };

    // 六面体2次要素の節点のξ,η,ζ座標
    const double HEXA2_NODE[20][3] = {
        {-1, -1, -1}, 
        {1, -1, -1}, 
        {1, 1, -1},
        {-1, 1, -1},
        {-1, -1, 1}, 
        {1, -1, 1}, 
        {1, 1, 1}, 
        {-1, 1, 1},
        {0, -1, -1},
        {1, 0, -1}, 
        {0, 1, -1}, 
        {-1, 0, -1},
        {0, -1, 1}, 
        {1, 0, 1}, 
        {0, 1, 1}, 
        {-1, 0, 1},
        {-1, -1, 0}, 
        {1, -1, 0}, 
        {1, 1, 0}, 
        {-1, 1, 0}
    };

    // 六面体2次要素の積分点のξ,η,ζ座標,重み係数
    double HEXA2_INT[9][4]; // コンストラクタで初期化

    // 六面体1次要素の質量マトリックス係数
    double HEXA1_MASS_BASE[8][8]; // コンストラクタで初期化



    vector<vector<double>> nodeP;
    vector<vector<double>> intP;

public:

    SolidElement(int label, int material, vector<int> nodes, vector <vector<double>> _nodeP, vector <vector<double>> _intP);

    void jacobianMatrix(vector<FENode> p, vector<vector<double>> sf, double out[9]);



};



//--------------------------------------------------------------------//
// ソリッド要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
// nodeP - 節点のξ,η,ζ座標
// intP - 積分点のξ,η,ζ座標,重み係数
SolidElement::SolidElement(int label, int material, vector<int> nodes, vector<vector<double>> _nodeP, vector<vector<double>> _intP) :
    FElement(label, material, nodes) {

    nodeP = _nodeP;
    intP = _intP;

    // 六面体2次要素の積分点のξ,η,ζ座標,重み係数
    int a = 0;
    for (int k = 0; k < 3; k++) {
        for (int j = 0; j < 3; j++) {
            for (int i = 0; i < 3; i++) {
                HEXA2_INT[a][0] = GX3[i];
                HEXA2_INT[a][1] = GX3[j];
                HEXA2_INT[a][2] = GX3[k];
                HEXA2_INT[a][3] = GW3[i] * GW3[j] * GW3[k];
                a++;
            }
        }
    }

    // 六面体1次要素の質量マトリックス係数
    for (int i = 0; i < 8; i++) {
        for (int j = 0; j < 8; j++) {
            double s = abs(HEXA1_NODE[i][0] - HEXA1_NODE[j][0]) +
                       abs(HEXA1_NODE[i][1] - HEXA1_NODE[j][1]) +
                       abs(HEXA1_NODE[i][2] - HEXA1_NODE[j][2]);
            HEXA1_MASS_BASE[i][j] = pow(0.5, 0.5 * s) / 27;
        }
    }

};


// ヤコビ行列を返す
// p - 要素節点
// sf - 形状関数行列
void SolidElement::jacobianMatrix(vector<FENode> p, vector<vector<double>> sf, double out[9]) {

    int count = nodeCount();

    for (int i = 0; i < 9; i++) {
        out[i] = 0;
    }

    for (int i = 0; i < count; i++) {
        vector<double> sfi = sf[i];
        double pix = p[i].x;
        double piy = p[i].y;
        double piz = p[i].z;
        for (int j = 0; j < 3; j++) {
            double sfij = sfi[j + 1];
            out[j] += sfij * pix;
            out[j + 3] += sfij * piy;
            out[j + 6] += sfij * piz;
        }
    }
}


// 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
// p - 要素節点
// ja - ヤコビ行列
// sf - 形状関数行列
SolidElement::grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, double gr[9]) {

    int count = nodeCount();

    var ji = new THREE.Matrix3().getInverse(ja, true).elements;


    for (int i = 0; i < count; i++) {
        gr[i] = [ji[0] * sf[i][1] + ji[3] * sf[i][2] + ji[6] * sf[i][3],
            ji[1] * sf[i][1] + ji[4] * sf[i][2] + ji[7] * sf[i][3],
            ji[2] * sf[i][1] + ji[5] * sf[i][2] + ji[8] * sf[i][3]];
    }
    return gr;
};

// 歪 - 変位マトリックスの転置行列を返す
// grad - 形状関数の勾配
SolidElement.prototype.strainMatrix = function(grad) {
    var count = this.nodeCount(), m = numeric.rep([3 * count, 6], 0);
    for (var i = 0; i < count; i++) {
        var i3 = 3 * i, gr = grad[i];
        m[i3][0] = gr[0];
        m[i3 + 1][1] = gr[1];
        m[i3 + 2][2] = gr[2];
        m[i3][3] = gr[1];
        m[i3 + 1][3] = gr[0];
        m[i3 + 1][4] = gr[2];
        m[i3 + 2][4] = gr[1];
        m[i3][5] = gr[2];
        m[i3 + 2][5] = gr[0];
    }
    return m;
};

// 積分点の形状関数マトリックス [ NiNj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
SolidElement.prototype.shapePart = function(p, x, w) {
    var sf = this.shapeFunction(x[0], x[1], x[2]);
    var ja = this.jacobianMatrix(p, sf);
    var count = this.nodeCount(), matrix = [];
    var coef = w * Math.abs(ja.determinant());
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
SolidElement.prototype.gradPart = function(p, x, w) {
    var sf = this.shapeFunction(x[0], x[1], x[2]);
    var ja = this.jacobianMatrix(p, sf);
    var gr = this.grad(p, ja, sf);
    var count = this.nodeCount(), matrix = [];
    var coef = w * Math.abs(ja.determinant());
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

// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
SolidElement.prototype.massMatrix = function(p, dens) {
    var count = this.nodeCount(), m = numeric.rep([3 * count, 3 * count], 0);
    for (var i = 0; i < this.intP.length; i++) {
        var sf = this.shapeFunction(this.intP[i][0], this.intP[i][1],
            this.intP[i][2]);
        var ja = this.jacobianMatrix(p, sf);
        var coef = this.intP[i][3] * dens * Math.abs(ja.determinant());
        for (var i1 = 0; i1 < count; i1++) {
            for (var j1 = 0; j1 < count; j1++) {
                var value = coef * sf[i1][0] * sf[j1][0], i3 = 3 * i1, j3 = 3 * j1;
                m[i3][j3] += value;
                m[i3 + 1][j3 + 1] += value;
                m[i3 + 2][j3 + 2] += value;
            }
        }
    }
    return m;
};

// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
SolidElement.prototype.stiffnessMatrix = function(p, d1) {
    var count = 3 * this.nodeCount(), kk = numeric.rep([count, count], 0);
    for (var i = 0; i < this.intP.length; i++) {
        var ip = this.intP[i];
        var sf = this.shapeFunction(ip[0], ip[1], ip[2]);
        var ja = this.jacobianMatrix(p, sf);
        var ks = this.stiffPart(d1, this.strainMatrix(this.grad(p, ja, sf)),
            ip[3] * Math.abs(ja.determinant()));
        addMatrix(kk, ks);
    }
    return kk;
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
SolidElement.prototype.shapeFunctionMatrix = function(p, coef) {
    var count = this.nodeCount(), s = numeric.rep([count, count], 0);
    for (var i = 0; i < this.intP.length; i++) {
        addMatrix(s, this.shapePart(p, this.intP[i], coef * this.intP[i][3]));
    }
    return s;
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
SolidElement.prototype.gradMatrix = function(p, coef) {
    var count = this.nodeCount(), g = numeric.rep([count, count], 0);
    for (var i = 0; i < this.intP.length; i++) {
        addMatrix(g, this.gradPart(p, this.intP[i], coef * this.intP[i][3]));
    }
    return g;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
SolidElement.prototype.geomStiffnessMatrix = function(p, u, d1) {
    var count = this.nodeCount(), kk = numeric.rep([3 * count, 3 * count], 0);
    var v = this.toArray(u, 3);
    for (var i = 0; i < this.intP.length; i++) {
        var ip = this.intP[i];
        var sf = this.shapeFunction(ip[0], ip[1], ip[2]);
        var ja = this.jacobianMatrix(p, sf);
        var gr = this.grad(p, ja, sf);
        var sm = this.strainMatrix(gr);
        var str = numeric.dotMV(d1, numeric.dotVM(v, sm));
        var w = ip[3] * Math.abs(ja.determinant());
        for (var i1 = 0; i1 < count; i1++) {
            var i3 = 3 * i1, gri = gr[i1];
            for (var j1 = 0; j1 < count; j1++) {
                var j3 = 3 * j1, grj = gr[j1];
                var s = w * (gri[0] * (str[0] * grj[0] + str[3] * grj[1] + str[5] * grj[2]) +
                    gri[1] * (str[3] * grj[0] + str[1] * grj[1] + str[4] * grj[2]) +
                    gri[2] * (str[5] * grj[0] + str[4] * grj[1] + str[2] * grj[2]));
                kk[i3][j3] += s;
                kk[i3 + 1][j3 + 1] += s;
                kk[i3 + 2][j3 + 2] += s;
            }
        }
    }
    return kk;
};

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
SolidElement.prototype.strainStress = function(p, u, d1) {
    var count = this.nodeCount(), v = this.toArray(u, 3);
    var strain = [], stress = [], energy = [];
    for (var i = 0; i < count; i++) {
        var eps = this.strainPart(p, v, this.nodeP[i]);
        strain[i] = new Strain(eps);
        var str = numeric.dotMV(d1, eps);
        stress[i] = new Stress(str);
        energy[i] = 0.5 * strain[i].innerProduct(stress[i]);
    }
    return[strain, stress, energy];
};

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// x - ξ,η,ζ座標
SolidElement.prototype.strainPart = function(p, v, x) {
    var sf = this.shapeFunction(x[0], x[1], x[2]);
    var ja = this.jacobianMatrix(p, sf);
    var sm = this.strainMatrix(this.grad(p, ja, sf));
    return numeric.dotVM(v, sm);
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
SolidElement.prototype.elementStrainStress = function(p, u, d1) {
    var v = this.toArray(u, 3), cf = 1 / this.intP.length;
    var strain = [0, 0, 0, 0, 0, 0], stress = [0, 0, 0, 0, 0, 0], energy = 0;
    for (var i = 0; i < this.intP.length; i++) {
        var eps = this.strainPart(p, v, this.intP[i]);
        strain = numeric.add(strain, eps);
        var str = numeric.dotMV(d1, eps);
        stress = numeric.add(stress, str);
        energy += numeric.dotVV(eps, str);
    }
    strain = numeric.mul(strain, cf);
    stress = numeric.mul(stress, cf);
    energy *= 0.5 * cf;
    return[new Strain(strain), new Stress(stress), energy];
};

// 要素を表す文字列を返す
// materials - 材料
// p - 節点
SolidElement.prototype.toString = function(materials, p) {
    var s = this.getName() + '\t' + this.label.toString(10) + '\t' +
        materials[this.material].label.toString(10);
    for (var i = 0; i < this.nodes.length; i++) {
        s += '\t' + p[this.nodes[i]].label.toString(10);
    }
    return s;
};