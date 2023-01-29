#pragma once
#include "SymmetricTensor3.h"
class Strain : public SymmetricTensor3 {

private:
    double xy;
    double yz;
    double zx;


public:
    Strain();
    Strain(VectorXd s);
    VectorXd vector();
};

