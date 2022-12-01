#pragma once

#include <vector>
using namespace std;
using std::vector;

#include <Eigen/Core>
using namespace Eigen;


class Solver {

private:
    double PRECISION = 1e-10;	// �}�g���b�N�X���x

    Vector3d matrix;
    Vector3d matrix2;
    Vector3d vector;
    int dof;

public:
    int  LU_METHOD = 0;	        // LU����@
    int  ILUCG_METHOD = 1;      // �s���SLU�����������z�@

    int method;

    Solver();
    void clear();
};