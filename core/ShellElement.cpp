#include "ShellElement.h"

//--------------------------------------------------------------------//
// シェル要素
// label - 要素ラベル
// material - 材料のインデックス
// param - シェルパラメータのインデックス
// nodes - 節点番号
// nodeP - 節点のξ,η座標
// intP - 積分点のξ,η座標,重み係数
ShellElement::ShellElement() : FElement() {
    isShell = true;
}
ShellElement::ShellElement(int _label, int material, int _param, vector<int> nodes, 
    MatrixXd _nodeP, MatrixXd _intP) :
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
MatrixXd ShellElement::jacobianMatrix(vector<FENode> p, MatrixXd sf, Vector3Dim n, double t) {
    
    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(3, 3);

    for (int i = 0; i < count; i++) {
        
        FENode ppi = p[i];
        double pix = ppi.x;
        double piy = ppi.y;
        double piz = ppi.z;

        for (int j = 0; j < 2; j++) {
            double sfij = sf(i, j + 1);
            result(0, j) += sfij * pix;
            result(1, j) += sfij * piy;
            result(2, j) += sfij * piz;
        }
    }
    result(0, 2) = 0.5 * t * n[0];
    result(1, 2) = 0.5 * t * n[1];
    result(2, 2) = 0.5 * t * n[2];

    return result;
}


// 逆ヤコビ行列を返す
// ja - ヤコビ行列
// d - 方向余弦マトリックス
MatrixXd ShellElement::jacobInv(MatrixXd ja, MatrixXd d) {

    MatrixXd jd(3, 3);
    jd(0, 0) = ja(0, 0) * d(0, 0) + ja(1, 0) * d(1, 0) + ja(2, 0) * d(2, 0);
    jd(0, 1) = ja(0, 0) * d(0, 1) + ja(1, 0) * d(1, 1) + ja(2, 0) * d(2, 1);
    jd(0, 2) = ja(0, 0) * d(0, 2) + ja(1, 0) * d(1, 2) + ja(2, 0) * d(2, 2);
    jd(1, 0) = ja(0, 1) * d(0, 0) + ja(1, 1) * d(1, 0) + ja(2, 1) * d(2, 0);
    jd(1, 1) = ja(0, 1) * d(0, 1) + ja(1, 1) * d(1, 1) + ja(2, 1) * d(2, 1);
    jd(1, 2) = ja(0, 1) * d(0, 2) + ja(1, 1) * d(1, 2) + ja(2, 1) * d(2, 2);
    jd(2, 0) = 0;
    jd(2, 1) = 0;
    jd(2, 2) = ja(0, 2) * d(0, 2) + ja(1, 2) * d(1, 2) + ja(2, 2) * d(2, 2);

    MatrixXd result = jd.inverse();

    return result;
};


// 形状関数の勾配 [ dNi/dx dNi/dy ] を返す
// p - 要素節点
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
// t - 要素厚さ
MatrixXd ShellElement::grad(vector<FENode> p, MatrixXd ja, MatrixXd sf, MatrixXd d, double t) {
    
    int count = nodeCount();
    MatrixXd result(count, 3);

    MatrixXd ji = jacobInv(ja, d);

    for (int i = 0; i < count; i++) {
        double dndxsi = sf(i, 1);
        double dndeta = sf(i, 2);
        result(i, 0) = ji(0, 0) * dndxsi + ji(1, 0) * dndeta;
        result(i, 1) = ji(0, 1) * dndxsi + ji(1, 1) * dndeta;
        result(i, 2) = ji(0, 2) * dndxsi + ji(1, 2) * dndeta;
    }

    return result;
}


// 歪 - 変位マトリックスの転置行列を返す
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
MatrixXd ShellElement::strainMatrix1(MatrixXd ja, MatrixXd sf, MatrixXd d) {

    int count = nodeCount();
    MatrixXd result(count, 4);

    MatrixXd ji = jacobInv(ja, d);

    for (int i = 0; i < count; i++) {
        for (int j = 0; j < 3; j++) {
            result(i, j) = ji(0, j) * sf(i, 1) + ji(1, j) * sf(i, 2);
        }
        result(i, 3) = ji(1, 1) * sf(i, 0);
    }

    return result;
}


