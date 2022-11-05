/*
* https://learn.microsoft.com/ja-jp/visualstudio/python/working-with-c-cpp-python-in-visual-studio?view=vs-2022
*/

#include <Windows.h>
#include <cmath>
#include <Python.h>

PyObject* stiffnessMatrix(PyObject* self, PyObject* args) {

    return PyFloat_FromDouble(3.9);
}
static PyMethodDef Core_methods[] = {
    { 
        "stiffnessMatrix",              // The first property is the name exposed to Python,
        (PyCFunction)stiffnessMatrix,   // The second is the C++ function with the implementation
        METH_O,                         // METH_O means it takes a single PyObject argument
        nullptr 
    },
    { nullptr, nullptr, 0, nullptr }    // Terminate the array with an object containing nulls.
};

static PyModuleDef Core_module = {
    PyModuleDef_HEAD_INIT,
    "FEMCore",                              // Module name to use with Python import statements
    "Provides some functions, but faster",  // Module description
    0,
    Core_methods                            // Structure that defines the methods of the module
};

PyMODINIT_FUNC PyInit_Core() {
    return PyModule_Create(&Core_module);
}