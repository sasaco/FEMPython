#include "FElement.h";

#include <numeric>


//--------------------------------------------------------------------//
// �v�f
// label - �v�f���x��
// material - �ޗ��̃C���f�b�N�X
// nodes - �ߓ_�ԍ�
FElement::FElement(int _label, int _material, vector<int> _nodes)
    : Nodes(_nodes) {

    label = label;
    material = _material;
    isShell = false;        // �V�F���v�f�ł͂Ȃ�
    isBar = false;		    // ���v�f�ł͂Ȃ�
};

// �ϕ��_�̍����}�g���b�N�X��Ԃ�
// d - ����-�c�}�g���b�N�X
// b - �c-�ψʃ}�g���b�N�X�̓]�u�s��
// coef - �W��
void FElement::stiffPart(vector<vector<double>> d, vector<vector<double>> b, double coef, vector<vector<double>> out) {

    int size1 = b.size();
    int size2 = d.size();
    
    vector<double> a;

    for (int i = 0; i < size1; i++) {

        vector<double> bi = b[i];

        for (int j = 0; j < size2; j++) {
            a.push_back(
                coef * inner_product(bi.begin(), bi.end(), d[j].begin(), 0)
            );
        }

        vector<double> ki;

        for (int j = 0; j < size1; j++) {
            ki.push_back(
                inner_product(a.begin(), a.end(), b[j].begin(), 0)
            );
        }
        out.push_back(ki);
    }
}

// �ߓ_�ψʂ�1�����z��ɕϊ�����
// u - �ߓ_�ψ�
// dof - �ߓ_���R�x
void FElement::toArray(vector<BoundaryCondition> u, int dof, vector<double> out) {

    int count = nodeCount();
    out.clear();

    for (int i = 0; i < count; i++) {
        vector<double> ux = u[i].x;
        for (int j = 0; j < dof; j++) {
            out.push_back(ux[j]);
        }
    }
};

// �ߓ_�ψʂ��Ǐ����W�n�E1�����z��ɕϊ�����
// u - �ߓ_�ψ�
// d - �����]���}�g���b�N�X
void FElement::toLocalArray(vector<BoundaryCondition> u, vector<vector<double>> d, vector<double> v) {

    v.clear();

    for (int i = 0; i < 2; i++) {
        vector<double> ux = u[i].x;
        for (int j = 0; j < 3; j++) {
            v.push_back(d[0][j] * ux[0] + d[1][j] * ux[1] + d[2][j] * ux[2]);
        }
        for (int j = 0; j < 3; j++) {
            v.push_back(d[0][j] * ux[3] + d[1][j] * ux[4] + d[2][j] * ux[5]);
        }
    }
}


