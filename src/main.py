import config, core

class FEMPython:

    def __init__(self):
        self.core = core
        self.model = self.core.Solver()
        self.model.clear()


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


