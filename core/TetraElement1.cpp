#include "TetraElement1.h";

//--------------------------------------------------------------------//
// 四面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
TetraElement1::TetraElement1(int label, int material, vector<int> nodes) :
    SolidElement(label, material, nodes) {
};

// 要素名称を返す
string TetraElement1::getName() {
    return "TetraElement1";
}

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
void TetraElement1::shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) {
    out = {
        {1 - xsi - eta - zeta, -1, -1, -1},
        {xsi, 1, 0, 0},
        {eta, 0, 1, 0},
        {zeta, 0, 0, 1}
    };
}

// ヤコビアンを返す
// p - 要素節点
double TetraElement1::jacobian(vector<FENode> p) {
    double p0x = p[0].x;
    double p0y = p[0].y;
    double p0z = p[0].z;
    double j11 = (p[2].y - p0y) * (p[3].z - p0z) - (p[3].y - p0y) * (p[2].z - p0z);
    double j21 = (p[3].y - p0y) * (p[1].z - p0z) - (p[1].y - p0y) * (p[3].z - p0z);
    double j31 = (p[1].y - p0y) * (p[2].z - p0z) - (p[2].y - p0y) * (p[1].z - p0z);
    return (p[1].x - p0x) * j11 + (p[2].x - p0x) * j21 + (p[3].x - p0x) * j31;
};


// 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
// p - 要素節点
// ja - ヤコビアン
void TetraElement1::grad(vector<FENode> p, double ja, vector<vector<double>> out) {

    int count = nodeCount();

    double ji = 1 / ja;

    for (int i = 0; i < count; i++) {
        ji = -ji;
        int i2 = (i + 1) % 4;
        int i3 = (i + 2) % 4;
        int i4 = (i + 3) % 4;
        double p2x = p[i2].x;
        double p2y = p[i2].y;
        double p2z = p[i2].z;
        vector<double> gr = {
            ji* ((p[i3].y - p2y) * (p[i4].z - p2z) - (p[i4].y - p2y) * (p[i3].z - p2z)),
            ji* ((p[i3].z - p2z) * (p[i4].x - p2x) - (p[i4].z - p2z) * (p[i3].x - p2x)),
            ji* ((p[i3].x - p2x) * (p[i4].y - p2y) - (p[i4].x - p2x) * (p[i3].y - p2y))
        };
        out.push_back(gr);
    }
};


// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
void TetraElement1::massMatrix(vector<FENode> p, double dens, vector<vector<double>> out) {

    for (int i = 0; i < 12; ++i) {
        vector<double> m;
        for (int j = 0; j < 12; ++j) {
            m.push_back(0);
        }
        out.push_back(m);
    }

    double ja = jacobian(p);

    double value = dens * ja / 60;
    double vh = 0.5 * value;

    for (int i = 0; i < 12; i += 3) {
        out[i][i] = value;
        out[i + 1][i + 1] = value;
        out[i + 2][i + 2] = value;
        for (int j = i + 3; j < 12; j += 3) {
            out[i][j] = vh;
            out[i + 1][j + 1] = vh;
            out[i + 2][j + 2] = vh;
            out[j][i] = vh;
            out[j + 1][i + 1] = vh;
            out[j + 2][i + 2] = vh;
        }
    }
};


// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
void TetraElement1::stiffnessMatrix(vector<FENode> p, vector<vector<double>> d1, vector<vector<double>> out) {

    double ja = jacobian(p);

    vector<vector<double>> gr;
    grad(p, ja, gr);

    vector<vector<double>> sm;
    strainMatrix(gr, sm);

    stiffPart(d1, sm, C1_6 * abs(ja), out);
};



// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
void TetraElement1::shapeFunctionMatrix(vector<FENode> p, double coef, vector<vector<double>> out) {

    double ja = jacobian(p);

    double value = 0.1 * C1_6 * coef * abs(ja);
    double vh = 0.5 * value;

    out = {
        {value, vh, vh, vh}, 
        {vh, value, vh, vh}, 
        {vh, vh, value, vh},
        {vh, vh, vh, value}
    };
};


// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
void TetraElement1::gradMatrix(vector<FENode> p, double coef, vector<vector<double>> out) {

    int count = nodeCount();

    for (int i = 0; i < 4; ++i) {
        vector<double> g;
        for (int j = 0; j < 4; ++j) {
            g.push_back(0);
        }
        out.push_back(g);
    }

    double ja = jacobian(p);

    vector<vector<double>> gr;
    grad(p, ja, gr);

    double  coef2 = C1_6 * coef * abs(ja);

    for (int i = 0; i < 4; i++) {
        for (double j = 0; j < 4; j++) {
            out[i][j] = coef2 * (gr[i][0] * gr[j][0] + gr[i][1] * gr[j][1] +
                gr[i][2] * gr[j][2]);
        }
    }
};


// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
void TetraElement1::geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, vector<vector<double>> out) {

    int count = nodeCount();

    for (int i = 0; i < 3 * count; ++i) {
        vector<double> kk;
        for (int j = 0; j < 3 * count; ++j) {
            kk.push_back(0);
        }
        out.push_back(kk);
    }

    double ja = jacobian(p);

    vector<vector<double>> gr;
    grad(p, ja, gr);

    vector<vector<double>> sm;
    strainMatrix(gr, sm);

    vector<double> v;
    toArray(u, 3, v);

    vector<double> vm;
    numeric::dotVM(v, sm, vm);

    vector<double> str;
    numeric::dotMV(d1, vm, str);

    double w = C1_6 * abs(ja);


    for (int i1 = 0; i1 < count; i1++) {
        
        int i3 = 3 * i1;
        vector<double> gri = gr[i1];

        for (int j1 = 0; j1 < count; j1++) {
            int j3 = 3 * j1;
            vector<double> grj = gr[j1];

            double s = w * (gri[0] * (str[0] * grj[0] + str[3] * grj[1] + str[5] * grj[2]) +
                gri[1] * (str[3] * grj[0] + str[1] * grj[1] + str[4] * grj[2]) +
                gri[2] * (str[5] * grj[0] + str[4] * grj[1] + str[2] * grj[2]));

            out[i3][j3] = s;
            out[i3 + 1][j3 + 1] = s;
            out[i3 + 2][j3 + 2] = s;
        }
    }
};


// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
void TetraElement1::strainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
    vector<Strain> _Strain, vector<Stress> _Stress, vector<double> _Energy) {

    double ja = jacobian(p);

    vector<vector<double>> gr;
    grad(p, ja, gr);

    vector<vector<double>> sm;
    strainMatrix(gr, sm);

    vector<double> v;
    toArray(u, 3, v);

    vector<double> eps;
    numeric::dotVM(v, sm, eps);

    Strain strain = Strain(eps);

    vector<double> str;
    numeric::dotMV(d1, eps, str);

    Stress stress = Stress(stress);
    doule energy = 0.5 * _Strain.innerProduct(stress);

    for (int i = 0; i < 4; i++) {
        _Strain.push_back(strain);
        _Stress.push_back(stress);
        _Energy.push_back(energy);
    }

};


// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
void TetraElement1::elementStrainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
    Strain _Strain, Stress _Stress, double energy);


    double ja= jacobian(p);

    vector<vector<double>> gr;
    grad(p, ja, gr);

    vector<vector<double>> sm;
    strainMatrix(gr, sm);

    vector<double> v;
    toArray(u, 3, v);

    vector<double> eps;
    numeric::dotVM(v, sm, eps);

    vector<double> str;
    numeric::dotMV(d1, eps, str);


    energy = 0.5 * numeric.dotVV(eps, str);

    _Strain = Strain(strain);
    _Stress = Stress(stress);


};
