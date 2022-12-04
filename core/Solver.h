#pragma once

#include <vector>
using namespace std;
using std::vector;

#include <Eigen/Core>
using namespace Eigen;


class Solver {

private:
    double PRECISION = 1e-10;	// マトリックス精度

    VectorXd matrix;
    VectorXd matrix2;
    VectorXd vector;
    int dof;

public:
    int  LU_METHOD = 0;	        // LU分解法
    int  ILUCG_METHOD = 1;      // 不完全LU分解共役勾配法

    int method;

    Solver();
    void clear();
};