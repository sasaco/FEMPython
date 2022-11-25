#pragma once

//--------------------------------------------------------------------//
// FEM �f�[�^���f��
#include <numbers>
#include <vector>
using namespace std;
using std::vector;


class FemDataModel {

private:
    double COEF_F_W = 0.5 / numbers::pi;    // f/�֔� 1/2��
    /*
    vector<Material> materials;             // �ޗ�
    vector<ShellParameter> shellParams;     // �V�F���p�����[�^
    vector<BarParameter> barParams;         // ���p�����[�^
    vector<Coordinates> coordinates;	    // �Ǐ����W�n
    MeshModel mesh;		                    // ���b�V�����f��
    BoundaryCondition bc;	                // ���E����
    Solver solver;		                    // �A�������������I�u�W�F�N�g
    Result result;		                    // �v�Z����
    bool hasShellBar;                       // �V�F���v�f�܂��͗��v�f���܂܂Ȃ�

    // Method
    void init();
    void reNumbering();
    */
public:

    FemDataModel();
    void clear();

};