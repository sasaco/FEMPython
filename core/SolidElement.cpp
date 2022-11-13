#include "FElement.h";
#include "FENode.h";

#include <string>
#include <vector>
using namespace std;


class SolidElement : public FElement {

private:

    // �l�ʑ�2���v�f�̐ߓ_�̃�,��,�č��W
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

    // �l�ʑ�2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
    const double TETRA2_INT[4][4] = {
        {GTETRA2[0], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[1], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[1], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[0], GTETRA2[1], C1_24}
    };

    // ���`1���v�f�̐ߓ_�̃�,��,�č��W
    const double WEDGE1_NODE[6][3] = {
        {0, 0, -1}, 
        {1, 0, -1}, 
        {0, 1, -1}, 
        {0, 0, 1}, 
        {1, 0, 1}, 
        {0, 1, 1}
    };

    // ���`1���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
    const double WEDGE1_INT[2][4] = {
        {C1_3, C1_3, GX2[0], 0.5}, 
        {C1_3, C1_3, GX2[1], 0.5}
    };

    // ���`2���v�f�̐ߓ_�̃�,��,�č��W
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


    // ���`2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
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

    // �Z�ʑ�1���v�f�̐ߓ_�̃�,��,�č��W
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

    // �Z�ʑ�1���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
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

    // �Z�ʑ�2���v�f�̐ߓ_�̃�,��,�č��W
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

    // �Z�ʑ�2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
    double HEXA2_INT[9][4]; // �R���X�g���N�^�ŏ�����

    // �Z�ʑ�1���v�f�̎��ʃ}�g���b�N�X�W��
    double HEXA1_MASS_BASE[8][8]; // �R���X�g���N�^�ŏ�����



    vector<vector<double>> nodeP;
    vector<double[3]> intP;

public:

    SolidElement(int label, int material, vector<int> nodes, vector <vector<double>> _nodeP, vector<double[3]> _intP);

    void jacobianMatrix(vector<FENode> p, vector<vector<double>> sf, double out[9]);

    void grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, vector<vector<double>> out);

    void strainMatrix(vector<vector<double>> grad, vector<vector<double>> out);

    virtual void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) = 0;

    double determinant(double ja[9]);

    void shapePart(vector<FENode> p, double x[3], double w, vector<vector<double>> out);

    void gradPart(vector<FENode> p, double x[3], double w, vector<vector<double>> out);

    void massMatrix(vector<FENode> p, double dens, vector<vector<double>> out);

    void addMatrix(vector<vector<double>> a, vector<vector<double>> da);

    void stiffnessMatrix(vector<FENode> p, vector<vector<double>> d1, vector<vector<double>> out);

    void shapeFunctionMatrix(vector<FENode> p, double coef, vector<vector<double>> out);

    void gradMatrix(vector<FENode> p, double coef, vector<vector<double>> out);

    double dotVV(vector<double> x, vector<double> y);

    void dotMV(vector<vector<double>> x, vector<double> y, vector<double> out);

    void dotVM(vector<double> x, vector<vector<double>> y, vector<double> out);

    void geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, vector<vector<double>> out);


};



//--------------------------------------------------------------------//
// �\���b�h�v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
// nodeP - �ߓ_�̃�,��,�č��W
// intP - �ϕ��_�̃�,��,�č��W,�d�݌W��
SolidElement::SolidElement(int label, int material, vector<int> nodes, vector<vector<double>> _nodeP, vector<double[3]> _intP) :
    FElement(label, material, nodes) {

    nodeP = _nodeP;
    intP = _intP;

    // �Z�ʑ�2���v�f�̐ϕ��_�̃�,��,�č��W,�d�݌W��
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

    // �Z�ʑ�1���v�f�̎��ʃ}�g���b�N�X�W��
    for (int i = 0; i < 8; i++) {
        for (int j = 0; j < 8; j++) {
            double s = abs(HEXA1_NODE[i][0] - HEXA1_NODE[j][0]) +
                       abs(HEXA1_NODE[i][1] - HEXA1_NODE[j][1]) +
                       abs(HEXA1_NODE[i][2] - HEXA1_NODE[j][2]);
            HEXA1_MASS_BASE[i][j] = pow(0.5, 0.5 * s) / 27;
        }
    }

};


// ���R�r�s���Ԃ�
// p - �v�f�ߓ_
// sf - �`��֐��s��
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


// �`��֐��̌��z [ dNi/dx dNi/dy dNi/dz ] ��Ԃ�
// p - �v�f�ߓ_
// ja - ���R�r�s��
// sf - �`��֐��s��
void SolidElement::grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, vector<vector<double>> out) {

    int count = nodeCount();

    double ji[9];

    // �� 3x3 �s��̋t�s������߂�
    // 3x3�̍s������
    double a[3][3];
    for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 3; ++j) {
            a[i][j] = ja[i + j];
        }
    }

    //�P�ʍs������
    double inv_a[3][3];
    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            inv_a[i][j] = (i == j) ? 1.0 : 0.0;
        }
    }
    //�|���o���@
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
    // �� 


    for (int i = 0; i < count; i++) {
        out[i][0] = ji[0] * sf[i][1] + ji[3] * sf[i][2] + ji[6] * sf[i][3];
        out[i][1] = ji[1] * sf[i][1] + ji[4] * sf[i][2] + ji[7] * sf[i][3];
        out[i][2] = ji[2] * sf[i][1] + ji[5] * sf[i][2] + ji[8] * sf[i][3];
    }
};