// 歪 - 変位マトリックスの転置行列を返す
// ただし歪は要素面座標、変位は全体座標
// ja - ヤコビ行列
// sf - 形状関数行列
// d - 方向余弦マトリックス
// zeta - 節点のζ座標
// t - 要素厚さ
MatrixXd ShellElement::strainMatrix(MatrixXd ja, MatrixXd sf, MatrixXd d, double zeta, double t) {
    
    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(6 * count, 5);

    MatrixXd b = strainMatrix1(ja, sf, d);
    double z = 0.5 * t * zeta;
    MatrixXd m1 = MatrixXd::Zero(5, 6);

    for (int i = 0; i < count; i++) {
        m1(0, 0) = b(i, 0);
        m1(0, 4) = z * b(i, 0);
        m1(1, 1) = b(i, 1);
        m1(1, 3) = -z * b(i, 1);
        m1(2, 0) = b(i, 1);
        m1(2, 1) = b(i, 0);
        m1(2, 3) = -z * b(i, 0);
        m1(2, 4) = z * b(i, 1);
        m1(3, 1) = b(i, 2);
        m1(3, 2) = b(i, 1);
        m1(3, 3) = -0.5 * t * b(i, 3) - z * b(i, 2);
        m1(4, 0) = b(i, 2);
        m1(4, 2) = b(i, 0);
        m1(4, 4) = 0.5 * t * b(i, 3) + z * b(i, 2);
        int ib = 6 * i;
        for (int i1 = 0; i1 < 5; i1++) {
            for (int j1 = 0; j1 < 3; j1++) {
                double s1 = 0;
                double s2 = 0;
                for (int k1 = 0; k1 < 3; k1++) {
                    s1 += m1(i1, k1) * d(j1, k1);
                    s2 += m1(i1, k1 + 3) * d(j1, k1);
                }
                result(ib + j1, i1) += s1;
                result(ib + 3 + j1, i1) += s2;
            }
        }
    }

    return result;
}

// 積分点の形状関数マトリックス [ NiNj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
// t - 要素厚さ
MatrixXd ShellElement::shapePart(vector<FENode> p, VectorXd x, double w, double t) {

    int count = nodeCount();
    MatrixXd result(count, count);

    MatrixXd sf = shapeFunction(x(0), x(1));
    Vector3Dim n = normalVector(p);
    MatrixXd ja = jacobianMatrix(p, sf, n, t);
    double det = ja.determinant();
    double coef = 2 * w * abs(det);

    for (int i = 0; i < count; i++) {
        vector<double> matr;
        double cf2 = coef * sf(i, 0);
        for (int j = 0; j < count; j++) {
            result(i,j) = cf2 * sf(j, 0);
        }
    }

    return result;
}


// 積分点の拡散マトリックス [ ∇Ni・∇Nj ] を返す
// p - 要素節点
// x - ξ,η,ζ座標
// w - 重み係数
// t - 要素厚さ
MatrixXd ShellElement::gradPart(vector<FENode> p, VectorXd x, double w, double t) {

    int count = nodeCount();
    MatrixXd result(count, count);

    MatrixXd sf = shapeFunction(x(0), x(1));
    Vector3Dim n = normalVector(p);
    MatrixXd ja = jacobianMatrix(p, sf, n, t);
    MatrixXd d = dirMatrix(p);
    MatrixXd gr = grad(p, ja, sf, d, t);
    double det = ja.determinant();
    double coef = 2 * w * abs(det);

    for (int i = 0; i < count; i++) {
        vector<double> matr;
        double c1 = coef * gr(i, 0);
        double c2 = coef * gr(i, 1);
        double c3 = coef * gr(i, 2);
        for (int j = 0; j < count; j++) {
            result(i, j) = c1 * gr(j, 0) + c2 * gr(j, 1) + c3 * gr(j, 2);
        }
    }

    return result;
}


