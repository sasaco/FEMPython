#pragma once
#include "SymmetricTensor3.h"
class Strain : public SymmetricTensor3 {

private:
    // SymmetricTensor3 �N���X
    //double xy;
    //double yz;
    //double zx;


public:
    Strain();
    Strain(VectorXd s);
    VectorXd vector();
};

