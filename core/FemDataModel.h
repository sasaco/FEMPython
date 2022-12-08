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
#include <format>
#include <numbers>
#include <vector>
#include <map>
using namespace std;
using std::vector;


class FemDataModel {

private:
    double COEF_F_W = 0.5 / numbers::pi;    // f/�֔� 1/2��


    bool hasShellBar;                       // �V�F���v�f�܂��͗��v�f���܂܂Ȃ�
    Solver solver;		                    // �A�������������I�u�W�F�N�g
    Result result;		                    // �v�Z����

    // Method
    void reNumbering();

    void resetNodes(map<int, int> map, ElementManager &s);
    void resetNodePointer(map<int, int> map, int &node);
    void resetElementPointer(map<int, int> map, int &element);

    void resetMaterialLabel();
    void resetParameterLabel();
    void resetCoordinates();

    void resetCoordinatesPointer(map<int, Coordinates> map, Restraint &bc);
    void resetCoordinatesPointer(map<int, Coordinates> map, Load &bc);


public:
    MeshModel mesh;		                    // ���b�V�����f��
    BoundaryCondition bc;	                // ���E����
    vector<Material> materials;             // �ޗ�
    vector<ShellParameter> shellParams;     // �V�F���p�����[�^
    vector<BarParameter> barParams;         // ���p�����[�^
    vector<Coordinates> coordinates;	    // �Ǐ����W�n

    FemDataModel();

    void clear();
    void init();
    void calculate();

};