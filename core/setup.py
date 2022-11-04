from distutils.core import setup, Extension


sfc_module = Extension('Core', sources=['module.cpp'])

setup(name='Core', version='1.0',
      description='Python Package with Core C++ extension',
      ext_modules=[sfc_module]
      )