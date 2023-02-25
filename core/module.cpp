#include <pybind11/pybind11.h>


#include "Solver.h"

// FileIO.cpp で定義した関数を使用します
extern Solver readFemModelDataStr(string s);

PYBIND11_MODULE(core, m){

    // 関数のbind
    m.def("readFemModel", &readFemModelDataStr);

    // クラスのbind
    pybind11::class_<Solver>(m, "Solver")
        .def(pybind11::init())
        .def("clear", &Solver::clear)
        .def("calculate", &Solver::calculate);
            
}
