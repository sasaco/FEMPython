#include "SolidElement.h";

#include <format>

//--------------------------------------------------------------------//
// \bhvf
// label - vfx
// material - Þ¿ÌCfbNX
// nodes - ß_Ô
// nodeP - ß_ÌÌ,Å,ÄÀW
// intP - Ïª_ÌÌ,Å,ÄÀW,dÝW
SolidElement::SolidElement(int label, int material, vector<int> nodes, vector<vector<double>> _nodeP, vector<vector<double>> _intP) :
    FElement(label, material, nodes) {

    nodeP = _nodeP;
    intP = _intP;

    // ZÊÌ2vfÌÏª_ÌÌ,Å,ÄÀW,dÝW
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



};


// RrsñðÔ·
// p - vfß_
// sf - `óÖsñ
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


// `óÖÌùz [ dNi/dx dNi/dy dNi/dz ] ðÔ·
// p - vfß_
// ja - Rrsñ
// sf - `óÖsñ
void SolidElement::grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, vector<vector<double>> out) {

    int count = nodeCount();

    double ji[9];

    //  3x3 sñÌtsñðßé
    // 3x3ÌsñðüÍ
    double a[3][3];
    for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 3; ++j) {
            a[i][j] = ja[i + j];
        }
    }

    //PÊsñðìé
    double inv_a[3][3];
    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            inv_a[i][j] = (i == j) ? 1.0 : 0.0;
        }
    }
    //|«oµ@
    for (int i = 0; i < 3; i++) {
        double buf = 1 / a[i][i];
        for (int j = 0; j < 3; j++) {
            a[i][j] *= buf;
            inv_a[i][j] *= buf;
        }
        for (int j = 0; j < 3; j++) {
            if (i != j) {
                buf = a[j][i];
                for (int k = 0; k < 3; k++) {
                    a[j][k] -= a[i][k] * buf;
                    inv_a[j][k] -= inv_a[i][k] * buf;
                }
            }
        }
    }

    for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 3; ++j) {
            ji[i + j] = inv_a[i][j];
        }
    }
    //  


    for (int i = 0; i < count; i++) {
        out[i][0] = ji[0] * sf[i][1] + ji[3] * sf[i][2] + ji[6] * sf[i][3];
        out[i][1] = ji[1] * sf[i][1] + ji[4] * sf[i][2] + ji[7] * sf[i][3];
        out[i][2] = ji[2] * sf[i][1] + ji[5] * sf[i][2] + ji[8] * sf[i][3];
    }
};


// c - ÏÊ}gbNXÌ]usñðÔ·
// grad - `óÖÌùz
void SolidElement::strainMatrix(vector<vector<double>> grad, vector<vector<double>> out) {

    int count = nodeCount();
    for (int i = 0; i < 3 * count; ++i) {
        vector<double> m;
        for (int j = 0; j < 6; ++j) {
            m.push_back(0);
        }
        out.push_back(m);
    }

    for (int i = 0; i < count; i++) {
        int i3 = 3 * i;
        vector<double> gr = grad[i];
        out[i3][0] = gr[0];
        out[i3 + 1][1] = gr[1];
        out[i3 + 2][2] = gr[2];
        out[i3][3] = gr[1];
        out[i3 + 1][3] = gr[0];
        out[i3 + 1][4] = gr[2];
        out[i3 + 2][4] = gr[1];
        out[i3][5] = gr[2];
        out[i3 + 2][5] = gr[0];
    }
}


// 3x3Ìsñ®ðÔ·
double SolidElement::determinant(double ja[9]) {
    
    double det = 0;
    // 3x3ÌsñðüÍ
    double a[3][3];
    for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 3; ++j) {
            a[i][j] = ja[i + j];
        }
    }
    // sñ®ÌvZ
    for (int i = 0; i < 3; ++i) {
        det = det + a[0][i] * (a[1][(i + 1) % 3] * a[2][(i + 2) % 3] - a[1][(i + 2) % 3] * a[2][(i + 1) % 3]);
    }

    return det;
}


// Ïª_Ì`óÖ}gbNX [ NiNj ] ðÔ·
// p - vfß_
// x - Ì,Å,ÄÀW
// w - dÝW
void SolidElement::shapePart(vector<FENode> p, double x[3], double w, vector<vector<double>> out) {

    vector<vector<double>> sf;
    shapeFunction(x[0], x[1], x[2], sf);

    double ja[9];
    jacobianMatrix( p,  sf, ja);
    int count = nodeCount();

    double det = determinant(ja);

    double coef = w * abs(det);
    for (int i = 0; i < count; i++) {
        vector<double> matr;
        double cf2 = coef * sf[i][0];
        for (int j = 0; j < count; j++) {
            matr.push_back(cf2 * sf[j][0]);
        }
        out.push_back(matr);
    }
}


