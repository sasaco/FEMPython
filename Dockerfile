FROM ubuntu:20.04

ENV DEBIAN_FRONTEND=noninteractive

ENV TZ Asia/Tokyo

RUN apt -y update \
    && apt -y upgrade \
    && apt-get install -y \
    git

# Build tools
RUN apt -y install \
    sudo \
    wget \
    tzdata \
    g++ \
    clang \
    cmake \
    make \
    build-essential \
    w3m \
    less \
    nkf \
    diffutils \
    patch \
    zlib1g-dev \
    unzip locales \
    && locale-gen ja_JP.UTF-8


# Eigen 3.1.0
## https://gitlab.com/libeigen/eigen/-/releases
RUN	mkdir -p /home/eigen_ws &&\
	cd /home/eigen_ws &&\
	wget https://gitlab.com/libeigen/eigen/-/archive/3.1.0/eigen-3.1.0.zip &&\
	unzip eigen-3.1.0.zip &&\
	cd eigen-3.1.0 && \
	mkdir build &&\
	cd build &&\
	cmake .. &&\
	make -j $(nproc --all) &&\
	make install


# Python:
RUN sudo apt install -y \
    python3-dev \
    python3-pip \
    python3-ipython \
    ipython3 \
    python3-tk \
    python3-numpy \
    && pip3 install pybind11


# 起点となるディレクトリ（execコマンドでコンテナに入った際のディレクトリ）
WORKDIR /home
