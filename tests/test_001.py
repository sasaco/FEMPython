import pytest
import sys 
import os

def test_sampleBeamHexa1():

    fileName = os.path.abspath(os.path.dirname(os.path.abspath(__file__)) + '/examples/beam/sampleBeamHexa1.fem')
    f = open(fileName, encoding="utf-8")
    fstr = f.read()  # ファイル終端まで全て読んだデータを返す
    f.close()

    from FEMCore import readFemModel
    c = readFemModel(fstr) 

    print('Running benchmarks with COUNT = {}'.format(c))



if __name__ == "__main__":
    sys.path.append(os.path.abspath(os.path.dirname(os.path.abspath(__file__)) + "/../"))
    import conftest

    test_sampleBeamHexa1()
