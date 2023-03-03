# 使用するcppファイルはSRCS追加
SRCS = module.cpp
SRCS += FemDataModel.cpp

SRCS += Element/BarElement/BarElement.cpp
SRCS += BarParameter.cpp
SRCS += BoundaryCondition.cpp
SRCS += CircleSection.cpp
SRCS += Coordinates.cpp
SRCS += EigenValue.cpp
SRCS += ElementBorderBound.cpp
SRCS += ElementManager.cpp
#SRCS += ElementManager.h
SRCS += FElement.cpp
SRCS += FENode.cpp
SRCS += FileIO.cpp
SRCS += HeatTransferBound.cpp
SRCS += HexaElement1.cpp
SRCS += Load.cpp
SRCS += Material.cpp
SRCS += MeshModel.cpp
SRCS += Nodes.cpp
SRCS += Pressure.cpp
SRCS += RectSection.cpp
SRCS += Restraint.cpp
SRCS += Result.cpp
SRCS += Section.cpp
SRCS += ShellElement.cpp
SRCS += ShellParameter.cpp
SRCS += SolidElement.cpp
SRCS += Solver.cpp
SRCS += Strain.cpp
SRCS += Stress.cpp
SRCS += SymmetricTensor3.cpp
SRCS += Temperature.cpp
SRCS += Vector3R.cpp

# cppを追加するときはここで追加していく
# SRCS += BoundaryCondition.cpp 
# SRCS += HeatTransferBound.cpp 
# SRCS += Load.cpp 
# SRCS += Pressure.cpp 
# SRCS += Restraint.cpp 
# SRCS += Temperature.cpp 
# SRCS += Vector3R.cpp 
# SRCS += Coordinates.cpp 
# SRCS += BarElement.cpp 
# SRCS += BEBarElement.cpp 
# SRCS += TBarElement.cpp 
# SRCS += FElement.cpp 
# SRCS += QuadElement1.cpp 
# SRCS += ShellElement.cpp 
# SRCS += TriElement1.cpp 
# SRCS += HexaElement1.cpp 
# SRCS += HexaElement1WT.cpp 
# SRCS += HexaElement2.cpp 
# SRCS += SolidElement.cpp 
# SRCS += TetraElement1.cpp 
# SRCS += TetraElement2.cpp 
# SRCS += WedgeElement1.cpp 
# SRCS += WedgeElement2.cpp 
# SRCS += FENode.cpp 
# SRCS += Nodes.cpp 
# SRCS += BarParameter.cpp 
# SRCS += Material.cpp 
# SRCS += ShellParameter.cpp 
# SRCS += matrix3.cpp 
# SRCS += numeric.cpp 
# SRCS += CircleSection.cpp 
# SRCS += RectSection.cpp 

INCLUDES=`python3 -m pybind11 --includes` -I$(HOME)/include/eigen3 -I$(HOME)/include -I.

core: $(SRCS)
	g++ -O3 -Wall -shared -std=c++20 -fPIC $(INCLUDES) -g -o core`python3-config --extension-suffix` -undefined dynamic_lookup $(SRCS)
# -g：デバッグ用のコンパイルを可能にするオプション。これを入れることでブレークポイントでのデバッグができる。
