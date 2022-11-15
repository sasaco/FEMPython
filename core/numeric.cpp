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
/// x Ç… y ÇÃílÇâ¡éZÇ∑ÇÈ
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
/// x Ç… y ÇÃílÇèÊÇ∂ÇÈ
/// </summary>
/// <param name="x"></param>
/// <param name="y"></param>
void numeric::mul(vector<double> x, double y) {

    for (int i = 0; i < x.size(); i++) {
        x[i] = x[i] * i;
    }
}

