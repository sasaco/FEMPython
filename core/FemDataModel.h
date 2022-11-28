#pragma once
#include "MeshModel.h"
#include "Material.h"
#include "ShellParameter.h"
#include "BarParameter.h"
#include "BoundaryCondition.h"
#include "Solver.h"
#include "Result.h"


//--------------------------------------------------------------------//
// FEM �f�[�^���f��
#include <numbers>
#include <vector>
using namespace std;
using std::vector;


class FemDataModel {

private:
    const double COEF_F_W = 0.5 / numbers::pi;    // f/�֔� 1/2��

    MeshModel mesh;		                    // ���b�V�����f��
    BoundaryCondition bc;	                // ���E����

    vector<Material> materials;             // �ޗ�
    vector<ShellParameter> shellParams;     // �V�F���p�����[�^
    vector<BarParameter> barParams;         // ���p�����[�^
    vector<Coordinates> coordinates;	    // �Ǐ����W�n
    bool hasShellBar;                       // �V�F���v�f�܂��͗��v�f���܂܂Ȃ�
    Solver solver;		                    // �A�������������I�u�W�F�N�g
    Result result;		                    // �v�Z����

    /*

    // Method
    void init();
    void reNumbering();
*/

public:

    FemDataModel();
    void clear();

};