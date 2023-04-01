import config, core

from dataManager import _dataManager

class FEMPython(_dataManager):

    def __init__(self):
        self._model = core.Solver()
        self._model.clear()


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
        self.model.calculate()
        return self.model.result
    


    def vtk(self):
        import pyvtk 
        import numpy as np

        XYZ = np.array([np.array([node.x, node.y, node.z]) for node in self.model.mesh.nodes])
        polygon = [elem.nodes() for elem in self.model.mesh.elements]
        structure = pyvtk.PolyData(points=XYZ, polygons=polygon) 


        pointdata = pyvtk.PointData(pyvtk.Scalars(XYZ[:,2], name='point-scalar', lookup_table='default')) 
        cellvalue = [np.mean([XYZ[node, 2]  for node in nodelist], dtype=np.float32) for nodelist in polygon] 
        celldata = pyvtk.CellData(pyvtk.Scalars(cellvalue, name='cell-scalar', lookup_table="default")) 

        vtk = pyvtk.VtkData( structure, "# test", celldata, pointdata) 

        return vtk.to_string().decode('utf-8')


