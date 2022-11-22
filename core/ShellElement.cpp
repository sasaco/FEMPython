#include "ShellElement.h";

//--------------------------------------------------------------------//
// シェル要素
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
// nodeP - 節点のξ,η座標
// intP - 積分点のξ,η座標,重み係数
ShellElement::ShellElement(int _label, int material, int _param, vector<int> nodes, 
    vector<vector<double>> _nodeP, vector<vector<double>> _intP) :
    FElement(label, material, nodes) {
    label = _label;
    param = _param;
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

    double[3] n;
    normalVector(p, n);

    double ja[9];
    jacobianMatrix(p, sf, n, t);

    int count = nodeCount();

    double det = determinant(ja);

    double coef = 2 * w * abs(det);

    for (int i = 0; i < count; i++) {
        vector<double> matr;
        double cf2 = coef * sf[i][0];
        for (int j = 0; j < count; j++) {
            matr.push_back(cf2 * sf[j][0]);
        }
        out.push_back(matr);
    }
}


// 積分点の拡散マトリックス [ ∇Ni・∇Nj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
// t - 要素厚さ
void ShellElement::gradPart(vector<FENode> p, double x[3], double w, double t, vector<vector<double>> out) {

    vector<vector<double>> sf
    shapeFunction(x[0], x[1], sf);

    double n[3];
    normalVector(p, n);

    double ja[9];
    jacobianMatrix(p, sf, n, t, ja);

    vector<vector<double>> d;
    dirMatrix(p, d);

    vector<vector<double>> gr;
    grad(p, ja, sf, d, t, gr);

    int count = nodeCount();

    double det = determinant(ja);

    double coef = 2 * w * abs(det);

    for (int i = 0; i < count; i++) {
        vector<double> matr;
        vector<double> gri = gr[i];
        double c1 = coef * gri[0];
        double c2 = coef * gri[1];
        double c3 = coef * gri[2];
        for (int j = 0; j < count; j++) {
            vector<double> grj = gr[j];
            matr.push_back(c1 * grj[0] + c2 * grj[1] + c3 * grj[2]);
        }
        out.push_back(matr);
    }
};

// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
void ShellElement::shapeFunctionMatrix(vector<FENode> p, double coef, ShellParameter sp, vector<vector<double>> out) {
    
    int count = nodeCount();
    
    for (int i = 0; i < count; i++) {
        vector<double> s;
        for (int j = 0; j < count; j++) {
            s.push_back(0);
        }
        out.push_back(s);
    }
    
    double t = sp.thickness;

    for (int i = 0; i < intP.size(); i++) {

        vector<vector<double>> spr;
        shapePart(p, intP[i], coef * intP[i][2], t, spr);
        addMatrix(out, spr);
    }
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
void ShellElement::gradMatrix(vector<FENode> p, double coef, ShellParameter sp, vector<vector<double>> out) {
    
    int count = nodeCount();

    numeric::rep(count, count, out);

    double t = sp.thickness;

    for (int  i = 0; i < intP.size(); i++) {

        vector<vector<double>> gpr;
        gradPart(p, intP[i], coef * intP[i][2], t, gpr);
        addMatrix(out, gpr);
    }
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
void ShellElement::geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, ShellParameter sp, vector<vector<double>> out) {
    
    int count = nodeCount();
    
    numeric::rep(6 * count, 6 * count, out);

    vector<vector<double>> d;
    dirMatrix(p, d);
     
    double[3] n;
    normalVector(p, n);

    vector<double> v;
    toArray(u, 6, v);

    double t = sp.thickness;

    for (int i = 0; i < intP.size(); i++) {
        
        int ip = intP[i];
        
        vector<vector<double>> sf;
        shapeFunction(ip[0], ip[1], sf);

        double ja[9];
        jacobianMatrix(p, sf, n, t, ja);

        vector<vector<double>> gr;
        grad(p, ja, sf, d, t, gr);

        vector<vector<double>> sm;
        strainMatrix(ja, sf, d, 0, t, sm);

        vector<double> vm;
        numeric::dotVM(v, sm, vm);

        vector<double> mv;
        numeric::dotMV(d1, vm, mv);


        Stress str;
        toStress(mv, str);

        double det = determinant(ja);

        double w = 2 * ip[2] * abs(det);

        for (int i1 = 0; i1 < count; i1++) {

            int i6 = 6 * i1;
            vector<double> gri = gr[i1];

            for (int j1 = 0; j1 < count; j1++) {
                
                int j6 = 6 * j1;
                vector<double> grj = gr[j1];

                double s = w * (gri[0] * (str.xx * grj[0] + str.xy * grj[1] + str.zx * grj[2]) +
                    gri[1] * (str.xy * grj[0] + str.yy * grj[1] + str.yz * grj[2]) +
                    gri[2] * (str.zx * grj[0] + str.yz * grj[1] + str.zz * grj[2]));

                out[i6][j6] += s;
                out[i6 + 1][j6 + 1] += s;
                out[i6 + 2][j6 + 2] += s;
            }
        }
    }
    toDir3(d, out);
};


// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
void ShellElement::strainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, ShellParameter sp,
                                vector<Strain> strain1, vector<Stress> stress1, vector<double> energy1, 
                                vector<Strain> strain2, vector<Stress> stress2, vector<double> energy2) {

    int count = nodeCount();

    vector<vector<double>> d;
    dirMatrix(p, d);

    double[3] n;
    normalVector(p, n);


    vector<double> v;
    toArray(u, 6, v);

    double t = sp.thickness;

    for (int i = 0; i < count; i++) {
        vector<double> np = nodeP[i];

        vector<double> eps1;
        strainPart(p, v, n, d, np[0], np[1], 1, t, eps1);
        vector<double> eps2;
        strainPart(p, v, n, d, np[0], np[1], -1, t, eps2);

        strain1[i] = toStrain(eps1);
        stress1[i] = toStress(numeric::dotMV(d1, eps1));
        strain2[i] = toStrain(eps2);
        stress2[i] = toStress(numeric::dotMV(d1, eps2));
        strain1[i].rotate(d);
        stress1[i].rotate(d);
        strain2[i].rotate(d);
        stress2[i].rotate(d);
        energy1[i] = 0.5 * strain1[i].innerProduct(stress1[i]);
        energy2[i] = 0.5 * strain2[i].innerProduct(stress2[i]);
    }
};

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// n - 法線ベクトル
// d - 方向余弦マトリックス
// xsi,eta,zeta - ξ,η,ζ座標
// t - 要素厚さ
void ShellElement::strainPart(vector<FENode> p, vector<double> v, double n[3], vector<vector<double>> d, double  xsi, double eta, double zeta, double t, vector<double> out) {

    vector<vector<double>> sf;
    shapeFunction(xsi, eta, sf);

    double ja[9];
    jacobianMatrix(p, sf, n, t, ja);

    vector<vector<double>> sm;
    strainMatrix(ja, sf, d, zeta, t, sm);

    numeric::dotVM(v, sm, out);
};

// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
void ShellElement::elementStrainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, ShellParameter sp, 
                                        vector<Strain> _Strain1, vector<Stress> _Stress1, double energy1,
                                        vector<Strain> _Strain2, vector<Stress> _Stress2, double energy2) {

    vector<vector<double>> d;
    dirMatrix(p, d);

    double n[3];
    normalVector(p, n);

    vector<double> v;
    toArray(u, 6, v);

    double t = sp.thickness;
    
    double cf = 1 / intP.size();

    vector<double> strain1;
    vector<double> strain2;
    vector<double> stress1;
    vector<double> stress2;
    for (int i = 0; i < intP.size(); i++) {
        
        vector<double> ip = intP[i];
        
        vector<double> eps1;
        strainPart(p, v, n, d, ip[0], ip[1], 1, t, eps1);

        vector<double> eps2;
        strainPart(p, v, n, d, ip[0], ip[1], -1, t, eps2);

        numeric::add(strain1, eps1);
        numeric::add(strain2, eps2);

        vector<double> str1;
        numeric::dotMV(d1, eps1, str1);

        vector<double> str2;
        numeric::dotMV(d1, eps2, str2);

        numeric::add(stress1, str1);
        numeric::add(stress2, str2);

        energy1 += numeric::dotVV(eps1, str1);
        energy2 += numeric::dotVV(eps2, str2);
    }
    numeric::mul(strain1, cf);
    numeric::mul(stress1, cf);
    energy1 *= 0.5 * cf;
    numeric::mul(strain1, cf);
    numeric::mul(stress1, cf);
    energy2 *= 0.5 * cf;

    toStrain(strain1, _Strain1);
    toStress(stress1, _Stress1);
    toStrain(strain2, _Strain1);
    toStress(stress2, _Stress1);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
void ShellElement::toStrain(vector<double> s, Strain out) {
    out =  Strain([s[0], s[1], 0, s[2], s[3], s[4]]);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
void ShellElement::toStress(vector<double> s, Stress out) {
    out = Stress([s[0], s[1], 0, s[2], s[3], s[4]]);
};

// 要素を表す文字列を返す
// materials - 材料
// params - シェルパラメータ
// p - 節点
string ShellElement::toString(vector<Material> materials, vector<ShellParameter> params, vector<FENode> p) {

    string s = format("{}\t{}\t{}\t{}\t{}",
        getName(), label, materials[material].label, params[this.param].label);

    for (int i = 0; i < nodes.size(); i++) {
        s += '\t' + p[nodes[i]].label;
    }
    return s;
};


// 節点座標を局所座標系に変換する
// d - 方向余弦マトリックス
// p - 要素節点
vector<double> ShellElement::toLocal(d, vector<FENode> p) {

    vector<vector<double>> x;

    for (int i = 0; i < p.size(); i++) {
        vector<double> y = {
            d[0][0] * p[i].x + d[1][0] * p[i].y + d[2][0] * p[i].z,
            d[0][1] * p[i].x + d[1][1] * p[i].y + d[2][1] * p[i].z,
            d[0][2] * p[i].x + d[1][2] * p[i].y + d[2][2] * p[i].z
        };
        x.push_back(y);
    }
    return x;
}