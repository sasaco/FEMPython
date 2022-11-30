#include "Section.h"

MatrixXd Section::strainStress(Material material, double ex, double thd, double kpy, double kpz,
    VectorXd sy, VectorXd sz) {

    MatrixXd a = MatrixXd::Zero(3, 3);
    return a;
}