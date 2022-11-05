

# CPython ägí£ã@î\ (module.cpp) ÇÃèÍçá:

from distutils.core import setup, Extension


sfc_module = Extension('Core', sources=['module.cpp'])

setup(name='Core', version='1.0',
      description='Python Package with Core C++ extension',
      ext_modules=[sfc_module]
      )
"""



# PyBind11 (Solver.cpp) ÇÃèÍçá:
from setuptools import setup, Extension
import pybind11

cpp_args = ['-std=c++11', '-stdlib=libc++', '-mmacosx-version-min=10.7']

sfc_module = Extension(
    'Core',
    sources=['Solver.cpp'],
    include_dirs=[pybind11.get_include()],
    language='c++',
    extra_compile_args=cpp_args,
    )

setup(
    name='Core',
    version='1.0',
    description='Python Package with Core C++ extension (PyBind11)',
    ext_modules=[sfc_module],
)
"""