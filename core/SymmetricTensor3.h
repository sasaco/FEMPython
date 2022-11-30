#pragma once
#include <Eigen/Core> 

using namespace Eigen;

class SymmetricTensor3 {

protected:
    double xx;
    double yy;
    double zz;
    double xy;
    double yz;
    double zx;

public:
    SymmetricTensor3(VectorXd s);

    VectorXd vector();
    void add(SymmetricTensor3 t);
    void mul(double a);

    double innerProduct(SymmetricTensor3 t);

};

