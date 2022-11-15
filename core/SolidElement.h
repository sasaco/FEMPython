#pragma once
#include "FElement.h";
#include "FENode.h";
#include "Material.h";
#include "numeric.h";


#include <string>
#include <vector>
using namespace std;



class SolidElement : public FElement {

private:

    // 四面体2次要素の節点のξ,η,ζ座標
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

    // 四面体2次要素の積分点のξ,η,ζ座標,重み係数
    const double TETRA2_INT[4][4] = {
        {GTETRA2[0], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[1], GTETRA2[0], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[1], GTETRA2[0], C1_24},
        {GTETRA2[0], GTETRA2[0], GTETRA2[1], C1_24}
    };

    // 楔形1次要素の節点のξ,η,ζ座標
    const double WEDGE1_NODE[6][3] = {
        {0, 0, -1},
        {1, 0, -1},
        {0, 1, -1},
        {0, 0, 1},
        {1, 0, 1},
        {0, 1, 1}
    };

    // 楔形1次要素の積分点のξ,η,ζ座標,重み係数
    const double WEDGE1_INT[2][4] = {
        {C1_3, C1_3, GX2[0], 0.5},
        {C1_3, C1_3, GX2[1], 0.5}
    };

    // 楔形2次要素の節点のξ,η,ζ座標
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

    // 楔形2次要素の積分点のξ,η,ζ座標,重み係数
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

    // 六面体2次要素の節点のξ,η,ζ座標
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

    // 六面体2次要素の積分点のξ,η,ζ座標,重み係数
    double HEXA2_INT[9][4]; // コンストラクタで初期化


    vector<vector<double>> nodeP;
    vector<vector<double>> intP;

public:

    SolidElement(int label, int material, vector<int> nodes, vector<vector<double>> _nodeP, vector<vector<double>> _intP);

    void jacobianMatrix(vector<FENode> p, vector<vector<double>> sf, double out[9]);

    void grad(vector<FENode> p, double ja[9], vector<vector<double>> sf, vector<vector<double>> out);

    void strainMatrix(vector<vector<double>> grad, vector<vector<double>> out);

    virtual void shapeFunction(double xsi, double eta, double zeta, vector<vector<double>> out) = 0;

    double determinant(double ja[9]);

    void shapePart(vector<FENode> p, double x[3], double w, vector<vector<double>> out);

    void gradPart(vector<FENode> p, double x[3], double w, vector<vector<double>> out);

    virtual void massMatrix(vector<FENode> p, double dens, vector<vector<double>> out);

    void addMatrix(vector<vector<double>> a, vector<vector<double>> da);

    virtual void stiffnessMatrix(vector<FENode> p, vector<vector<double>> d1, vector<vector<double>> out);

    void shapeFunctionMatrix(vector<FENode> p, double coef, vector<vector<double>> out);

    void gradMatrix(vector<FENode> p, double coef, vector<vector<double>> out);

    void geomStiffnessMatrix(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1, vector<vector<double>> out);

    void strainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
        vector<Strain> strain, vector<Stress> stress, vector<double> energy);

    void strainPart(vector<FENode> p, vector<double> v, double x[3], vector<double> out);

    void elementStrainStress(vector<FENode> p, vector<BoundaryCondition> u, vector<vector<double>> d1,
        Strain _Strain, Stress _Stress, double energy);

    virtual string getName() = 0;

    string toString(vector<Material> materials, vector<FENode> p);
};

