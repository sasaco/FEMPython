import os
import sys
import conftest

def test_sampleBeamHexa1():

    # サンプルファイルを読む
    fileName = os.path.abspath(os.path.dirname(os.path.abspath(__file__)) 
                               + '/examples/beam/sampleBeamHexa1.fem')
    f = open(fileName, encoding="utf-8")
    s = f.read()  
    f.close()


    # main 関数を呼ぶ
    from main import initModel
    m = initModel(s)

    assert True

    
if __name__ == "__main__":
    test_sampleBeamHexa1()
