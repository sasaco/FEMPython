import "CircleSection.h";


#include <format>
using namespace std;         //  ���O��Ԏw��


//--------------------------------------------------------------------//
// �~�`�f��
// ss - �f�[�^������
CircleSection::CircleSection(double ss[2]) {

    d1 = ss[0];	// �O�a
    d2 = ss[1];	// ���a
    // �f�ʐ�
    area = 0.25 * PI * (d1 * d1 - d2 * d2);
    // �f�ʂQ�����[�����g
    iy = 0.0625 * area * (d1 * d1 + d2 * d2);
    iz = iy;
    ip = iy + iz;	// �f�ʂQ���Ƀ��[�����g
};


// ����f�␳�W����Ԃ�
double CircleSection::shearCoef() {
    return KS_CIRCLE;
};

// �c�E���̓x�N�g����Ԃ�
// material - �ޗ�
// ex - �������k�c
// thd - �䝀��p
// kpy,kpz - �Ȃ��ɂ��ȗ�
// sy,sz - �f�ʂ���f�c
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

// ���ʁE�d�S����̊������[�����g��Ԃ�
// dens - ���x
// l - �v�f����
void CircleSection::massInertia(double dens, double l, double out[4]) {

    double dl = dens * l;
    double dly = dl * iy;

    out[0] = dl * area;
    out[1] = 2 * dly;
    out[2] = dly;
    out[3] = dly;

};

// �f�ʂ�\���������Ԃ�
string CircleSection::toString() {

    return format("{}\t{}",
        d1, d2);

};