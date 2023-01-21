#include <Python.h>

#include <string>
#include <vector>
#include <sstream> // std::stringstream
#include <istream> // std::getline
#include "FemDataModel.h"

using namespace std;
using std::vector;

// FileIO.cpp で定義した関数を使用します。
extern FemDataModel readFemModel(string s);


PyObject* CreadFemModel(PyObject* self, PyObject* args) {

	char* str = NULL;
	if (!PyArg_ParseTuple(args, "s", &str)) {
		return NULL;
	}

	if(str == NULL) {
		return NULL;
	}

	FemDataModel model = readFemModel(string(str));


	string result = "OK";
	return Py_BuildValue("s", result);
}

static PyMethodDef FEMCore_methods[] = {

	// The first property is the name exposed to Python, fast_tanh, the second is the C++
	// function name that contains the implementation.
	{ "readFemModel", (PyCFunction)CreadFemModel, METH_VARARGS, "Prints Message" },

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