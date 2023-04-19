import config, core

from dataManager import _dataManager

class FEMPython(_dataManager):

    def __init__(self):
        self._model = core.Solver()
        self._model.clear()
        self.core = core


    def init(self):

        self._model.materials = self._materials
        self._model.shellParams = self._shellParams
        self._model.barParams = self._barParams
        self._model.coordinates = self._coordinates

        self._model.mesh.nodes = self._nodes
        self._model.mesh.elements = self._elements

        self._model.bc.restraints = self._restraints
        self._model.bc.loads = self._loads
        #self._model.bc.pressures = self._pressures
        self._model.bc.temperature = self._temperature
        #self._model.bc.htcs = self._htcs

        self._model.init()



    def calculate(self):
        self._model.calculate()
        return self._model.result
    


    def vtk(self):
        return "OKS"

