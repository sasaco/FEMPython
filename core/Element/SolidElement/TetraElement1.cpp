#include "TetraElement1.h";

//--------------------------------------------------------------------//
// 四面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
TetraElement1::TetraElement1() : SolidElement() {};
TetraElement1::TetraElement1(string label, string material, vector<string> nodes) :
    SolidElement(label, material, nodes) {
};

// 要素名称を返す
string TetraElement1::getName() {
    return "TetraElement1";
}

// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
MatrixXd TetraElement1::shapeFunction(double xsi, double eta, double zeta) {
    MatrixXd result;
    result << 1 - xsi - eta - zeta, -1, -1, -1,
              xsi, 1, 0, 0,
              eta, 0, 1, 0,
              zeta, 0, 0, 1;
    return result;
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
MatrixXd TetraElement1::grad(vector<FENode> p, double ja) {

    int count = nodeCount();

    double ji = 1 / ja;

    MatrixXd result(count, 3);

    for (int i = 0; i < count; i++) {
        ji = -ji;
        int i2 = (i + 1) % 4;
        int i3 = (i + 2) % 4;
        int i4 = (i + 3) % 4;
        double p2x = p[i2].x;
        double p2y = p[i2].y;
        double p2z = p[i2].z;
        result(i, 0) = ji * ((p[i3].y - p2y) * (p[i4].z - p2z) - (p[i4].y - p2y) * (p[i3].z - p2z));
        result(i, 1) = ji * ((p[i3].z - p2z) * (p[i4].x - p2x) - (p[i4].z - p2z) * (p[i3].x - p2x));
        result(i, 2) = ji * ((p[i3].x - p2x) * (p[i4].y - p2y) - (p[i4].x - p2x) * (p[i3].y - p2y));
    }

    return result;
}


// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
MatrixXd TetraElement1::massMatrix(vector<FENode> p, double dens) {

    MatrixXd result = MatrixXd::Zero(12, 12);

    double ja = jacobian(p);

    double value = dens * ja / 60;
    double vh = 0.5 * value;

    for (int i = 0; i < 12; i += 3) {
        result(i, i) = value;
        result(i + 1, i + 1) = value;
        result(i + 2, i + 2) = value;
        for (int j = i + 3; j < 12; j += 3) {
            result(i, j) = vh;
            result(i + 1, j + 1) = vh;
            result(i + 2, j + 2) = vh;
            result(j, i) = vh;
            result(j + 1, i + 1) = vh;
            result(j + 2, i + 2) = vh;
        }
    }
    return result;
}


// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
MatrixXd TetraElement1::stiffnessMatrix(vector<FENode> p, MatrixXd d1) {

    double ja = jacobian(p);
    auto gr = grad(p, ja);
    auto sm = strainMatrix(gr);
    auto result = stiffPart(d1, sm, C1_6 * abs(ja));

    return result;
}



// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
MatrixXd TetraElement1::shapeFunctionMatrix(vector<FENode> p, double coef) {

    double ja = jacobian(p);

    double value = 0.1 * C1_6 * coef * abs(ja);
    double vh = 0.5 * value;

    MatrixXd result;
    result << value, vh, vh, vh,
        vh, value, vh, vh,
        vh, vh, value, vh,
        vh, vh, vh, value;

    return result;
}


// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
MatrixXd TetraElement1::gradMatrix(vector<FENode> p, double coef) {

    int count = nodeCount();

    MatrixXd result = MatrixXd::Zero(4, 4);

    double ja = jacobian(p);
    auto gr = grad(p, ja);
    double  coef2 = C1_6 * coef * abs(ja);

    for (int i = 0; i < 4; i++) {
        for (double j = 0; j < 4; j++) {
            result(i, j) = coef2 * (gr(i, 0) * gr(j, 0) + gr(i, 1) * gr(j, 1) +
                gr(i, 2) * gr(j, 2));
        }
    }
    return result;
}


// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
MatrixXd TetraElement1::geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) {

    int count = nodeCount();

    MatrixXd result = MatrixXd::Zero(3, 3);

    double ja = jacobian(p);
    MatrixXd gr = grad(p, ja);
    MatrixXd sm = strainMatrix(gr);
    VectorXd v = toArray(u, 3);
    VectorXd vm = v * sm;
    VectorXd str = d1 * vm;
    double w = C1_6 * abs(ja);

    for (int i1 = 0; i1 < count; i1++) {
        
        int i3 = 3 * i1;

        for (int j1 = 0; j1 < count; j1++) {
            int j3 = 3 * j1;

            double s = w * (gr(i1, 0) * (str[0] * gr(j1, 0) + str[3] * gr(j1, 1) + str[5] * gr(j1, 2)) +
                gr(i1, 1) * (str[3] * gr(j1, 0) + str[1] * gr(j1, 1) + str[4] * gr(j1, 2)) +
                gr(i1, 2) * (str[5] * gr(j1, 0) + str[4] * gr(j1, 1) + str[2] * gr(j1, 2)));

            result(i3, j3) = s;
            result(i3 + 1, j3 + 1) = s;
            result(i3 + 2, j3 + 2) = s;
        }
    }

    return result;
}


// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
tuple<vector<Strain>, vector<Stress>, vector<double>> 
    TetraElement1::strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) {

    double ja = jacobian(p);

    MatrixXd gr = grad(p, ja);
    MatrixXd sm = strainMatrix(gr);
    VectorXd v = toArray(u, 3);
    VectorXd eps = v * sm;
    Strain stra = Strain(eps);
    VectorXd str = d1 * eps;
    Stress stre = Stress(str);

    double eng = 0.5 * stra.innerProduct(stre);
    
    vector<Strain> strain;
    vector<Stress> stress;
    vector<double> energy;

    for (int i = 0; i < 4; i++) {
        strain.push_back(stra);
        stress.push_back(stre);
        energy.push_back(eng);
    }

    tuple<vector<Strain>, vector<Stress>, vector<double>> result = make_tuple(strain, stress, energy);

    return result;

};


// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
tuple<Strain, Stress, double> 
    TetraElement1::elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1){

    double ja= jacobian(p);
    MatrixXd gr = grad(p, ja);
    MatrixXd sm = strainMatrix(gr);
    VectorXd v = toArray(u, 3);
    VectorXd eps = v * sm;
    VectorXd str = d1 * eps;

    double energy = 0.5 * eps.dot(str);

    tuple<Strain, Stress, double> result = make_tuple(Strain(eps), Stress(str), energy);

    return result;
}
