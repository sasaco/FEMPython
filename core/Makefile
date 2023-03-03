# ソースコードディレクトリ指定
SOURCEDIR = .
# 1. サブディレクトリを含むディレクトリリストの生成
SRCDIRLIST := $(shell find $(SOURCEDIR) -type d)
# 2. 全てのcppファイルのリストの生成
SRCLIST = $(foreach srcdir, $(SRCDIRLIST), $(wildcard $(srcdir)/*.cpp))

# インクルードディレクトリ指定
INCLUDES = `python3 -m pybind11 --includes` -I. -isystem ../packages/eigen
INCLUDES += $(addprefix -I, $(SRCDIRLIST))

core: $(SRCLIST)
	g++ -O3 -Wall -shared -std=c++20 -fPIC $(INCLUDES) -g -o core`python3-config --extension-suffix` $(SRCLIST)
# -g：デバッグ用のコンパイルを可能にするオプション。これを入れることでブレークポイントでのデバッグができる。