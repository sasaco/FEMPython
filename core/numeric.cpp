#include "numeric.h";

double numeric::dotVV(vector<double> x, vector<double> y) {

    int n = x.size();
    double ret = x[n - 1] * y[n - 1];

    int i = 0;
    for (i = n - 2; i >= 1; i -= 2) {
        int i1 = i - 1;
        ret += x[i] * y[i] + x[i1] * y[i1];
    }
    if (i == 0) {
        ret += x[0] * y[0];
    }
    return ret;
}


void numeric::dotMV(vector<vector<double>> x, vector<double> y, vector<double> out) {

    int p = x.size();
    int q = y.size();

    for (int i = 0; i < p; ++i) {
        out.push_back(0);
    }

    for (int i = p - 1; i >= 0; i--) {
        out[i] = dotVV(x[i], y);
    }
}


void numeric::dotVM(vector<double> x, vector<vector<double>> y, vector<double> out) {

    int p = x.size();
    int q = y[0].size();

    for (int i = 0; i < q; ++i) {
        out.push_back(0);
    }

    for (int k = q - 1; k >= 0; k--) {
        double woo = x[p - 1] * y[p - 1][k];
        int j = 0;
        for (j = p - 2; j >= 1; j -= 2) {
            int i0 = j - 1;
            woo += x[j] * y[j][k] + x[i0] * y[i0][k];
        }
        if (j == 0) {
            woo += x[0] * y[0][k];
        }
        out[k] = woo;
    }
}


/// <summary>
/// x に y の値を加算する
/// </summary>
/// <param name="x"></param>
/// <param name="y"></param>
void numeric::add(vector<double> x, vector<double> y) {

    int n = min(x.size(), y.size());

    for (int i = 0; i < n; i++) {
        x[i] = x[i] + y[i];
    }
}


/// <summary>
/// x に y の値を乗じる
/// </summary>
/// <param name="x"></param>
/// <param name="y"></param>
void numeric::mul(vector<double> x, double y) {

    for (int i = 0; i < x.size(); i++) {
        x[i] = x[i] * i;
    }
}

/// <summary>
/// nxn 行列の逆行列を求める
/// </summary>
void numeric::getInverse(vector<vector<double>> a, vector<vector<double>> inv_a) {

    inv_a.clear();

    //単位行列を作る
    for (int i = 0; i < a.size(); i++) {
        vector<double> b;
        for (int j = 0; j < a[i].size(); j++) {
            b.push_back((i == j) ? 1.0 : 0.0);
        }
        inv_a.push_back(b);
    }

    //掃き出し法
    for (int i = 0; i < a.size(); i++) {
        double buf = 1 / a[i][i];
        for (int j = 0; j < a[i].size(); j++) {
            a[i][j] *= buf;
            inv_a[i][j] *= buf;
        }
        for (int j = 0; j < a.size(); j++) {
            if (i != j) {
                buf = a[j][i];
                for (int k = 0; k < a[i].size(); k++) {
                    a[j][k] -= a[i][k] * buf;
                    inv_a[j][k] -= inv_a[i][k] * buf;
                }
            }
        }
    }

 
}

