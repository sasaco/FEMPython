#include "HexaElement1.h";

//--------------------------------------------------------------------//
// 六面体1次要素
// label - 要素ラベル
// material - 材料のインデックス
// nodes - 節点番号
HexaElement1::HexaElement1(int label, int material, vector<int> nodes) : 
    SolidElement(label, material, nodes, HEXA1_NODE, HEXA1_INT) {

    // 六面体1次要素の質量マトリックス係数
    for (int i = 0; i < 8; i++) {
        for (int j = 0; j < 8; j++) {
            double s = abs(HEXA1_NODE[i][0] - HEXA1_NODE[j][0]) +
                abs(HEXA1_NODE[i][1] - HEXA1_NODE[j][1]) +
                abs(HEXA1_NODE[i][2] - HEXA1_NODE[j][2]);
            HEXA1_MASS_BASE[i][j] = pow(0.5, 0.5 * s) / 27;
        }
    }
};


// 要素名称を返す
string HexaElement1::getName() {
    return "HexaElement1";
} 


// 形状関数行列 [ Ni dNi/dξ dNi/dη dNi/dζ ] を返す
// xsi,eta,zeta - 要素内部ξ,η,ζ座標
void HexaElement1::shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) {

    out = {
        {0.125 * (1 - xsi) * (1 - eta) * (1 - zeta), -0.125 * (1 - eta) * (1 - zeta),
        -0.125 * (1 - xsi) * (1 - zeta), -0.125 * (1 - xsi) * (1 - eta)} ,
        {0.125 * (1 + xsi) * (1 - eta) * (1 - zeta), 0.125 * (1 - eta) * (1 - zeta),
        -0.125 * (1 + xsi) * (1 - zeta), -0.125 * (1 + xsi) * (1 - eta)},
        {0.125 * (1 + xsi) * (1 + eta) * (1 - zeta), 0.125 * (1 + eta) * (1 - zeta),
        0.125 * (1 + xsi) * (1 - zeta), -0.125 * (1 + xsi) * (1 + eta)},
        {0.125 * (1 - xsi) * (1 + eta) * (1 - zeta), -0.125 * (1 + eta) * (1 - zeta),
        0.125 * (1 - xsi) * (1 - zeta), -0.125 * (1 - xsi) * (1 + eta)},
        {0.125 * (1 - xsi) * (1 - eta) * (1 + zeta), -0.125 * (1 - eta) * (1 + zeta),
        -0.125 * (1 - xsi) * (1 + zeta), 0.125 * (1 - xsi) * (1 - eta)},
        {0.125 * (1 + xsi) * (1 - eta) * (1 + zeta), 0.125 * (1 - eta) * (1 + zeta),
        -0.125 * (1 + xsi) * (1 + zeta), 0.125 * (1 + xsi) * (1 - eta)},
        {0.125 * (1 + xsi) * (1 + eta) * (1 + zeta), 0.125 * (1 + eta) * (1 + zeta),
        0.125 * (1 + xsi) * (1 + zeta), 0.125 * (1 + xsi) * (1 + eta)},
        {0.125 * (1 - xsi) * (1 + eta) * (1 + zeta), -0.125 * (1 + eta) * (1 + zeta),
        0.125 * (1 - xsi) * (1 + zeta), 0.125 * (1 - xsi) * (1 + eta)}
    };

}


// 質量マトリックスを返す
// p - 要素節点
// dens - 材料の密度
void HexaElement1::massMatrix(vector<FENode> p, double dens, vector<vector<double>> out) {

    double _ja = 0;

    for (int i = 0; i < 8; i++) {

        vector<vector<double>> sf;
        shapeFunction(HEXA1_INT[i][0], HEXA1_INT[i][1],
            HEXA1_INT[i][2], sf);

        double ja[9];
        jacobianMatrix(p, sf, ja);
        double det = determinant(ja);

        _ja += abs(det);
    }

    double coef = dens * _ja;
    
    for (int i = 0; i < 3 * 8; ++i) {
        vector<double> m;
        for (int j = 0; j < 3 * 8; ++j) {
            m.push_back(0);
        }
        out.push_back(m);
    }

    for (int i = 0; i < 8; i++) {
        int i3 = 3 * i;
        for (int j = 0; j < 8; j++) {
            double value = coef * HEXA1_MASS_BASE[i][j];
            int j3 = 3 * j;
            out[i3][j3] += value;
            out[i3 + 1][j3 + 1] += value;
            out[i3 + 2][j3 + 2] += value;
        }
    }
}
