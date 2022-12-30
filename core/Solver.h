#pragma once

#include"FemDataModel.h"
#include"FENode.h"

#include <vector>
using namespace std;
using std::vector;

#include<cblas.h>

#define EIGEN_USE_BLAS
#define EIGEN_USE_LAPACKE
#include <Eigen/Core>
#include <Eigen/LU>

// Sparseヘッダはコンパイルが恐ろしく遅くなる！！！
#include <Eigen/Sparse>
// 代わりにこうする
//#include <Eigen/SparseCore>
//#include <Eigen/SparseLU>
//#include <Eigen/SparseQR>

using namespace Eigen;
typedef Eigen::Triplet<double> T;

class Solver : public FemDataModel {

private:
    double PRECISION = 1e-10;	// マトリックス精度

    MatrixXd _matrix;
    MatrixXd _matrix2;
    VectorXd _vector;

    SparseMatrix<double> matrix_;
    SparseMatrix<double> vector_;


    int  LU_METHOD = 0;	        // LU分解法
    int  ILUCG_METHOD = 1;      // 不完全LU分解共役勾配法

    int dof;
    int method;

    MatrixXd heatMatrix();
    VectorXd tempVector(MatrixXd matrix);
    void createHeatMatrix();

    MatrixXd stiffnessMatrix(int dof);
    void createStiffnessMatrix();

    double setElementMatrix(ElementManager element, int dof, MatrixXd &matrix, MatrixXd km, double kmax);
    VectorXd loadVector(int dof);

    void extruct(MatrixXd matrix1, VectorXd vector1, vector<int> list);

    VectorXd solve();

public:
    Solver();
    void clear();
    void calculate();

};

