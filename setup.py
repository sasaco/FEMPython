from setuptools import setup, find_packages
import src

def _requires_from_file(filename):
    return open(filename).read().splitlines()

try:
    with open('README.md', encoding="utf-8") as f:
        readme = f.read()
except IOError:
    readme = ''

setup(
    name =              'FEMPython',
    author =            'Yosuke Sasazawa',
    author_email =      'sasaicco@gmail.com',
    maintainer =        'Yosuke Sasazawa',
    maintainer_email =  'sasaicco@gmail.com',
    description =       "FEMPython is a Python package for solving partial differential equations using the finite element method.",
    long_description =  readme,
    license =           'MIT License',
    url =               'https://github.com/sasaco/FEMPython',
    version =           src.__version__,
    download_url =      'https://pypi.org/project/FEMPython/',
    python_requires =   ">=3.6",
    install_requires =  _requires_from_file('requirements.txt'),
    packages =          find_packages('src', exclude=['test']),
    classifiers = [
        'Intended Audience :: Science/Research',
        'License :: SI Approved :: MIT License',
        'Programming Language :: Python :: 3',
        'Programming Language :: Python :: 3.6',
        'Programming Language :: Python :: 3.7',
        'Programming Language :: Python :: 3.8',
        'Programming Language :: Python :: 3.9',
        'Programming Language :: Python :: 3 :: Only',
        'Topic :: Scientific/Engineering',
        'Topic :: Scientific/Engineering :: Visualization',
        'Topic :: Scientific/Engineering :: Artificial Intelligence',
        'Topic :: Multimedia :: Graphics',
        'Framework :: Matplotlib',
    ]
)