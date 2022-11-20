#include "SolidElement.h";

#include <format>

//--------------------------------------------------------------------//
// \bhvf
// label - vfx
// material - ήΏΜCfbNX
// nodes - ί_Τ
// nodeP - ί_ΜΜ,Ε,ΔΐW
// intP - Οͺ_ΜΜ,Ε,ΔΐW,dέW
SolidElement::SolidElement(int label, int material, vector<int> nodes) :
    FElement(label, material, nodes) {
}

SolidElement::SolidElement(int label, int material, vector<int> nodes, vector<vector<double>> _nodeP, vector<vector<double>> _intP) :
    FElement(label, material, nodes) {
    nodeP = _nodeP;
    intP = _intP;
};


// RrsρπΤ·
// p - vfί_
// sf - `σΦsρ
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


// `σΦΜωz [ dNi/dx dNi/dy dNi/dz ] πΤ·
// p - vfί_
// ja - Rrsρ
// sf - `σΦsρ
void SolidElement::grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, vector<vector<double>> out) {

    int count = nodeCount();

    double ji[9];

    //  3x3 sρΜtsρπίι
    // 3x3ΜsρπόΝ
    vector<vector<double>> a;
    for (int i = 0; i < 3; ++i) {
        vector<double> b;
        for (int j = 0; j < 3; ++j) {
            b.push_back(ja[i + j]);
        }
        a.push_back(b);
    }

    vector<vector<double>> inv_a;
    numeric::getInverse(a, inv_a);

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


// c - ΟΚ}gbNXΜ]usρπΤ·
// grad - `σΦΜωz
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





// Οͺ_Μ`σΦ}gbNX [ NiNj ] πΤ·
// p - vfί_
// x - Μ,Ε,ΔΐW
// w - dέW
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


// Οͺ_ΜgU}gbNX [ ήNiEήNj ] πΤ·
// p - vfί_
// x - Μ,Ε,ΔΐW
// w - dέW
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


// ΏΚ}gbNXπΤ·
// p - vfί_
// dens - ήΏΜ§x
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


// sρΜaπvZ·ι
// a - ξsρ
// da - Α¦ιsρ
void SolidElement::addMatrix(vector<vector<double>> a, vector<vector<double>> da) {
    for (int i = 0; i < a.size(); i++) {
        for (int j = 0; j < a[i].size(); j++) {
            a[i][j] += da[i][j];
        }
    }
}


// «}gbNXπΤ·
// p - vfί_
// d1 - Ν - c}gbNX
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


// `σΦ}gbNX [ ηNiNjdV ] πΤ·
// p - vfί_
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

// gU}gbNX [ ηήNiEήNjdV ] πΤ·
// p - vfί_
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


// τ½«}gbNXπΤ·
// p - vfί_
// u - ί_ΟΚ
// d1 - Ν - c}gbNX
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


// ί_cEΝπΤ·
// p - vfί_
// u - ί_ΟΚ
// d1 - Ν - c}gbNX
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

// vfΰΜcxNgπΤ·
// p - vfί_
// v - ί_ΟΚxNg
// x - Μ,Ε,ΔΐW
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


// vfcEΝπΤ·
// p - vfί_
// u - ί_ΟΚ
// d1 - Ν - c}gbNX
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

// vfπ\·ΆρπΤ·
// materials - ήΏ
// p - ί_
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