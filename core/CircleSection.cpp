#include "CircleSection.h"


//--------------------------------------------------------------------//
// 円形断面
// ss - データ文字列
CircleSection::CircleSection(double ss[2]) {

    d1 = ss[0];	// 外径
    d2 = ss[1];	// 内径
    // 断面積
    area = 0.25 * PI * (d1 * d1 - d2 * d2);
    // 断面２次モーメント
    iy = 0.0625 * area * (d1 * d1 + d2 * d2);
    iz = iy;
    ip = iy + iz;	// 断面２次極モーメント
};


// せん断補正係数を返す
double CircleSection::shearCoef() {
    return KS_CIRCLE;
};

// 歪・応力ベクトルを返す
// material - 材料
// ex - 引張圧縮歪
// thd - 比捩り角
// kpy,kpz - 曲げによる曲率
// sy,sz - 断面せん断歪
void CircleSection::strainStress(Material material, double ex, double thd, double kpy, double kpz,
    double sy, double sz, double out[4][6]) {

    double kp = sqrt(kpy * kpy + kpz * kpz);
    double kpr = 0.5 * kp * d1;

    double gx = 0.5 * d1 * thd;
    double gy = 0;
    double gz = gx;

    if (kp > 0) {
        gy = -gx * kpz / kp;
        gz = gx * kpy / kp;
    }

    double ee = material.ee;
    double gg = material.gg;

    out[0][0] = ex + kpr;
    out[0][1] = 0;
    out[0][2] = 0;
    out[0][3] = gy + sy;
    out[0][4] = 0;
    out[0][5] = gz + sz;

    out[1][0] = ee * (ex + kpr);
    out[1][1] = 0;
    out[1][2] = 0;
    out[1][3] = gg * (gy + sy);
    out[1][4] = 0;
    out[1][5] = gg * (gz + sz);

    out[2][0] = ex - kpr;
    out[2][1] = 0;
    out[2][2] = 0;
    out[2][3] = -gy + sy;
    out[2][4] = 0;
    out[2][5] = -gz + sz;

    out[3][0] = ee * (ex - kpr);
    out[3][1] = 0;
    out[3][2] = 0;
    out[3][3] = gg * (-gy + sy);
    out[3][4] = 0;
    out[3][5] = gg * (-gz + sz);

};

// 質量・重心周りの慣性モーメントを返す
// dens - 密度
// l - 要素長さ
void CircleSection::massInertia(double dens, double l, double out[4]) {

    double dl = dens * l;
    double dly = dl * iy;

    out[0] = dl * area;
    out[1] = 2 * dly;
    out[2] = dly;
    out[3] = dly;

};

// 断面を表す文字列を返す
string CircleSection::toString() {

    return format("{}\t{}",
        d1, d2);

};