#include "BarElement.h"

//--------------------------------------------------------------------//
// 梁要素
// label - 要素ラベル
// material - 材料のインデックス
// param - 梁パラメータのインデックス
// nodes - 節点番号
// axis - 断面基準方向ベクトル
BarElement::BarElement(int label, int material, int _param, vector<int> nodes, Vector3 _axis) :
    FElement(label, material, nodes) {
    param = _param;
    isBar = true;
    axis = _axis;
    axis.normalize();
};

// 剛性マトリックスを返す
// p - 要素節点
// material - 材料
// sect - 梁断面パラメータ
MatrixXd BarElement::stiffnessMatrix(vector<FENode> p, Material material, Section sect) {
    
    MatrixXd kk = MatrixXd::Zero(12, 12);
    double l = p[0].distanceTo(p[1]);
    Matrix3d d = dirMatrix(p, axis);

    double kx = material.ee * sect.area / l;
    double kt = material.gg * sect.ip / l;

    kk(0, 0) = kx;
    kk(0, 6) = -kx;
    kk(6, 0) = -kx;
    kk(6, 6) = kx;
    kk(3, 3) = kt;
    kk(3, 9) = -kt;
    kk(9, 3) = -kt;
    kk(9, 9) = kt;

    MatrixXd km = stiffBend(l, material, sect);

    for (int i = 0; i < 4; i++) {
        VectorXd kk1 = kk.row(I_YMZ[i]);
        VectorXd kk2 = kk.row(I_ZMY[i]);
        VectorXd kmi1 = km.row(i);
        VectorXd kmi2 = km.row(i + 4);

        for (int j = 0; j < 4; j++) {
            kk1(I_YMZ[j]) = kmi1(j);
            kk2(I_ZMY[j]) = kmi2(j);
        }
    }

    toDir3(d, kk);
    return kk;
}


// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
// sect - 梁断面パラメータ
MatrixXd BarElement::gradMatrix(vector<FENode> p, double coef, Section sect) {

    double c = coef * sect.area / p[0].distanceTo(p[1]);

    MatrixXd result(2, 2);
    result(0, 0) = c; 
    result(0, 1) = -c;

    result(1, 0) = -c;
    result(1, 1) = c;

    return result;
}


// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
MatrixXd BarElement::geomStiffnessMatrix(vector<FENode> p, vector<Vector3R> u, Material material, Section sect) {
    
    double l2 = p[0].distanceToSquared(p[1]);
    Matrix3d d = dirMatrix(p, axis);
    VectorXd v = toLocalArray(u, d);
    MatrixXd kk = MatrixXd::Zero(12, 12);
    double sx = material.ee * sect.area * (v[6] - v[0]) / l2;
    for (int i = 0; i < 3; i++) {
        kk(i, i) = sx;
        kk(i + 6, i) = -sx;
        kk(i, i + 6) = -sx;
        kk(i + 6, i + 6) = sx;
    }
    toDir3(d, kk);
    return kk;
}


// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
tuple<vector<Strain>, vector<Stress>, vector<double>, vector<Strain>, vector<Stress>, vector<double>>
    BarElement::strainStress(vector<FENode> p, vector<Vector3R> u, Material material, Section sect) {

    double l = p[0].distanceTo(p[1]);
    Matrix3d d = dirMatrix(p, axis);
    VectorXd v = toLocalArray(u, d);

    vector<Strain> strain1;
    vector<Stress> stress1;
    vector<double> energy1;
    vector<Strain> strain2;
    vector<Stress> stress2;
    vector<double> energy2;

    double ex = (v[6] - v[0]) / l;
    double thd = (v[9] - v[3]) / l;

    MatrixXd ks = bendCurveShare(v, l, material, sect);

    VectorXd kpy = ks.row(0);
    VectorXd kpz = ks.row(1);
    VectorXd sy = ks.row(2);
    VectorXd sz = ks.row(3);

    for (int i = 0; i < 2; i++) {

        MatrixXd str = sect.strainStress(material, ex, thd, kpy[i], kpz[i], sy, sz);

        Strain trai1 = Strain(str.row(0));
        Stress sres1 = Stress(str.row(1));
        Strain trai2 = Strain(str.row(2));
        Stress sres2 = Stress(str.row(3));
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
};


// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
tuple<Strain, Stress, double, Strain, Stress, double>
    BarElement::elementStrainStress(vector<FENode> p, vector<Vector3R> u, Material material, Section sect) {

    double l = p[0].distanceTo(p[1]);
    Matrix3d d = dirMatrix(p, axis);
    VectorXd v = toLocalArray(u, d);
    double ex = (v[6] - v[0]) / l;
    double thd = (v[9] - v[3]) / l;

    MatrixXd ks = bendCurveShare(v, l, material, sect);

    VectorXd kpy = ks.row(0);
    VectorXd kpz = ks.row(1);
    VectorXd sy = ks.row(2);
    VectorXd sz = ks.row(3);

    vector<MatrixXd> str;
    for (int i = 0; i < 2; i++) {
        str.push_back(sect.strainStress(material, ex, thd, kpy[i], kpz[i], sy, sz));
    }

    VectorXd st0 = str[0].row(0) + str[1].row(0);
    VectorXd st1 = str[0].row(1) + str[1].row(1);
    VectorXd st2 = str[0].row(2) + str[1].row(2);
    VectorXd st3 = str[0].row(3) + str[1].row(3);
    Strain strain1 = Strain(0.5 * st0);
    Stress stress1 = Stress(0.5 * st1);
    Strain strain2 = Strain(0.5 * st2);
    Stress stress2 = Stress(0.5 * st3);
    strain1.rotate(d);
    stress1.rotate(d);
    strain2.rotate(d);
    stress2.rotate(d);
    double energy1 = 0.5 * strain1.innerProduct(stress1);
    double energy2 = 0.5 * strain2.innerProduct(stress2);

    tuple<Strain, Stress, double, Strain, Stress, double>
        result = make_tuple(strain1, stress1, energy1, strain2, stress2, energy2);

    return result;
};


// 要素を表す文字列を返す
// materials - 材料
// params - 梁パラメータ
// p - 節点
string BarElement::toString(vector<Material> materials, vector<BarParameter> params, vector<FENode> p) {

    Material mat = materials[material];
    BarParameter par = params[param];

    string s = format("{}\t{}\t{}\t{}",
        getName(), label, mat.label, par.label);

    for (int i = 0; i < nodes.size(); i++) {
        FENode n = p[nodes[i]];
        s += '\t' + n.label;
    }

    return s;
}
