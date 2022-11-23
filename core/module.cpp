#include <Python.h>

PyObject* tanh_impl(PyObject* self, PyObject* args) {

	char* str;
	if (!PyArg_ParseTuple(args, "s", &str)) {
		return NULL;
	}
	printf("%s\n", str);
	return Py_BuildValue("s", str);
}

static PyMethodDef FEMCore_methods[] = {

	// The first property is the name exposed to Python, fast_tanh, the second is the C++
	// function name that contains the implementation.
	{ "fast_tanh", (PyCFunction)tanh_impl, METH_VARARGS, "Prints Message" },

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