// �c - �ψʃ}�g���b�N�X�̓]�u�s���Ԃ�
// grad - �`��֐��̌��z
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


// 3x3�̍s�񎮂�Ԃ�
double SolidElement::determinant(double ja[9]) {
    
    double det = 0;
    // 3x3�̍s������
    double a[3][3];
    for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 3; ++j) {
            a[i][j] = ja[i + j];
        }
    }
    // �s�񎮂̌v�Z
    for (int i = 0; i < 3; ++i) {
        det = det + a[0][i] * (a[1][(i + 1) % 3] * a[2][(i + 2) % 3] - a[1][(i + 2) % 3] * a[2][(i + 1) % 3]);
    }

    return det;
}


// �ϕ��_�̌`��֐��}�g���b�N�X [ NiNj ] ��Ԃ�
// p - �v�f�ߓ_
// x - ��,��,�č��W
// w - �d�݌W��
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


// �ϕ��_�̊g�U�}�g���b�N�X [ ��Ni�E��Nj ] ��Ԃ�
// p - �v�f�ߓ_
// x - ��,��,�č��W
// w - �d�݌W��
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


// ���ʃ}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// dens - �ޗ��̖��x
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


// �s��̘a���v�Z����
// a - ��s��
// da - ������s��
void SolidElement::addMatrix(vector<vector<double>> a, vector<vector<double>> da) {
    for (int i = 0; i < a.size(); i++) {
        for (int j = 0; j < a[i].size(); j++) {
            a[i][j] += da[i][j];
        }
    }
}


// �����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// d1 - ���� - �c�}�g���b�N�X
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


// �`��֐��}�g���b�N�X [ ��NiNjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
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

// �g�U�}�g���b�N�X [ ���Ni�E��NjdV ] ��Ԃ�
// p - �v�f�ߓ_
// coef - �W��
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


double SolidElement::dotVV(vector<double> x, vector<double> y) {

    int n = x.size();
    double ret = x[n - 1] * y[n - 1];

    int i = 0;
    for (i = n - 2; i >= 1; i -= 2) {
        int i1 = i - 1;
        ret += x[i] * y[i] + x[i1] * y[i1];
    }
    if (i = 0) { 
        ret += x[0] * y[0]; 
    }
    return ret;
}


void SolidElement::dotMV(vector<vector<double>> x, vector<double> y, vector<double> out) {
    int p = x.size();
    int q = y.size();

    for (int i = 0; i < p; ++i) {
        out.push_back(0);
    }

    for (int i = p - 1; i >= 0; i--) {
        out[i] = dotVV(x[i], y);
    }
}


void SolidElement::dotVM(vector<double> x, vector<vector<double>> y, vector<double> out) {

    int p = x.size();
    int q = y[0].size();

    for (int i = 0; i < q; ++i) {
        out.push_back(0);
    }
    
    for (int k = q - 1; k >= 0; k--) {
        double woo = x[p - 1] * y[p - 1][k];
        int j = 0;
        for (j = p - 2; j >= 1; j -= 2) {
            int i0 = j - 1;
            woo += x[j] * y[j][k] + x[i0] * y[i0][k];
        }
        if (j = 0) { 
            woo += x[0] * y[0][k];
        }
        out[k] = woo;
    }
}


// �􉽍����}�g���b�N�X��Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
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
        dotVM(v, sm, vm);

        vector<double> str;
        dotMV(d1, vm, str);

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


// �ߓ_�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
void SolidElement::strainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1) {

    int count = nodeCount();
    
    vector<double> v;
    toArray(u, 3, v);

    vector<Strain> strain;
    vector<Stress> stress;
    vector<double> energy;

    for (int i = 0; i < count; i++) {
        vector<double> eps;
        strainPart(p, v, nodeP[i], eps);

        strain.push_back(Strain(eps))-;
        var str = numeric.dotMV(d1, eps);
        stress[i] = new Stress(str);
        energy[i] = 0.5 * strain[i].innerProduct(stress[i]);
    }
    return[strain, stress, energy];
};

// �v�f���̘c�x�N�g����Ԃ�
// p - �v�f�ߓ_
// v - �ߓ_�ψʃx�N�g��
// x - ��,��,�č��W
SolidElement.prototype.strainPart = function(p, v, x) {
    var sf = this.shapeFunction(x[0], x[1], x[2]);
    var ja = this.jacobianMatrix(p, sf);
    var sm = this.strainMatrix(this.grad(p, ja, sf));
    return dotVM(v, sm);
};


// �v�f�c�E���͂�Ԃ�
// p - �v�f�ߓ_
// u - �ߓ_�ψ�
// d1 - ���� - �c�}�g���b�N�X
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

// �v�f��\���������Ԃ�
// materials - �ޗ�
// p - �ߓ_
SolidElement.prototype.toString = function(materials, p) {
    var s = this.getName() + '\t' + this.label.toString(10) + '\t' +
        materials[this.material].label.toString(10);
    for (var i = 0; i < this.nodes.length; i++) {
        s += '\t' + p[this.nodes[i]].label.toString(10);
    }
    return s;
};