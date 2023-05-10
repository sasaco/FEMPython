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
    _type = result.type
    _ELEMENT_DATA = result.ELEMENT_DATA
    _NODE_DATA = result.NODE_DATA
    _displacement = result.displacement
    _dispMax = result.dispMax
    _angleMax = result.angleMax
    _strain1 = result.strain1
    _stress1 = result.stress1
    _sEnergy1 = result.sEnergy1
    _strain2 = result.strain2
    _stress2 = result.stress2
    _sEnergy2 = result.sEnergy2
    _temperature = result.temperature
    _tempMax = result.tempMax
    _calculated = result.calculated



    assert True

    
if __name__ == "__main__":
    test_sampleBeamHexa1()
