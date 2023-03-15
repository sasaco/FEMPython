import os, conftest

def test_sampleBeamHexa1():

    # サンプルファイルを読む
    fileName = os.path.abspath(os.path.dirname(os.path.abspath(__file__)) 
                               + '/examples/beam/sampleBeamHexa1.fem')
    f = open(fileName, encoding="utf-8")
    s = f.read()  
    f.close()

    # src 関数を呼ぶテスト
    from FileIO import readTestData
    calc = readTestData(s)
    result = calc.calculate()


    assert True

    
if __name__ == "__main__":
    test_sampleBeamHexa1()
