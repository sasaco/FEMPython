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
    if (axis != nullptr)
        axis.normalize();
};

// 剛性マトリックスを返す
// p - 要素節点
// material - 材料
// sect - 梁断面パラメータ
vector<vector<double>> BarElement::stiffnessMatrix(vector<FENode> p, Material material, Section sect) {
    
    vector<vector<double>> kk = numeric::rep(12, 12);
    
    double l = p[0].distanceTo(p[1]);

    vector<double> d = dirMatrix(p, axis);

    double kx = material.ee * sect.area / l;
    double kt = material.gg * sect.ip / l;

    kk[0][0] = kx;
    kk[0][6] = -kx;
    kk[6][0] = -kx;
    kk[6][6] = kx;
    kk[3][3] = kt;
    kk[3][9] = -kt;
    kk[9][3] = -kt;
    kk[9][9] = kt;

    vector<vector<double>> km = stiffBend(l, material, sect);

    for (int i = 0; i < 4; i++) {
        vector<double> kk1 = kk[I_YMZ[i]];
        vector<double> kk2 = kk[I_ZMY[i]];
        vector<double> kmi1 = km[i];
        vector<double> kmi2 = km[i + 4];

        for (int j = 0; j < 4; j++) {
            kk1[I_YMZ[j]] = kmi1[j];
            kk2[I_ZMY[j]] = kmi2[j];
        }
    }

    toDir3(d, kk);
    return kk;
};

// 拡散マトリックス [ ∫∇Ni・∇NjdV ] を返す
// p - 要素節点
// coef - 係数
// sect - 梁断面パラメータ
vector<vector<double>> BarElement::gradMatrix(vector<FENode> p, double coef, Section sect) {
    double c = coef * sect.area / p[0].distanceTo(p[1]);
    vector<vector<double>> resulr = {
        {c, -c}, {-c, c}
    };

    return result;
};

// 幾何剛性マトリックスを返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
vector<vector<double>> BarElement::geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, Material material, Section sect) {
    
    double l2 = p[0].distanceToSquared(p[1]);
    
    vector<vector<double>> d = dirMatrix(p, axis);

    vector<vector<double>> v = toLocalArray(u, d);
    
    vector<vector<double>> kk = numeric::rep(12, 12);

    double sx = material.ee * sect.area * (v[6] - v[0]) / l2;

    for (int i = 0; i < 3; i++) {
        kk[i][i] = sx;
        kk[i + 6][i] = -sx;
        kk[i][i + 6] = -sx;
        kk[i + 6][i + 6] = sx;
    }
    toDir3(d, kk);
    return kk;
};

// 節点歪・応力を返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
vector<vector<double>> BarElement::strainStress(vector<FENode> p, vector<BoundaryCondition> u, Material material, Section sect) {

    double l = p[0].distanceTo(p[1]);
    
    vector<vector<double>> d = dirMatrix(p, axis);

    vector<vector<double>> v = toLocalArray(u, d);

    vector<double> strain1;
    vector<double> stress1;
    vector<double> energy1;
    vector<double> strain2;
    vector<double> stress2;
    vector<double> energy2;

    double ex = (v[6] - v[0]) / l;
    double thd = (v[9] - v[3]) / l;

    vector<double> ks = this.bendCurveShare(v, l, material, sect);

    vector<double> kpy = ks[0];
    vector<double> kpz = ks[1];
    vector<double> sy = ks[2];
    vector<double> sz = ks[3];

    for (int i = 0; i < 2; i++) {

        vector<double> str = sect.strainStress(material, ex, thd, kpy[i], kpz[i], sy, sz);
        strain1[i] = Strain(str[0]);
        stress1[i] = Stress(str[1]);
        strain2[i] = Strain(str[2]);
        stress2[i] = Stress(str[3]);
        strain1[i].rotate(d);
        stress1[i].rotate(d);
        strain2[i].rotate(d);
        stress2[i].rotate(d);
        energy1[i] = 0.5 * strain1[i].innerProduct(stress1[i]);
        energy2[i] = 0.5 * strain2[i].innerProduct(stress2[i]);
    }

    vector<vector<double>> result = {
        strain1, stress1, energy1, strain2, stress2, energy2
    };
    return result;
};


// 要素歪・応力を返す
// p - 要素節点
// u - 節点変位
// material - 材料
// sect - 梁断面パラメータ
vector<vector<double>> BarElement::elementStrainStress(vector<FENode> p, vector<BoundaryCondition> u, Material material, Section sect) {

    double l = p[0].distanceTo(p[1]);
    
    vector<vector<double>> d = dirMatrix(p, axis);

    vector<double> v = toLocalArray(u, d);

    vector<vector<double>> str;

    double ex = (v[6] - v[0]) / l;
    double thd = (v[9] - v[3]) / l;

    vector<vector<double>> ks = bendCurveShare(v, l, material, sect);

    vector<double> kpy = ks[0];
    vector<double> kpz = ks[1];
    vector<double> sy = ks[2];
    vector<double> sz = ks[3];

    for (int i = 0; i < 2; i++) {
        str[i] = sect.strainStress(material, ex, thd, kpy[i], kpz[i], sy, sz);
    }

    Strain strain1 = Strain(numeric::mul(0.5, numeric::add(str[0][0], str[1][0])));
    Stress stress1 = Stress(numeric::mul(0.5, numeric::add(str[0][1], str[1][1])));
    Strain strain2 = Strain(numeric::mul(0.5, numeric::add(str[0][2], str[1][2])));
    Stress stress2 = Stress(numeric::mul(0.5, numeric::add(str[0][3], str[1][3])));
    strain1.rotate(d);
    stress1.rotate(d);
    strain2.rotate(d);
    stress2.rotate(d);
    double energy1 = 0.5 * strain1.innerProduct(stress1);
    double energy2 = 0.5 * strain2.innerProduct(stress2);

    vector<vector<double>> result = {
        strain1, stress1, energy1, strain2, stress2, energy2
    };

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
};
