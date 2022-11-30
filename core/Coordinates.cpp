#include "Coordinates.h"

#include <format>


//--------------------------------------------------------------------//
// 局所座標系
// label - 座標系ラベル
// n11,n12,n13,n21,n22,n23,n31,n32,n33 - 座標変換マトリックスの成分
Coordinates::Coordinates(){}

Coordinates::Coordinates(int _label, 
    double n11, double n12, double n13,
    double n21, double n22, double n23, 
    double n31, double n32, double n33) {
    
    label = _label;

    c[0] = n11;
    c[1] = n12;
    c[2] = n13;

    c[3] = n21;
    c[4] = n22;
    c[5] = n23;

    c[6] = n31;
    c[7] = n32;
    c[8] = n33;
};


// グローバル座標系に変換する
// x - ベクトル成分
void Coordinates::toGlobal(double x[6], double out[9]) {

    double e[9] = { c[0], c[3], c[6],
                    c[1], c[4], c[7],
                    c[2], c[5], c[8] };

    for (int i = 0; i < 3; i++) {
        out[i] = e[i] * x[0] + e[i + 3] * x[1] + e[i + 6] * x[2];
        out[i + 3] = e[i] * x[3] + e[i + 3] * x[4] + e[i + 6] * x[5];
    }

    return;
};


// 剛性マトリックスを変換する
// matrix - 剛性マトリックス
// dof - マトリックスの自由度
// idx0 - 節点ポインタ
// ndof - 節点自由度
void Coordinates::transMatrix(vector<vector<double>> matrix, int dof, int idx0, int ndof) {

    double e[9] = { c[0], c[3], c[6],
                    c[1], c[4], c[7],
                    c[2], c[5], c[8] };
        
        
    for (int i = 0; i < dof; i += 3) {
        vector<double> mi1 = matrix[i];
        vector<double> mi2 = matrix[i + 1];
        vector<double> mi3 = matrix[i + 2];

        for (int j = idx0; j < idx0 + ndof; j += 3) {

            double me[9] = {mi1[j], mi2[j], mi3[j], mi1[j + 1], mi2[j + 1], mi3[j + 1],
                mi1[j + 2], mi2[j + 2], mi3[j + 2] };

            for (int k = 0; k < 3; k++) {
                int ke3 = 3 * k;

                double e1 = e[ke3];
                double e2 = e[ke3 + 1];
                double e3 = e[ke3 + 2];

                mi1[j + k] = me[0] * e1 + me[3] * e2 + me[6] * e3;
                mi2[j + k] = me[1] * e1 + me[4] * e2 + me[7] * e3;
                mi3[j + k] = me[2] * e1 + me[5] * e2 + me[8] * e3;
            }

        }
    }

    for (int i = idx0; i < idx0 + ndof; i += 3) {
       vector<double> mi1 = matrix[i];
       vector<double> mi2 = matrix[i + 1];
       vector<double> mi3 = matrix[i + 2];

        for (int j = 0; j < dof; j += 3) {

            double me[9] = { mi1[j], mi2[j], mi3[j], mi1[j + 1], mi2[j + 1], mi3[j + 1],
                mi1[j + 2], mi2[j + 2], mi3[j + 2] };

            for (int k = 0; k < 3; k++) {
                int km3 = 3 * k;
                double me1 = me[km3];
                double me2 = me[km3 + 1];
                double me3 = me[km3 + 2];
                mi1[j + k] = e[0] * me1 + e[1] * me2 + e[2] * me3;
                mi2[j + k] = e[3] * me1 + e[4] * me2 + e[5] * me3;
                mi3[j + k] = e[6] * me1 + e[7] * me2 + e[8] * me3;
            }
        }
    }
}


// 荷重ベクトルを変換する
// vector - 荷重ベクトル
// dof - マトリックスの自由度
// idx0 - 節点ポインタ
// ndof - 節点自由度
void Coordinates::transVector(vector<double> vector, int dof, int idx0, int ndof) {
    
    double e[9] = { c[0], c[3], c[6],
                    c[1], c[4], c[7],
                    c[2], c[5], c[8] };

    for (int j = idx0; j < idx0 + ndof; j += 3) {
        double x1 = vector[j];
        double x2 = vector[j + 1];
        double x3 = vector[j + 2];
        vector[j] = e[0] * x1 + e[1] * x2 + e[2] * x3;
        vector[j + 1] = e[3] * x1 + e[4] * x2 + e[5] * x3;
        vector[j + 2] = e[6] * x1 + e[7] * x2 + e[8] * x3;
    }
};


// 局所座標系を表す文字列を返す
string Coordinates::toString() {

    return format("Coordinates\t{}\t{}\t{}\t{}\t{}\t{}\t{}\t{}\t{}\t{}", label, 
        c[0], c[3], c[6],
        c[1], c[4], c[7],
        c[2], c[5], c[8]);

};
