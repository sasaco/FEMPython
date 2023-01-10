import pytest
import sys 
import os

def test_sampleBeamHexa1():

    fileName = os.path.abspath(os.path.dirname(os.path.abspath(__file__)) + '/../examples/beam/sampleBeamHexa1.fem')
    f = open(fileName, encoding="utf-8")
    fstr = f.read()  # ファイル終端まで全て読んだデータを返す
    f.close()

    # ここに pybind11 をつかって c++ と連携する処理
    ###
    ###
    ###
    ###

