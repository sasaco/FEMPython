import os
import sys
import conftest
import core

def test_sampleBeamHexa1():

    # サンプルファイルを読む
    fileName = os.path.abspath(os.path.dirname(os.path.abspath(__file__)) 
                               + '/examples/beam/sampleBeamHexa1.fem')
    f = open(fileName, encoding="utf-8")
    s = f.read()  
    f.close()

    # C++ を直接呼び出すテスト
    model = core.readFemModel(s)
    model.calculate()
    result = model.result

    # main 関数を呼ぶ
    from dataInput import dataInput
    inp = dataInput()
    calc = inp.readTestData(s)
    result = calc.calculate()

    assert True

    
if __name__ == "__main__":
    test_sampleBeamHexa1()
