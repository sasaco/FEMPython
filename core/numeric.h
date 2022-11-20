#pragma once
#include <vector>
using namespace std;

static class numeric {

private:

public:


    static void getInverse(vector<vector<double>> a, vector<vector<double>> out);

    static double dotVV(vector<double> x, vector<double> y);

    static void dotMV(vector<vector<double>> x, vector<double> y, vector<double> out);

    static void dotVM(vector<double> x, vector<vector<double>> y, vector<double> out);

    static void add(vector<double> x, vector<double> y);

    static void mul(vector<double> x, double y);

    static void rep(int row, int col, vector<vector<double>> out);
};