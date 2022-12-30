#pragma once
#include "SymmetricTensor3.h"
class Stress :
    public SymmetricTensor3
{

private:

public:
    Stress();
    Stress(VectorXd s);
    double mises();

};

