#include "FemDataModel.h"


FemDataModel::FemDataModel() {
    printf("FemDataModel instance was constructed.\n");
};

// データを消去する
void FemDataModel::clear(){
    printf("function clear() was executed.\n");
};

// モデルを初期化する
void FemDataModel::init(){
    printf("function init() was executed.\n");
};

// サンプルコード テキストを読み込む
void FemDataModel::readString(string str){
    // private変数にstrを格納
    s = str;
};

// サンプルコード テキストを出力する
void FemDataModel::printString(){
    cout << s << endl;
};