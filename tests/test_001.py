import os, conftest
from FileIO import readTestData

def test_sampleBeamHexa1():

    # サンプルファイルを読む
    filePath = os.path.dirname(os.path.abspath(__file__)) 
    fileName = filePath + '/examples/beam/sampleBeamHexa1.fem'
    with open(fileName, encoding="utf-8") as f:
        s = f.read()  

    # src 関数を呼ぶテスト
    fem = readTestData(s)
    result = fem.calculate()
    ed = result.ELEMENT_DATA
    vtk: str = fem.vtk()
    

    assert True

    
if __name__ == "__main__":
    test_sampleBeamHexa1()
