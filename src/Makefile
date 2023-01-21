# 使用するcppファイルはSRCS追加
SRCS = FileIO.cpp
SRCS += FemDataModel.cpp
# SRCS += AAA.cpp  # cppを追加するときはここで追加していく

FileIO: $(SRCS)
	g++ -O3 -Wall -shared -std=c++20 -fPIC `python3 -m pybind11 --includes` -g $(SRCS) -o FileIO`python3-config --extension-suffix` -isystem /eigen
# -g：デバッグ用のコンパイルを可能にするオプション。これを入れることでブレークポイントでのデバッグができる。