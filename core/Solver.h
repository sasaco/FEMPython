#pragma once

#include"FemDataModel.h"

#include <vector>
using namespace std;
using std::vector;

#include <Eigen/Core>
using namespace Eigen;


class Solver {

private:
    double PRECISION = 1e-10;	// マトリックス精度

    MatrixXd _matrix;
    MatrixXd _matrix2;
    VectorXd _vector;

public:
    int  LU_METHOD = 0;	        // LU分解法
    int  ILUCG_METHOD = 1;      // 不完全LU分解共役勾配法

    int dof;
    int method;

    Solver();
    void clear();

    MatrixXd heatMatrix(const FemDataModel& model);
    MatrixXd tempVector(const FemDataModel& model, MatrixXd matrix);
    void extruct(MatrixXd matrix1, VectorXd vector1, vector<int> list);

    void createHeatMatrix(FemDataModel& model);
};