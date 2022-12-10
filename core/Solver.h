#pragma once

#include"FemDataModel.h"

#include <vector>
using namespace std;
using std::vector;

#include <Eigen/Core>
#include <Eigen/LU>
using namespace Eigen;


class Solver {

private:
    double PRECISION = 1e-10;	// �}�g���b�N�X���x

    FemDataModel model;

    MatrixXd _matrix;
    MatrixXd _matrix2;
    VectorXd _vector;

public:
    int  LU_METHOD = 0;	        // LU����@
    int  ILUCG_METHOD = 1;      // �s���SLU�����������z�@

    int dof;
    int method;

    Solver();
    void clear();

    MatrixXd heatMatrix();
    MatrixXd tempVector(MatrixXd matrix);
    void createHeatMatrix(const FemDataModel& model);

    MatrixXd stiffnessMatrix(int dof);
    void createStiffnessMatrix(const FemDataModel& model);


    void extruct(MatrixXd matrix1, VectorXd vector1, vector<int> list);

    MatrixXd solve();   
};