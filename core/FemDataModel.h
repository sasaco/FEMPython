#pragma once
#include "MeshModel.h"
#include "Material.h"
#include "ShellParameter.h"
#include "BarParameter.h"
#include "BoundaryCondition.h"
#include "Result.h"


#include <format>
#include <numbers>
#include <vector>
#include <map>
using namespace std;
using std::vector;


//--------------------------------------------------------------------//
// FEM �f�[�^���f��
class FemDataModel {

private:
    double COEF_F_W = 0.5 / numbers::pi;    // f/�֔� 1/2��


    bool hasShellBar;                       // �V�F���v�f�܂��͗��v�f���܂܂Ȃ�
    // Solver solver;		                    // �A�������������I�u�W�F�N�g

    // Method
    void reNumbering();

    void resetNodes(map<int, int> map, ElementManager &s);
    void resetNodePointer(map<int, int> map, int &node);
    void resetElementPointer(map<int, int> map, int &element);

    void resetMaterialLabel();
    void resetParameterLabel();
    void resetCoordinates();

    template <typename T>
    void resetCoordinatesPointer(map<int, Coordinates> map, T &bc);


public:
    Result result;		                    // �v�Z����
    MeshModel mesh;		                    // ���b�V�����f��
    BoundaryCondition bc;	                // ���E����
    vector<Material> materials;             // �ޗ�
    vector<ShellParameter> shellParams;     // �V�F���p�����[�^
    vector<BarParameter> barParams;         // ���p�����[�^
    vector<Coordinates> coordinates;	    // �Ǐ����W�n

    FemDataModel();

    void clear();
    void init();
    int setNodeDoF();
    // void calculate();
    void calculateElementStress();
    void calculateNodeStress();

};