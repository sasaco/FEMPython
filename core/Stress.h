#pragma once
#include "SymmetricTensor3.h"
class Stress :
    public SymmetricTensor3
{

private:

public:
    Stress(VectorXd s);
    double mises();

};

