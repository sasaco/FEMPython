#include "RectSection.h"

//--------------------------------------------------------------------//
// 矩形断面
// ss - データ文字列
RectSection::RectSection() {}
RectSection::RectSection(vector<double> ss) {

    double b1 = ss[0];   // 外側幅
    double h1 = ss[1];   // 外側高さ
    double b2 = ss[2];   // 内側幅
    double h2 = ss[3];   // 内側高さ

    // 断面積
    area = b1 * h1 - b2 * h2;
    // 断面２次モーメント
    double i11 = b1 * b1 * b1 * h1;
    double i12 = b1 * h1 * h1 * h1;
    double i21 = b2 * b2 * b2 * h2;
    double i22 = b2 * h2 * h2 * h2;

    iy = (i11 - i21) / 12;
    iz = (i12 - i22) / 12;

    double ip1;
    double ip2;
    if (b1 >= h1) {
        auto k1 = rectCoef(b1 / h1);
        ip1 = k1[0] * i12;
        zy = k1[1] * h1;
        zz = k1[3] * zy;
    }
    else {
        auto k1 = rectCoef(h1 / b1);
        ip1 = k1[0] * i11;
        zz = k1[1] * b1;
        zy = k1[3] * zz;
    }
    if (i22 == 0) {
        ip2 = 0;
    }
    else if (b2 >= h2) {
        auto k2 = rectCoef(b2 / h2);
        ip2 = k2[0] * i22;
    }
    else {
        auto k2 = rectCoef(h2 / b2);
        ip2 = k2[0] * i21;
    }
    ip = ip1 - ip2;		// 断面２次極モーメント
};


// せん断補正係数を返す
double RectSection::shearCoef() {
    return KS_RECT;
};

// 歪・応力ベクトルを返す
// material - 材料
// ex - 引張圧縮歪
// thd - 比捩り角
// kpy,kpz - 曲げによる曲率
// sy,sz - 断面せん断歪
MatrixXd RectSection::strainStress(Material material, double ex, double thd, double kpy, double kpz,
    double sy, double sz) {

    double sby = 0.5 * kpy * b1;
    double sbz = 0.5 * kpz * h1;
    double gy = zy * thd;
    double gz = zz * thd;

    double ee = material.ee;
    double gg = material.gg;

    double eps[][3] = {
        { ex + sby,          sy,         sz + gz },
        { ex + sby + sbz,    sy,         sz      },
        { ex + sbz,          sy - gy,    sz      },
        { ex - sby + sbz,    sy,         sz      },
        { ex - sby,          sy,         sz - gz },
        { ex - sby - sbz,    sy,         sz      },
        { ex - sbz,          sy + gy,    sz      },
        { ex + sby - sbz,    sy,         sz      }
    };

    int imax = 0;
    double enmax = 0;

    for (int i = 0; i < 8; i++) {
        double en = ee * eps[i][0] * eps[i][0] + gg * (eps[i][1] * eps[i][1] + eps[i][2] * eps[i][2]);
        if (en > enmax) {
            imax = i;
            enmax = en;
        }
    }
    if (eps[imax][0] < 0)
        imax = (imax + 4) % 8;

    int j = (imax + 4) % 8;

    MatrixXd out(4, 6);

    out(0, 0) = eps[imax][0];
    out(0, 1) = 0;
    out(0, 2) = 0;
    out(0, 3) = eps[imax][1];
    out(0, 4) = 0;
    out(0, 5) = eps[imax][2];

    out(1, 0) = ee * eps[imax][0];
    out(1, 1) = 0;
    out(1, 2) = 0;
    out(1, 3) = gg * eps[imax][1];
    out(1, 4) = 0;
    out(1, 5) = gg * eps[imax][2];

    out(2, 0) = eps[j][0];
    out(2, 1) = 0;
    out(2, 2) = 0;
    out(2, 3) = eps[j][1];
    out(2, 4) = 0;
    out(2, 5) = eps[j][2];

    out(3, 0) = ee * eps[j][0];
    out(3, 1) = 0;
    out(3, 2) = 0;
    out(3, 3) = gg * eps[j][1];
    out(3, 4) = 0;
    out(3, 5) = gg * eps[j][2];

};


// 質量・重心周りの慣性モーメントを返す
// dens - 密度
// l - 要素長さ
VectorXd RectSection::massInertia(double dens, double l) {
    double dl = dens * l;
    double dly = dl * iz;
    double dlz = dl * iy;

    VectorXd out(4);
    out[0] = dl * area;
    out[1] = dly + dlz;
    out[2] = dly;
    out[3] = dlz;

};

// 断面を表す文字列を返す
string RectSection::toString() {

    //return format("{}\t{}\t{}\t{}",
    //    b1, h1, b2, h2);

    return "RectSection";
};

// 矩形断面の捩り係数を求める
// ba - 辺の長さ比b/a
VectorXd RectSection::rectCoef(double ba) {

    double dk1s = 0;
    double dks = 0;
    double dbs = 0;
    double pba = 0.5 * PI * ba;

    double dk1;
    double dk;
    double db;
    double ex;
    int sg = 1;

    int i = 1;
    do {
        ex = exp(-2 * pba * i);
        dk1 = (1 - ex) / ((i + ex) * pow(i, 5));
        dk1s += dk1;
        i += 2;
    } while (dk1 / dk1s > 1e-10);

    i = 1;
    do {
        dk = 2 / ((exp(pba * i) + exp(-pba * i)) * i * i);
        dks += dk;
        i += 2;
    } while (dk / dks > 1e-10);

    i = 1;
    do {
        ex = exp(-2 * pba * i);
        db = sg * (1 - ex) / ((i + ex) * i * i);
        dbs += db;
        i += 2;
        sg = -sg;
    } while (abs(db / dbs) > 1e-12);


    double k1 = 1 / 3 - COEF_K1 * dk1s / ba;
    double k = 1 - COEF_K * dks;
    double b = COEF_K * dbs;

    VectorXd out(4);
    out[0] = k1;
    out[1] = k;
    out[2] = k1 / k;
    out[3] = b / k;

    return out;
}
