#pragma once
#include <Eigen/Core> 

using namespace Eigen;

class SymmetricTensor3 {

public:
    double xx;
    double yy;
    double zz;
    double xy;
    double yz;
    double zx;

    SymmetricTensor3(VectorXd s);

    VectorXd vector();
    void add(SymmetricTensor3 t);
    void mul(double a);
    double innerProduct(SymmetricTensor3 t);
    void rotate(Matrix3d d);

};

