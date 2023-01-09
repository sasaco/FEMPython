FROM ubuntu:20.04

RUN apt -y update \
    && apt -y upgrade 

RUN apt -y install \
    python3-dev \
    python3-pip \
    vim \
    git \
    g++ \
    clang \
    cmake \
    make \
    w3m \
    less \
    python3-ipython \
    ipython3 \
    nkf \
    less \
    diffutils \
    patch \
    sudo \
    zlib1g-dev \
    unzip locales \
    && locale-gen ja_JP.UTF-8 \
    && pip3 install pybind11

# 起点となるディレクトリ（execコマンドでコンテナに入った際のディレクトリ）
WORKDIR /home

# 本プロジェクトのソースファイルをコピー
COPY . /home

