# import numpy as np

# #--------------------------------------------------------------------#
# class Vector3:
#     def __init__(self,x=0,y=0,z=0):
#         self.x = float(x)
#         self.y = float(y)
#         self.z = float(z)
#         self.vec = np.array([self.x, self.y, self.z])

#     def set(self, x, y, z):
#         self.__init__(x,y,z)

#     def fromArray(self):
#         pass

#     def __add__(self,other):
#         # 足し算
#         x = self.x + other.x
#         y = self.y + other.y
#         z = self.z + other.z
#         return Vector3(x,y,z)

#     # 引き算
#     def sub(self,other):
#         return self.__sub__(other)

#     def __sub__(self,other):
#         x = self.x - other.x
#         y = self.y - other.y
#         z = self.z - other.z
#         return Vector3(x,y,z)

#     def cross(self,other):
#         # 外積
#         w = np.cross(self.vec, other.vec)
#         x = w[0]
#         y = w[1]
#         z = w[2]
#         return Vector3(x,y,z)

#     def crossVectors(self, a, b):
#         # Sets this vector to cross product of a and b.
#         w = np.cross(a.vec,  b.vec)
#         x = w[0]
#         y = w[1]
#         z = w[2]
#         self.set(x,y,z)

#     def normalize(self):
#         # 方向単位ベクトル
#         w = np.linalg.norm(self.vec, keepdims=True)
#         x = w[0]
#         y = w[1]
#         z = w[2]
#         return Vector3(x,y,z)

#     def dot(self,other):
#         w = np.dot(self.vec, other.vec)
#         x = w[0]
#         y = w[1]
#         z = w[2]
#         return Vector3(x,y,z)

#     def clone(self):
#         x = self.x
#         y = self.y
#         z = self.z
#         return Vector3(x,y,z)


