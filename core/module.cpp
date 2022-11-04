/*
* https://learn.microsoft.com/ja-jp/visualstudio/python/working-with-c-cpp-python-in-visual-studio?view=vs-2022
*/

#include <Windows.h>
#include <cmath>
#include <Python.h>

const double e = 2.7182818284590452353602874713527;

double sinh_impl(double x) {
    return (1 - pow(e, (-2 * x))) / (2 * pow(e, -x));
}

double cosh_impl(double x) {
    return (1 + pow(e, (-2 * x))) / (2 * pow(e, -x));
}

/// Python �̌^ (�܂�APyObject*) ���󂯓���Ė߂��悤�ɁAtanh_impl ���\�b�h��ύX���܂��B
//double tanh_impl(double x) {
//    return sinh_impl(x) / cosh_impl(x);
//}

PyObject* tanh_impl(PyObject* /* unused module reference */, PyObject* o) {
    double x = PyFloat_AsDouble(o);
    double tanh_x = sinh_impl(x) / cosh_impl(x);
    return PyFloat_FromDouble(tanh_x);
}

/// Python �ɑ΂��� C++ �� tanh_impl �֐���񎦂�����@���`����\���̂�ǉ����܂��B
static PyMethodDef Core_methods[] = {
    // The first property is the name exposed to Python, fast_tanh
    // The second is the C++ function with the implementation
    // METH_O means it takes a single PyObject argument
    { "fast_tanh", (PyCFunction)tanh_impl, METH_O, nullptr },

    // Terminate the array with an object containing nulls.
    { nullptr, nullptr, 0, nullptr }
};

/// Python �R�[�h�ŎQ�Ƃ��郂�W���[�����`����\���̂�ǉ����܂�
static PyModuleDef Core_module = {
    PyModuleDef_HEAD_INIT,
    "Core",                        // Module name to use with Python import statements
    "Provides some functions, but faster",  // Module description
    0,
    Core_methods                   // Structure that defines the methods of the module
};

PyMODINIT_FUNC PyInit_Core() {
    return PyModule_Create(&Core_module);
}