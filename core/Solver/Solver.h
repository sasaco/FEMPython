#pragma once

#include"FemDataModel.h"
#include"FENode.h"

#include <vector>
using namespace std;
using std::vector;

//#include<cblas.h>

#include <Eigen/Core>
#include <Eigen/LU>

// Sparse�w�b�_�̓R���p�C�������낵���x���Ȃ�I�I�I
#include <Eigen/Sparse>
// ����ɂ�������
//#include <Eigen/SparseCore>
//#include <Eigen/SparseLU>
//#include <Eigen/SparseQR>

using namespace Eigen;
typedef Eigen::Triplet<double> T;

class Solver : public FemDataModel {

private:
    double PRECISION = 1e-10;	// �}�g���b�N�X���x

    MatrixXd _matrix;
    MatrixXd _matrix2;
    VectorXd _vector;

    SparseMatrix<double> matrix_;
    SparseMatrix<double> vector_;


    int  LU_METHOD = 0;	        // LU����@
    int  ILUCG_METHOD = 1;      // �s���SLU�����������z�@

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

