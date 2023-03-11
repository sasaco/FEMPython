from calcrate import FrameCalc

# テストデータを読み込む
def readTestData(s: str) -> FrameCalc:

    calc = FrameCalc()

    mesh = calc.model.mesh
    bc = calc.model.bc

    lines = s.splitlines()

    for l in lines:
        columns = l.split()

        if len(columns) == 0:
            continue

        keyWord = columns[0].lower()

        # 材料データ
        if keyWord == "material" and len(columns) > 7:
            pass

        # シェルパラメータ
        elif keyWord == "shellparameter" and len(columns) > 2:
            pass

        # 梁パラメータ
        elif keyWord == "barparameter" and len(columns) > 4:
            pass

        # 局所座標系
        elif keyWord == "coordinates" and len(columns) > 10:
            pass

        # 節点
        elif keyWord == "node" and len(columns) > 4:
            pass

        # 要素
        elif keyWord == "bebarelement" and  len(columns) > 5:
            if len(columns) < 8:
                pass
            else:
                pass

        elif keyWord == "tbarelement" and  len(columns) > 5:
            if len(columns) < 8:
                pass
            else:
                pass

        elif keyWord == "trielement1" and  len(columns) > 6:
            pass
        
        elif keyWord == "quadelement1" and  len(columns) > 7:
            pass

        elif keyWord == "tetraelement1" and  len(columns) > 6:
            pass

        elif keyWord == "wedgeelement1" and  len(columns) > 8:
            pass

        elif keyWord == "hexaelement1" and  len(columns) > 10:
            pass

        elif keyWord == "hexaelement1wt" and  len(columns) > 10:
            pass

        elif keyWord == "tetraelement2" and  len(columns) > 12:
            pass

        elif keyWord == "wedgeelement2" and  len(columns) > 17:
            pass

        elif keyWord == "hexaelement2" and  len(columns) > 22:
            pass
        
        # 境界条件
        elif keyWord == "restraint" and  len(columns) > 7:
            pass

        elif keyWord == "load" and  len(columns) > 4:
            pass

        elif keyWord == "pressure" and  len(columns) > 3:
            pass

        elif keyWord == "temperature" and  len(columns) > 2:
            pass

        elif keyWord == "htc" and  len(columns) > 4:
            pass

        # 計算結果
        elif keyWord == "resulttype" and  len(columns) > 1:
            pass

    calc.model.init()

    return calc