// 形状関数マトリックス [ ∫NiNjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
MatrixXd ShellElement::shapeFunctionMatrix(vector<FENode> p, double coef, ShellParameter sp) {

    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(count, count);
    double t = sp.thickness;
    for (int i = 0; i < intP.size(); i++) {
        MatrixXd spr = shapePart(p, intP.row(i), coef * intP(i, 2), t);
        result += spr;
    }

    return result;
}

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
// sp - シェルパラメータ
MatrixXd ShellElement::gradMatrix(vector<FENode> p, double coef, ShellParameter sp) {
    
    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(count, count);
    double t = sp.thickness;
    for (int  i = 0; i < intP.size(); i++) {

        MatrixXd gpr = gradPart(p, intP.row(i), coef * intP(i, 2), t);
        result += gpr;
    }

    return result;
}


// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
MatrixXd ShellElement::geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp) {
    
    int count = nodeCount();
    MatrixXd result = MatrixXd::Zero(6 * count, 6 * count);
    MatrixXd d = dirMatrix(p);
    Vector3Dim n = normalVector(p);
    VectorXd v = toArray(u, 6);
    double t = sp.thickness;

    for (int i = 0; i < intP.size(); i++) {
        
        VectorXd sf = shapeFunction(intP(i, 0), intP(i, 1));
        MatrixXd ja = jacobianMatrix(p, sf, n, t);
        MatrixXd gr = grad(p, ja, sf, d, t);
        MatrixXd sm = strainMatrix(ja, sf, d, 0, t);
        VectorXd vm = v * sm;
        VectorXd mv = d1 * vm;
        Stress str = toStress(mv);
        double det = ja.determinant();
        double w = 2 * intP(i, 2) * abs(det);

        for (int i1 = 0; i1 < count; i1++) {

            int i6 = 6 * i1;

            for (int j1 = 0; j1 < count; j1++) {
                
                int j6 = 6 * j1;

                double s = w * (gr(i1, 0) * (str.xx * gr(j1, 0) + str.xy * gr(j1, 1) + str.zx * gr(j1, 2)) +
                    gr(i1, 1) * (str.xy * gr(j1, 0) + str.yy * gr(j1, 1) + str.yz * gr(j1, 2)) +
                    gr(i1, 2) * (str.zx * gr(j1, 0) + str.yz * gr(j1, 1) + str.zz * gr(j1, 2)));

                result(i6, j6) += s;
                result(i6 + 1, j6 + 1) += s;
                result(i6 + 2, j6 + 2) += s;
            }
        }
    }
    toDir3(d, result);

    return result;
}


// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>> 
    ShellElement::strainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp) {

    int count = nodeCount();
    MatrixXd d = dirMatrix(p);
    Vector3Dim n = normalVector(p);
    VectorXd v = toArray(u, 6);
    double t = sp.thickness;

    vector<Strain> strain1;
    vector<Stress> stress1;
    vector<double> energy1;
    vector<Strain> strain2;
    vector<Stress> stress2;
    vector<double> energy2;

    for (int i = 0; i < count; i++) {

        VectorXd eps1 = strainPart(p, v, n, d, nodeP(i, 0), nodeP(i, 1), 1, t);
        VectorXd eps2 = strainPart(p, v, n, d, nodeP(i, 0), nodeP(i, 1), -1, t);

        Strain trai1 = toStrain(eps1);
        Stress sres1 = toStress(d1 * eps1);
        Strain trai2 = toStrain(eps2);
        Stress sres2 = toStress(d1 * eps2);
        trai1.rotate(d);
        sres1.rotate(d);
        trai2.rotate(d);
        sres2.rotate(d);
        double ergy1 = 0.5 * trai1.innerProduct(sres1);
        double ergy2 = 0.5 * trai2.innerProduct(sres2);

        strain1.push_back(trai1);
        stress1.push_back(sres1);
        strain2.push_back(trai2);
        stress2.push_back(sres2);
        energy1.push_back(ergy1);
        energy2.push_back(ergy2);
    }

    tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
        result = make_tuple(strain1, stress1, energy1, strain2, stress2, energy2);

    return result;
}


