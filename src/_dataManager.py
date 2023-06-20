import _config, core

class _dataManager:

    def __init__(self):


        ## core 
        self._materials = []
        self.nodes = {} 
        self._elements = []
        self._shellParams = []
        self._barParams = []
        self._coordinates = []
        self._pressures = []
        self._temperature = []
        self._htcs = []
        self._restraints = []
        self._loads = []

    # ß“_
    ## ß“_‚ğ’Ç‰Á‚·‚é
    def addNode(self, ID: str, x: float, y: float, z: float):
        key = str(ID)
        self.nodes[key] = core.FENode(key, float(x), float(y), float(z))
    

    # —v‘f‚ğ’Ç‰Á‚·‚é
    def addElement(self, ID, ni,  nj, e, cg, xi, yi, zi, xj, yj, zj):
        columns = []
        columns.append(ID)
        columns.append(str(ni))
        columns.append(str(nj))
        columns.append(str(e))
        columns.append(str(cg))
        columns.append(str(xi))
        columns.append(str(yi))
        columns.append(str(zi))
        columns.append(str(xj))
        columns.append(str(yj))
        columns.append(str(zj))
        element = core.ElementManager("barelement", columns)

        self._elements.append(element)






