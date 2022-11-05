#include <Python.h>
#include <Windows.h>
#include <cmath>

const double e = 2.7182818284590452353602874713527;

double sinh_impl(double x) {
	return (1 - pow(e, (-2 * x))) / (2 * pow(e, -x));
}

double cosh_impl(double x) {
	return (1 + pow(e, (-2 * x))) / (2 * pow(e, -x));
}

PyObject* tanh_impl(PyObject *, PyObject* o) {
	double x = PyFloat_AsDouble(o);
	double tanh_x = sinh_impl(x) / cosh_impl(x);
	return PyFloat_FromDouble(tanh_x);
}

static PyMethodDef FEMCore_methods[] = {
	// The first property is the name exposed to Python, fast_tanh, the second is the C++
	// function name that contains the implementation.
	{ "fast_tanh", (PyCFunction)tanh_impl, METH_O, nullptr },

	// Terminate the array with an object containing nulls.
{ nullptr, nullptr, 0, nullptr }
};

static PyModuleDef FEMCore_module = {
	PyModuleDef_HEAD_INIT,
	"FEMCore",                        // Module name to use with Python import statements
	"Provides some functions, but faster",  // Module description
	0,
	FEMCore_methods                   // Structure that defines the methods of the module
};

PyMODINIT_FUNC PyInit_FEMCore() {
	return PyModule_Create(&FEMCore_module);
}