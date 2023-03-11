#include <pybind11/pybind11.h>
#include <pybind11/stl.h>

#include "Solver.h"

// FileIO.cpp で定義した関数を使用します
extern Solver readFemModelDataStr(string s);

PYBIND11_MODULE(core, m){

    // 関数のbind
    m.def("readFemModel", &readFemModelDataStr);

    // クラスのbind
    pybind11::class_<SymmetricTensor3>(m, "SymmetricTensor3")
        .def(pybind11::init())
        .def_readonly("xx", &SymmetricTensor3::xx)
        .def_readonly("yy", &SymmetricTensor3::yy)
        .def_readonly("zz", &SymmetricTensor3::zz)
        .def_readonly("xy", &SymmetricTensor3::xy)
        .def_readonly("yz", &SymmetricTensor3::yz)
        .def_readonly("zx", &SymmetricTensor3::zx);

    pybind11::class_<Strain, SymmetricTensor3>(m, "Strain")
        .def(pybind11::init());

    pybind11::class_<Stress, SymmetricTensor3>(m, "Stress")
        .def(pybind11::init());

    pybind11::class_<Result>(m, "Result")
        .def(pybind11::init())
        .def_readonly("dispMax", &Result::dispMax)
        .def_readonly("angleMax", &Result::angleMax)
        .def_readonly("strain1", &Result::strain1)
        .def_readonly("stress1", &Result::stress1)
        .def_readonly("sEnergy1", &Result::sEnergy1)
        .def_readonly("strain2", &Result::strain2)
        .def_readonly("stress2", &Result::stress2)
        .def_readonly("sEnergy2", &Result::sEnergy2)
        .def_readonly("temperature", &Result::temperature)
        .def_readonly("tempMax", &Result::tempMax)
        .def_readonly("calculated", &Result::calculated);

    pybind11::class_<FemDataModel>(m, "FemDataModel")
        .def(pybind11::init())
        .def_readonly("result", &FemDataModel::result);

    pybind11::class_<Solver, FemDataModel>(m, "Solver")
        .def(pybind11::init())
        .def("clear", &Solver::clear)
        .def("calculate", &Solver::calculate);
}
