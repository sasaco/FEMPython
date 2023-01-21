#pragma once
#include <string>
#include <iostream>
using namespace std;


class FemDataModel {

private:
    string s = "";

public:

    FemDataModel();

    void clear();
    void init();
    void readString(string str);
    void printString();

};