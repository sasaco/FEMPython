#pragma once

#include <vector>
using namespace std;
using std::vector;

#include <Eigen/Core>
using namespace Eigen;


class Solver {


private:
    const double PRECISION = 1e-10;	// マトリックス精度
    const double  LU_METHOD = 0;	// LU分解法
    const double  ILUCG_METHOD = 1; // 不完全LU分解共役勾配法

    Vector3d matrix;
    Vector3d matrix2;
    Vector3d vector;
    int dof;
    int method;

public:
    Solver();
    void clear();
};