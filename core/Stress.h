#pragma once
#include "SymmetricTensor3.h"
class Stress :
    public SymmetricTensor3
{

private:

public:
    Stress(double* s);
    double mises();

};

