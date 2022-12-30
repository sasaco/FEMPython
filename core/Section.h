#pragma once

#include"Material.h"

#include<Eigen/Core>
using namespace Eigen;

class Section {

private:


public:
    // ’f–ÊÏ
    double area;

    // ’f–Ê‚QŸƒ‚[ƒƒ“ƒg
    double iy;
    double iz;

    // ’f–Ê‚QŸ‹Éƒ‚[ƒƒ“ƒg
    double ip;

    // ˜cE‰—ÍƒxƒNƒgƒ‹‚ğ•Ô‚·
    MatrixXd strainStress(Material material, double ex, double thd, double kpy, double kpz,
        VectorXd sy, VectorXd sz);
};