// Ïª_ÌgU}gbNX [ ÞNiEÞNj ] ðÔ·
// p - vfß_
// x - Ì,Å,ÄÀW
// w - dÝW
void SolidElement::gradPart(vector<FENode> p, double x[3], double w, vector<vector<double>> out) {

    vector<vector<double>> sf;
    shapeFunction(x[0], x[1], x[2], sf);

    double ja[9];
    jacobianMatrix(p, sf, ja);

    vector<vector<double>> gr;
    grad(p, ja, sf, gr);

    int count = nodeCount();

    double ja[9];
    double det = determinant(ja);
    double coef = w * abs(det);

    for (int i = 0; i < count; i++) {
        vector<double> matr; 
        vector<double> gri = gr[i];
        double c1 = coef * gri[0], c2 = coef * gri[1], c3 = coef * gri[2];
        for (int j = 0; j < count; j++) {
            vector<double> grj = gr[j];
            matr.push_back(c1 * grj[0] + c2 * grj[1] + c3 * grj[2]);
        }
        out.push_back(matr);
    }
};


// ¿Ê}gbNXðÔ·
// p - vfß_
// dens - Þ¿Ì§x
void SolidElement::massMatrix(vector<FENode> p, double dens, vector<vector<double>> out) {
    
    int count = nodeCount();

    for (int i = 0; i < 3 * count; ++i) {
        vector<double> m;
        for (int j = 0; j < 3 * count; ++j) {
            m.push_back(0);
        }
        out.push_back(m);
    }


    for (int i = 0; i < intP.size(); i++) {

        vector<vector<double>> sf;
        shapeFunction(intP[i][0], intP[i][1], intP[i][2], sf);

        double ja[9];
        jacobianMatrix(p, sf, ja);

        double det = determinant(ja);

        double coef = intP[i][3] * dens * abs(det);


        for (int i1 = 0; i1 < count; i1++) {
            for (int j1 = 0; j1 < count; j1++) {
                double value = coef * sf[i1][0] * sf[j1][0], i3 = 3 * i1, j3 = 3 * j1;
                out[i3][j3] += value;
                out[i3 + 1][j3 + 1] += value;
                out[i3 + 2][j3 + 2] += value;
            }
        }
    }
}


// sñÌaðvZ·é
// a - îsñ
// da - Á¦ésñ
void SolidElement::addMatrix(vector<vector<double>> a, vector<vector<double>> da) {
    for (int i = 0; i < a.size(); i++) {
        for (int j = 0; j < a[i].size(); j++) {
            a[i][j] += da[i][j];
        }
    }
}


// «}gbNXðÔ·
// p - vfß_
// d1 - Í - c}gbNX
void SolidElement::stiffnessMatrix(vector<FENode> p, vector<vector<double>> d1, vector<vector<double>> out) {
    
    int count = 3 * nodeCount();

    for (int i = 0; i < count; ++i) {
        vector<double> kk;
        for (int j = 0; j < count; ++j) {
            kk.push_back(0);
        }
        out.push_back(kk);
    }

    for (int i = 0; i < intP.size(); i++) {

        double* ip = intP[i];
        vector<vector<double>> sf;
        shapeFunction(ip[0], ip[1], ip[2], sf);

        double ja[9];
        jacobianMatrix(p, sf, ja);

        vector<vector<double>> gr;
        grad(p, ja, sf, gr);

        vector<vector<double>> sm;
        strainMatrix(gr, sm);

        double det = determinant(ja);
        double coef = ip[3] * abs(det);

        vector<vector<double>> ks;
        stiffPart(d1, sm, coef, ks);

        addMatrix(out, ks);
    }
}


// `óÖ}gbNX [ çNiNjdV ] ðÔ·
// p - vfß_
// coef - W
void SolidElement::shapeFunctionMatrix(vector<FENode> p, double coef, vector<vector<double>> out) {

    int count = nodeCount();
    
    for (int i = 0; i < count; ++i) {
        vector<double> s;
        for (int j = 0; j < count; ++j) {
            s.push_back(0);
        }
        out.push_back(s);
    }

    for (int i = 0; i < intP.size(); i++) {
        vector<vector<double>> sp;
        shapePart(p, intP[i], coef * intP[i][3], sp);

        addMatrix(out, sp);
    }
};

// gU}gbNX [ çÞNiEÞNjdV ] ðÔ·
// p - vfß_
// coef - W
void SolidElement::gradMatrix(vector<FENode> p, double coef, vector<vector<double>> out) {

    int count = nodeCount();
    
    for (int i = 0; i < count; ++i) {
        vector<double> g;
        for (int j = 0; j < count; ++j) {
            g.push_back(0);
        }
        out.push_back(g);
    }

    for (int i = 0; i < intP.size(); i++) {
        vector<vector<double>> gp;
        gradPart(p, intP[i], coef * intP[i][3], gp);

        addMatrix(out, gp);
    }
};


