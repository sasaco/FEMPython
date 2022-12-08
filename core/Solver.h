#pragma once

#include"FemDataModel.h"

#include <vector>
using namespace std;
using std::vector;

#include <Eigen/Core>
using namespace Eigen;


class Solver {

private:
    double PRECISION = 1e-10;	// �}�g���b�N�X���x

    VectorXd matrix;
    VectorXd matrix2;
    VectorXd vector;

public:
    int  LU_METHOD = 0;	        // LU����@
    int  ILUCG_METHOD = 1;      // �s���SLU�����������z�@

    int dof;
    int method;

    Solver();
    void clear();

    MatrixXd heatMatrix(FemDataModel& model);
    void createHeatMatrix(FemDataModel& model);
};