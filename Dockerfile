FROM ubuntu:22.04

# タイムゾーン
RUN ln -sf /usr/share/zoneinfo/Asia/Tokyo /etc/localtime

# 対話モードOFF
ENV DEBIAN_FRONTEND=noninteractive

# Linux基本設定
RUN apt-get update
RUN apt-get install -y curl wget vim git unzip cmake clang libssl-dev build-essential

# Pythonのインストール(今回は仮想環境構築は未実施)
RUN apt-get install -y python3 python3-pip
RUN pip3 install -r requirements.txt

# Eigenのダウンロード（コンパイル時にincludeするためbuildは不要）
RUN git clone https://gitlab.com/libeigen/eigen.git -b 3.4

#fmtのダウンロード and セットアップ
RUN git clone https://github.com/fmtlib/fmt
RUN cd fmt
RUN mkdir build
RUN cd build
RUN cmake /fmt
RUN make && make install
