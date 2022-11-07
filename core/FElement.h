#pragma once
#include "Material.h";
#include "Nodes.h";
#include "BoundaryCondition.h";

#include <vector>
using std::vector;


//--------------------------------------------------------------------//
// �v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
class FElement : public Nodes {

private:
    const double C1_3 = 1 / 3;
    const double C1_6 = 1 / 6;
    const double C1_12 = 1 / 12;
    const double C1_24 = 1 / 24;

    // �O�p�`2���v�f�̃K�E�X�ϕ��̐ϕ��_���W
    const double GTRI2[2] = { 1 / 6, 2 / 3 };
    // �l�ʑ�2���v�f�̃K�E�X�ϕ��̐ϕ��_���W
    const double GTETRA2[2] = { 0.25 - 0.05 * sqrt(5), 0.25 + 0.15 * sqrt(5) };
    // �l�p�`1���v�f�̃K�E�X�ϕ��̐ϕ��_���W
    const double GX2[2] = { -1 / sqrt(3), 1 / sqrt(3) };
    // �l�p�`2���v�f�̃K�E�X�ϕ��̐ϕ��_���W
    const double GX3[3] = { -sqrt(0.6), 0, sqrt(0.6) };
    // �K�E�X�ϕ��̏d�݌W��
    const double GW3[3] = { 5 / 9, 8 / 9, 5 / 9 };

    int label;
    Material material;
    bool isShell;
    bool isBar;

public:
    FElement(int _label, Material _material, vector<int> _nodes);

    // �ϕ��_�̍����}�g���b�N�X��Ԃ�
    void stiffPart(vector<vector<double>> d, vector<vector<double>> b, double coef, vector<vector<double>> k);

    // �ߓ_�ψʂ�1�����z��ɕϊ�����
    void toArray(vector<BoundaryCondition> u, int dof, vector<double> out);

    // �ߓ_�ψʂ��Ǐ����W�n�E1�����z��ɕϊ�����
    void toLocalArray(vector<BoundaryCondition> u, vector<vector<double>>d, vector<double> v);
};