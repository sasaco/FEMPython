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
        import numpy as np
        from pyevtk.hl import imageToVTK

        ix = 128
        jx = 128
        kx = 128

        xmin = 0.
        xmax = 1.
        ymin = 0.
        ymax = 1.
        zmin = 0.
        zmax = 1.

        dx = (xmax-xmin)/ix
        dy = (ymax-ymin)/jx
        dz = (zmax-zmin)/kx

        x = xmin + 0.5*dx + np.arange(ix)*dx
        y = ymin + 0.5*dy + np.arange(jx)*dy
        z = zmin + 0.5*dz + np.arange(kx)*dz

        xx,yy,zz = np.meshgrid(x,y,z)

        ds = 0.1

        x0 = (xmax+xmin)/2.
        y0 = (ymax+ymin)/2.
        z0 = (zmax+zmin)/2.

        amp = 1.0

        rr = (xx-x0)**2+(yy-y0)**2+(zz-z0)**2
        qq = amp*np.exp(-rr/(2.*ds**2))

        imageToVTK('./test',pointData={'quantity':qq})

        return "OKS"

