#include <pybind11/pybind11.h>
#include <pybind11/stl.h>

#include "Solver.h"

// FileIO.cpp で定義した関数を使用します
extern Solver readFemModelDataStr(string s);

PYBIND11_MODULE(core, m){

    // テスト用サンプルデータ読み取り関数のbind
    m.def("readFemModel", &readFemModelDataStr);

    // クラスのbind
    pybind11::class_<FemDataModel>(m, "FemDataModel")
        .def(pybind11::init())
        .def("init", &FemDataModel::init)
        .def_readwrite("mesh", &FemDataModel::mesh)
        .def_readwrite("bc", &FemDataModel::bc)
        .def_readwrite("materials", &FemDataModel::materials)
        .def_readwrite("shellParams", &FemDataModel::shellParams)
        .def_readwrite("barParams", &FemDataModel::barParams)
        .def_readwrite("coordinates", &FemDataModel::coordinates)
        .def_readwrite("result", &FemDataModel::result);

    pybind11::class_<Solver, FemDataModel>(m, "Solver")
        .def(pybind11::init())
        .def("clear", &Solver::clear)
        .def("calculate", &Solver::calculate);


    // データをインプットするクラス
    pybind11::class_<Material>(m, "Material")
        .def(pybind11::init<int, double, double, double, double, double>())
        .def_readwrite("label", &Material::label)
        .def_readwrite("ee", &Material::ee)
        .def_readwrite("nu", &Material::nu)
        .def_readwrite("dens", &Material::dens)
        .def_readwrite("hCon", &Material::hCon)
        .def_readwrite("sHeat", &Material::sHeat)
        .def_readwrite("cv", &Material::cv)
        .def_readwrite("gg", &Material::gg);


    pybind11::class_<MeshModel>(m, "MeshModel")
        .def(pybind11::init())
        .def_readwrite("nodes", &MeshModel::nodes)
        .def_readwrite("elements", &MeshModel::elements);

    pybind11::class_<BoundaryCondition>(m, "BoundaryCondition")
        .def(pybind11::init())
        .def_readwrite("bcList", &BoundaryCondition::bcList)
        .def_readwrite("restraints", &BoundaryCondition::restraints)
        .def_readwrite("loads", &BoundaryCondition::loads)
        .def_readwrite("pressures", &BoundaryCondition::pressures)
        .def_readwrite("temperature", &BoundaryCondition::temperature)
        .def_readwrite("htcs", &BoundaryCondition::htcs);

    pybind11::class_<Vector3Dim>(m, "Vector3Dim")
        .def(pybind11::init())
        .def_readwrite("x", &Vector3Dim::x)
        .def_readwrite("y", &Vector3Dim::y)
        .def_readwrite("z", &Vector3Dim::z);

    pybind11::class_<FENode, Vector3Dim>(m, "FENode")
        .def(pybind11::init<string, double, double, double>()) 
        .def_readwrite("label", &FENode::label);

    pybind11::class_<ElementManager>(m, "ElementManager")
        .def(pybind11::init<string, vector<string>>())
        .def("nodes", &ElementManager::nodes)
        .def("label", &ElementManager::label);

    pybind11::class_<Vector3R>(m, "Vector3R")
        .def(pybind11::init())
        .def_readwrite("x", &Vector3R::x);

    pybind11::class_<Restraint, Vector3R>(m, "Restraint")
        .def(pybind11::init<vector<string>>())
        .def_readwrite("node", &Restraint::node)
        .def_readwrite("coords", &Restraint::coords)
        .def_readwrite("rest", &Restraint::rest);

    pybind11::class_<Load, Vector3R>(m, "Load")
        .def(pybind11::init<vector<string>>())
        .def_readwrite("node", &Load::node)
        .def_readwrite("coords", &Load::coords)
        .def_readwrite("globalX", &Load::globalX);


    // 計算結果を参照するクラス
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
        .def_readwrite("type", &Result::type)
        .def_readonly("ELEMENT_DATA", &Result::ELEMENT_DATA)
        .def_readonly("NODE_DATA", &Result::NODE_DATA)
        .def_readonly("displacement", &Result::displacement)
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

}
