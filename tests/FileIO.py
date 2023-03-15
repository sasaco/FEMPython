from calcrate import FrameCalc

# テストデータを読み込む
def readTestData(s: str) -> FrameCalc:

    calc = FrameCalc()
    model = calc.model

    mesh = model.mesh
    bc = model.bc

    materials = []
    nodes = [] 
    elements = []
    shellParams = []
    barParams = []
    coordinates = []
    restraints = []
    loads = []

    lines = s.splitlines()

    for l in lines:

        columns = l.split()

        columns_size = len(columns)
        if columns_size == 0:
            continue

        keyWord = columns[0].lower()

        # 材料データ
        if keyWord == "material" and columns_size > 7:
            mat = calc.core.Material(int(columns[1]), float(columns[2]),
                float(columns[3]), float(columns[5]),
                float(columns[6]), float(columns[7]))
            materials.append(mat)

        # シェルパラメータ
        elif keyWord == "shellparameter" and columns_size > 2:
            shell = calc.core.ShellParameter(int(columns[1]), float(columns[2]))
            shellParams.append(shell)

        # 梁パラメータ
        elif keyWord == "barparameter" and columns_size > 4:
            bar = calc.core.BarParameter(int(columns[1]), columns[2], columns[3:])
            barParams.append(bar)

        # 局所座標系
        elif keyWord == "coordinates" and columns_size > 10:
            cord = calc.core.readCoordinates(columns)
            coordinates.append(cord)

        # 節点
        elif keyWord == "node" and columns_size > 4:
            node = calc.core.FENode(int(columns[1]), float(columns[2]),
                float(columns[3]),
                float(columns[4]))
            nodes.append(node)
        
        # 要素
        elif keyWord == "bebarelement" and  columns_size > 5
            or:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "tbarelement" and  columns_size > 5:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "trielement1" and  columns_size > 6:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "quadelement1" and  columns_size > 7:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "tetraelement1" and  columns_size > 6:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "wedgeelement1" and  columns_size > 8:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "hexaelement1" and  columns_size > 10:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "hexaelement1wt" and  columns_size > 10:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "tetraelement2" and  columns_size > 12:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "wedgeelement2" and  columns_size > 17:
            elements.append(calc.core.ElementManager(keyWord, columns))
        elif keyWord == "hexaelement2" and  columns_size > 22:
            elements.append(calc.core.ElementManager(keyWord, columns))
        
        # 境界条件
        elif keyWord == "restraint" and  columns_size > 7:
            rest = calc.core.Restraint(columns)
            restraints.append(rest)

        elif keyWord == "load" and  columns_size > 4:
            load = calc.core.Load(columns)
            loads.append(load)

        elif keyWord == "pressure" and  columns_size > 3:
            pass

        elif keyWord == "temperature" and  columns_size > 2:
            pass

        elif keyWord == "htc" and  columns_size > 4:
            pass

        # 計算結果
        elif keyWord == "resulttype" and  columns_size > 1:
            keyWord1 = columns[1].lower()
            if keyWord1 == "element":
                model.result.type = model.result.ELEMENT_DATA
            else:
                model.result.type = model.result.NODE_DATA

    model.materials = materials
    # model.shellParams = shellParams
    # model.barParams = barParams
    # model.coordinates = coordinates

    model.mesh.nodes = nodes
    model.mesh.elements = elements
    model.bc.restraints = restraints
    model.bc.loads = loads

    model.init()

    return calc