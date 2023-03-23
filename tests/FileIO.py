from main import FEMPython

# テストデータを読み込む
def readTestData(s: str) -> FEMPython:

    calc = FEMPython()
    model = calc.model

    materials = []
    nodes = [] 
    elements = []
    shellParams = []
    barParams = []
    coordinates = []
    pressures = []
    temperature = []
    htcs = []
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
        elif keyWord == "bebarelement" and  columns_size > 5 \
          or keyWord == "tbarelement" and  columns_size > 5 \
          or keyWord == "trielement1" and  columns_size > 6 \
          or keyWord == "quadelement1" and  columns_size > 7 \
          or keyWord == "tetraelement1" and  columns_size > 6 \
          or keyWord == "wedgeelement1" and  columns_size > 8 \
          or keyWord == "hexaelement1" and  columns_size > 10 \
          or keyWord == "hexaelement1wt" and  columns_size > 10 \
          or keyWord == "tetraelement2" and  columns_size > 12 \
          or keyWord == "wedgeelement2" and  columns_size > 17 \
          or keyWord == "hexaelement2" and  columns_size > 22:
            elements.append(calc.core.ElementManager(keyWord, columns))
        
        # 境界条件
        elif keyWord == "restraint" and  columns_size > 7:
            rest = calc.core.Restraint(columns)
            restraints.append(rest)

        elif keyWord == "load" and  columns_size > 4:
            load = calc.core.Load(columns)
            loads.append(load)

        elif keyWord == "pressure" and  columns_size > 3:
            press = calc.core.Pressure(int(columns[1]), columns[2], float(columns[3]))
            pressures.append(press)

        elif keyWord == "temperature" and  columns_size > 2:
            temp = calc.core.Temperature(int(columns[1]), float(columns[2]))
            temperature.append(temp)

        elif keyWord == "htc" and  columns_size > 4:
            htc = calc.core.HeatTransferBound(int(columns[1]), columns[2],
                    float(columns[3]), float(columns[4]))
            htcs.append(htc)

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
    #model.bc.pressures = pressures
    #model.bc.temperature = temperature
    #model.bc.htcs = htcs

    model.init()

    return calc