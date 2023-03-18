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
    vtk: str = fem.vtk()

    # 計算結果を書き込む
    fileName = os.path.join(filePath, os.path.splitext(os.path.basename(fileName))[0]) + '.vtk'
    with open(fileName, 'w') as f:
        f.write(vtk)

    assert True

    
if __name__ == "__main__":
    test_sampleBeamHexa1()