// ô½«}gbNXðÔ·
// p - vfß_
// u - ß_ÏÊ
// d1 - Í - c}gbNX
void SolidElement::geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, vector<vector<double>> out) {

    int count = nodeCount();

    for (int i = 0; i < 3 * count; ++i) {
        vector<double> kk;
        for (int j = 0; j < 3 * count; ++j) {
            kk.push_back(0);
        }
        out.push_back(kk);
    }

    vector<double> v;
    toArray(u, 3, v);

    for (int i = 0; i < intP.size(); i++) {

        double* ip = intP[i];

        vector<vector<double>> sf;
        shapeFunction(ip[0], ip[1], ip[2], sf);

        double ja[9];
        jacobianMatrix(p, sf, ja);

        vector<vector<double>> gr;
        grad(p, ja, sf, gr);

        vector<vector<double>> sm;
        strainMatrix(gr, sm);

        vector<double> vm;
        numeric::dotVM(v, sm, vm);

        vector<double> str;
        numeric::dotMV(d1, vm, str);

        double det = determinant(ja);
        double w = ip[3] * abs(det);

        for (int i1 = 0; i1 < count; i1++) {
            int i3 = 3 * i1;
            vector<double> gri = gr[i1];
            for (int j1 = 0; j1 < count; j1++) {
                int j3 = 3 * j1;
                vector<double> grj = gr[j1];
                double s = w * (gri[0] * (str[0] * grj[0] + str[3] * grj[1] + str[5] * grj[2]) +
                    gri[1] * (str[3] * grj[0] + str[1] * grj[1] + str[4] * grj[2]) +
                    gri[2] * (str[5] * grj[0] + str[4] * grj[1] + str[2] * grj[2]));
                out[i3][j3] += s;
                out[i3 + 1][j3 + 1] += s;
                out[i3 + 2][j3 + 2] += s;
            }
        }
    }
};


// ß_cEÍðÔ·
// p - vfß_
// u - ß_ÏÊ
// d1 - Í - c}gbNX
void SolidElement::strainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
                                vector<Strain> strain, vector<Stress> stress, vector<double> energy) {

    int count = nodeCount();
    
    vector<double> v;
    toArray(u, 3, v);

    for (int i = 0; i < count; i++) {
        vector<double> eps;
        strainPart(p, v, nodeP[i], eps);

        strain.push_back(Strain(eps));

        vector<double> str;
        numeric::dotMV(d1, eps, str);

        stress.push_back(Stress(str));

        energy.push_back(0.5 * strain[i].innerProduct(stress[i]));
    }

}

// vfàÌcxNgðÔ·
// p - vfß_
// v - ß_ÏÊxNg
// x - Ì,Å,ÄÀW
void SolidElement::strainPart(vector<FENode> p, vector<double> v, double x[3], vector<double> out) {

    vector<vector<double>> sf;
    shapeFunction(x[0], x[1], x[2], sf);

    double ja[9];
    jacobianMatrix(p, sf, ja);

    vector<vector<double>> gr;
    grad(p, ja, sf, gr);

    vector<vector<double>> sm;
    strainMatrix(gr, sm);

    numeric::dotVM(v, sm, out);
};


// vfcEÍðÔ·
// p - vfß_
// u - ß_ÏÊ
// d1 - Í - c}gbNX
void SolidElement::elementStrainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
                                        Strain _Strain, Stress _Stress, double energy) {

    vector<double> v;
    toArray(u, 3, v);

    double cf = 1 / intP.size();

    vector<double> strain = { 0, 0, 0, 0, 0, 0 };
    vector<double> stress = { 0, 0, 0, 0, 0, 0 };
    energy = 0;

    for (int i = 0; i < intP.size(); i++) {
        vector<double> eps;
        strainPart(p, v, intP[i], eps);

        numeric::add(strain, eps);

        vector<double> str;
        numeric::dotMV(d1, eps, str);

        numeric::add(stress, str);

        energy += numeric::dotVV(eps, str);
    }

    numeric::mul(strain, cf);
    numeric::mul(stress, cf);

    energy *= 0.5 * cf;
    _Strain = Strain(strain);
    _Stress = Stress(stress);

}

// vfð\·¶ñðÔ·
// materials - Þ¿
// p - ß_
string SolidElement::toString(vector<Material> materials, vector<FENode> p) {

    Material mat = materials[material];

    string s = "";
    for (int i = 0; i < nodes.size(); i++) {

        s += '\t';
        FENode n = p[nodes[i]];
        s += n.label;
    }

    return format("{}\t{}\t{}\t{}",
        getName(), label, mat.label, s)

};