from distutils.core import setup, Extension


sfc_module = Extension('FEMCore', sources=['module.cpp'])

setup(name='FEMCore', version='1.0',
      description='Python Package with FEMCore C++ extension',
      ext_modules=[sfc_module]
      )