// 要素内の歪ベクトルを返す
// p - 要素節点
// v - 節点変位ベクトル
// n - 法線ベクトル
// d - 方向余弦マトリックス
// xsi,eta,zeta - ξ,η,ζ座標
// t - 要素厚さ
VectorXd ShellElement::strainPart(vector<FENode> p, VectorXd v, Vector3Dim n, MatrixXd d, double  xsi, double eta, double zeta, double t) {

    MatrixXd sf = shapeFunction(xsi, eta);
    MatrixXd ja = jacobianMatrix(p, sf, n, t);
    MatrixXd sm = strainMatrix(ja, sf, d, zeta, t);
    VectorXd result =  v * sm;
    return result;
};


// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// d1 - 応力 - 歪マトリックス
// sp - シェルパラメータ
tuple<Strain, Stress, double, Strain, Stress, double>
    ShellElement::elementStrainStress(vector<FENode> p, vector<Vector3R> u, MatrixXd d1, ShellParameter sp) {

    MatrixXd d = dirMatrix(p);
    Vector3Dim n = normalVector(p);
    VectorXd v = toArray(u, 6);
    double t = sp.thickness;
    int count = (int)intP.size();
    auto cf = double(1 / count);

    VectorXd strain1 = VectorXd::Zero(6);
    VectorXd stress1 = VectorXd::Zero(6);
    double energy1 = 0;
    VectorXd strain2 = VectorXd::Zero(6);
    VectorXd stress2 = VectorXd::Zero(6);
    double energy2 = 0;

    for (int i = 0; i < intP.size(); i++) {
        
        VectorXd eps1 = strainPart(p, v, n, d, intP(i, 0), intP(i, 1), 1, t);
        VectorXd eps2 = strainPart(p, v, n, d, intP(i, 0), intP(i, 1), -1, t);

        strain1 += eps1;
        strain2 += eps2;

        VectorXd str1 = d1 * eps1;
        VectorXd str2 = d1 * eps2;

        stress1 += str1;
        stress2 += str2;

        energy1 += eps1.dot(str1);
        energy2 += eps2.dot(str2);
    }
    strain1 *= cf;
    stress1 *= cf;
    energy1 *= 0.5 * cf;
    strain1 *= cf;
    stress1 *= cf;
    energy2 *= 0.5 * cf;

    Strain trai1 = toStrain(strain1);
    Stress sres1 = toStress(stress1);
    Strain trai2 = toStrain(strain2);
    Stress sres2 = toStress(stress2);

    tuple<Strain, Stress, double, Strain, Stress, double>
        result = make_tuple(trai1, sres1, energy1, trai2, sres2, energy2);

    return result;
};


// ベクトルを歪に変換する
// s - 歪ベクトル
Strain ShellElement::toStrain(VectorXd s) {
    VectorXd ss(6);
    ss(0) = s(0);
    ss(1) = s(1);
    ss(2) = 0;
    ss(3) = s(2);
    ss(4) = s(3);
    ss(5) = s(4);
    return Strain(ss);
};

// ベクトルを歪に変換する
// s - 歪ベクトル
Stress ShellElement::toStress(VectorXd s) {
    VectorXd ss(6);
    ss(0) = s(0);
    ss(1) = s(1);
    ss(2) = 0;
    ss(3) = s(2);
    ss(4) = s(3);
    ss(5) = s(4);
    return Stress(ss);
};

// 要素を表す文字列を返す
// materials - 材料
// params - シェルパラメータ
// p - 節点
string ShellElement::toString(vector<Material> materials, vector<ShellParameter> params, vector<FENode> p) {

    //string s = format("{}\t{}\t{}\t{}\t{}",
    //    getName(), label, materials[material].label, params[param].label);

    //for (int i = 0; i < nodes.size(); i++) {
    //    s += '\t' + p[nodes[i]].label;
    //}
    //return s;

    return "ShellElement";
}
