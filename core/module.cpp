#include <pybind11/pybind11.h>
#include "FemDataModel.h"

#include <Eigen/Core> //Eigenを指定してもコンパイルが通ることを確認

#include <string>
using namespace std;

FemDataModel readFemModel(string s){
    printf("function readFemModel() was executed.\n");
    FemDataModel model = FemDataModel();
    // インスタンス内のprivate変数に文字列を格納したインスタンスを返す
    model.readString(s);
    return model;
};

PYBIND11_MODULE(core, m){

    // 関数のbind
    m.def("readFemModel", &readFemModel);

    // クラスのbind
    pybind11::class_<FemDataModel>(m, "FemDataModel")
            .def(pybind11::init())
            .def("init", &FemDataModel::init)
            .def("clear", &FemDataModel::clear);
            // .def("read_string", &FemDataModel::readString)
            // .def("print_string", &FemDataModel::printString);
            
}
