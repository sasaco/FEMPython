#include "SolidElement.h"

//--------------------------------------------------------------------//
// ソリッド要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
// nodeP - 節点のξ,η,ζ座標
// intP - 積分点のξ,η,ζ座標,重み係数
SolidElement::SolidElement() : FElement() { }
SolidElement::SolidElement(string label, string material, vector<string> nodes) :
    FElement(label, material, nodes) {
};


// ヤコビ行列を返す
// p - 要素節点
// sf - 形状関数行列
MatrixXd SolidElement::jacobianMatrix(vector<FENode> p, MatrixXd sf) {

    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(3, 3);

    for (int i = 0; i < count; i++) {
        double pix = p[i].x;
        double piy = p[i].y;
        double piz = p[i].z;
        for (int j = 0; j < 3; j++) {
            double sfij = sf(i, j + 1);
            result(0, j) += sfij * pix;
            result(1, j) += sfij * piy;
            result(2, j) += sfij * piz;
        }
    }

    return result;
}


// 形状関数の勾配 [ dNi/dx dNi/dy dNi/dz ] を返す
// p - 要素節点
// ja - ヤコビ行列
// sf - 形状関数行列
MatrixXd SolidElement::grad(vector<FENode> p, MatrixXd ja, MatrixXd sf) {

    int count = nodeCount();

    // ◆ 3x3 行列の逆行列を求める
    MatrixXd ji = ja.inverse();

    MatrixXd result(count, 3);

    for (int i = 0; i < count; i++) {
        result(i, 0) = ji(0, 0) * sf(i, 1) + ji(0, 1) * sf(i, 2) + ji(0, 2) * sf(i, 3);
        result(i, 1) = ji(1, 0) * sf(i, 1) + ji(1, 1) * sf(i, 2) + ji(1, 2) * sf(i, 3);
        result(i, 2) = ji(2, 0) * sf(i, 1) + ji(2, 1) * sf(i, 2) + ji(2, 2) * sf(i, 3);
    }

    return result;
};


// 歪 - 変位マトリックスの転置行列を返す
// grad - 形状関数の勾配
MatrixXd SolidElement::strainMatrix(MatrixXd grad) {

    int count = nodeCount();

    MatrixXd m = MatrixXd::Zero(3 * count, 6);

    for (int i = 0; i < count; i++) {
        int i3 = 3 * i;
        m(i3, 0) = grad(i, 0);
        m(i3 + 1, 1) = grad(i, 1);
        m(i3 + 2, 2) = grad(i, 2);
        m(i3, 3) = grad(i, 1);
        m(i3 + 1, 3) = grad(i, 0);
        m(i3 + 1, 4) = grad(i, 2);
        m(i3 + 2, 4) = grad(i, 1);
        m(i3, 5) = grad(i, 2);
        m(i3 + 2, 5) = grad(i, 0);
    }

    return m;
}


// 積分点の形状関数マトリックス [ NiNj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
MatrixXd SolidElement::shapePart(vector<FENode> p, VectorXd x, double w) {

    MatrixXd sf= shapeFunction(x(0), x(1), x(2));
    MatrixXd ja = jacobianMatrix(p, sf);
    int count = nodeCount();
    double det = ja.determinant();
    double coef = w * abs(det);

    MatrixXd result(count, count);

    for (int i = 0; i < count; i++) {
        double cf2 = coef * sf(i, 0);
        for (int j = 0; j < count; j++) {
            result(i, j) = cf2 * sf(j, 0);
        }
    }

    return result;
}


// 積分点の拡散マトリックス [ ∇Ni・∇Nj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
MatrixXd SolidElement::gradPart(vector<FENode> p, VectorXd x, double w) {

    MatrixXd sf = shapeFunction(x(0), x(1), x(2));
    MatrixXd ja = jacobianMatrix(p, sf);
    MatrixXd gr = grad(p, ja, sf);
    int count = nodeCount();
    double det = ja.determinant();
    double coef = w * abs(det);

    MatrixXd result(count, count);

    for (int i = 0; i < count; i++) {
        double c1 = coef * gr(i, 0);
        double c2 = coef * gr(i, 1);
        double c3 = coef * gr(i, 2);
        for (int j = 0; j < count; j++) {
            result(i, j) = c1 * gr(j, 0) + c2 * gr(j, 1) + c3 * gr(j, 2);
        }
    }

    return result;
};


// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
MatrixXd SolidElement::massMatrix(vector<FENode> p, double dens) {
    
    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(3 * count, 3 * count);

    for (int i = 0; i < intP.size(); i++) {
        MatrixXd sf = shapeFunction(intP(i, 0), intP(i, 1), intP(i, 2));
        MatrixXd ja = jacobianMatrix(p, sf);
        double det = ja.determinant();
        double coef = intP(i, 3) * dens * abs(det);

        for (int i1 = 0; i1 < count; i1++) {
            for (int j1 = 0; j1 < count; j1++) {
                double value = coef * sf(i1, 0) * sf(j1, 0);
                int i3 = 3 * i1;
                int j3 = 3 * j1;
                result(i3, j3) += value;
                result(i3 + 1, j3 + 1) += value;
                result(i3 + 2, j3 + 2) += value;
            }
        }
    }

    return result;
}


// 剛性マトリックスを返す
// p - 要素節点
// d1 - 応力 - 歪マトリックス
MatrixXd SolidElement::stiffnessMatrix(vector<FENode> p, MatrixXd d1) {
    
    int count = 3 * nodeCount();
    MatrixXd result = MatrixXd::Zero(count, count);

    for (int i = 0; i < intP.rows(); i++) {
        MatrixXd sf = shapeFunction(intP(i, 0), intP(i, 1), intP(i, 2));
        MatrixXd ja = jacobianMatrix(p, sf);
        MatrixXd gr = grad(p, ja, sf);
        MatrixXd sm = strainMatrix(gr);
        double det = ja.determinant();
        double coef = intP(i, 3) * abs(det);
        MatrixXd ks = stiffPart(d1, sm, coef);
        result += ks;
    }

    return result;
}


// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
MatrixXd SolidElement::shapeFunctionMatrix(vector<FENode> p, double coef) {

    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(count, count);

    for (int i = 0; i < intP.size(); i++) {
        MatrixXd sp = shapePart(p, intP.row(i), coef * intP(i, 3));
        result += sp;
    }

    return result;
}

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
MatrixXd SolidElement::gradMatrix(vector<FENode> p, double coef) {

    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(count, count);

    for (int i = 0; i < intP.size(); i++) {
        MatrixXd gp = gradPart(p, intP.row(i), coef * intP(i, 3));
        result += gp;
    }

    return result;
};


// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
MatrixXd SolidElement::geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) {

    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(3 * count, 3 * count);
    VectorXd v = FElement::toArray(u, 3);

    for (int i = 0; i < intP.size(); i++) {

        MatrixXd sf= shapeFunction(intP(i, 0), intP(i, 1), intP(i, 2));
        MatrixXd ja = jacobianMatrix(p, sf);
        MatrixXd gr = grad(p, ja, sf);
        MatrixXd sm = strainMatrix(gr);
        VectorXd vm = v * sm;
        VectorXd str = d1 * vm;

        double det = ja.determinant();
        double w = intP(i, 3) * abs(det);

        for (int i1 = 0; i1 < count; i1++) {
            int i3 = 3 * i1;

            for (int j1 = 0; j1 < count; j1++) {
                int j3 = 3 * j1;

                double s = w * (gr(i1, 0) * (str(0) * gr(j1, 0) + str(3) * gr(j1, 1) + str(5) * gr(j1, 2)) +
                    gr(i1, 1) * (str(3) * gr(j1, 0) + str(1) * gr(j1, 1) + str(4) * gr(j1, 2)) +
                    gr(i1, 2) * (str(5) * gr(j1, 0) + str(4) * gr(j1, 1) + str(2) * gr(j1, 2)));

                result(i3, j3) += s;
                result(i3 + 1, j3 + 1) += s;
                result(i3 + 2, j3 + 2) += s;
            }
        }
    }

    return result;
}


// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
tuple<vector<Strain>, vector<Stress>, vector<double>> 
    SolidElement::strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) {

    int count = nodeCount();
    VectorXd v = FElement::toArray(u, 3);

    vector<Strain> strain;
    vector<Stress> stress;
    vector<double> energy;

    for (int i = 0; i < count; i++) {

        VectorXd eps = strainPart(p, v, nodeP.row(i));
        Strain stra = Strain(eps);
        strain.push_back(stra);

        VectorXd str = d1 * eps;
        Stress stre = Stress(str);
        stress.push_back(stre);

        double eng = 0.5 * stra.innerProduct(stre);
        energy.push_back(eng);
    }

    tuple<vector<Strain>, vector<Stress>, vector<double>> result = make_tuple(strain, stress, energy);

    return result;
}

// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// x - ξ,η,ζ座標
VectorXd SolidElement::strainPart(vector<FENode> p, VectorXd v, VectorXd x) {

    MatrixXd sf = shapeFunction(x(0), x(1), x(2));
    MatrixXd ja = jacobianMatrix(p, sf);
    MatrixXd gr = grad(p, ja, sf);
    MatrixXd sm = strainMatrix(gr);
    VectorXd result = sm.transpose() * v;

    return result;
};


// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
tuple<Strain, Stress, double> SolidElement::elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1) {

    VectorXd v = FElement::toArray(u, 3);
    auto cf = 1 / (double)intP.rows();

    VectorXd strain = VectorXd::Zero(6);
    VectorXd stress = VectorXd::Zero(6);
    double energy = 0;

    for (int i = 0; i < intP.rows(); i++) {
        VectorXd eps = strainPart(p, v, intP.row(i));
        strain += eps;

        VectorXd str = d1 * eps;
        stress += str;

        energy += eps.dot(str);
    }

    strain *= cf;
    stress *= cf;
    energy *= 0.5 * cf;

    tuple<Strain, Stress, double> result = make_tuple(Strain(strain), Stress(stress), energy);

    return result;
}


// 要素を表す文字列を返す
// materials - 材料
// p - 節点
string SolidElement::toString(vector<Material> materials, vector<FENode> p) {

    //Material mat = materials[material];

    //string s = "";
    //for (int i = 0; i < nodes.size(); i++) {
    //    s += '\t';
    //    FENode n = p[nodes[i]];
    //    s += n.label;
    //}

    //return format("{}\t{}\t{}\t{}",
    //    getName(), label, mat.label, s);

    return "SolidElement";
};

// 三角形の立体角を球面過剰から求める
// p0 - 基点
// p1,p2,p3 - 頂点
double SolidElement::solidAngle(FENode p0, FENode p1, FENode p2, FENode p3) {
    FENode v1, v2, v3;
    p1.clone(v1);
    v1.sub(p0);
    p2.clone(v2);
    v2.sub(p0);
    p3.clone(v3);
    v3.sub(p0);

    FENode v12, v23, v31;
    v1.clone(v12);
    v12.cross(v2).normalize();
    v2.clone(v23);
    v23.cross(v3).normalize();
    v3.clone(v31);
    v31.cross(v1).normalize();
    double a1 = max(min(-v12.dot(v31), 1.0), -1.0);
    double a2 = max(min(-v23.dot(v12), 1.0), -1.0);
    double a3 = max(min(-v31.dot(v23), 1.0), -1.0);
    double result = acos(a1) + acos(a2) + acos(a3) - PI;
    return result;
}

// 平面上の角度を求める
// p0 - 基点
// p1,p2 - 頂点
double SolidElement::planeAngle(FENode p0, FENode p1, FENode p2) {
    FENode v1, v2, v3;
    p1.clone(v1);
    v1.sub(p0);
    p2.clone(v2);
    v2.sub(p0);

    FENode v12, v23;
    v1.clone(v12);
    v12.normalize();
    v2.clone(v23);
    v23.normalize();

    double result = acos(min(max(v12.dot(v23), 0.0), 1.0));
    return result;
}