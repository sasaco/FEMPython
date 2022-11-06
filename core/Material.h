#pragma once

#include <string>
using namespace std;         //  ���O��Ԏw��


//--------------------------------------------------------------------//
// �ޗ�
// label - �ޗ��ԍ�
// ee - �����O�� (�c�e���W��) 
// nu - �|�A�\����
// dens - ���x
// hCon - �M�`����
// sHeat - ��M
class Material {

private:

    int label;
    double nu;
    double dens;
    double hCon;
    double sHeat;
    double cv;	// �̐ϔ�M

    void matrix2Dstrain(double out[3][3]);
    void matrixAxiSymetric(double out[4][4]);
    void matrixTorsion(double out[2][2]);

public:
    Material(int _label, double _ee, double _nu, double _dens, double _hCon, double _sHeat);

    double ee;
    //double nu;
    //double dens;
    //double hCon;
    //double sHeat;
    //double cv;	// �̐ϔ�M
    double gg;    // ���e���W��


    double m2d[3][3]; // set from matrix2Dstress
    void matrix2Dstress();

    double msh[5][5]; // set from matrixShell
    void matrixShell();

    void matrix3D();
    double m3d[6][6]; // set from matrix3D

    string toString();

};