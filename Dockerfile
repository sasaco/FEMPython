# 使用するubuntuのバージョンを指定
FROM ubuntu:18.04

# FrontISTRをコンパイルするのに必要なツールやパッケージをインストール
RUN apt update && \
    apt -y upgrade && \
    apt install -y build-essential cmake gfortran git curl ruby libopenmpi-dev && \
    apt install -y unzip vim wget sudo

# ライブラリのインストール
RUN apt install -y libmetis5 libopenblas-dev libmumps-dev libmetis-dev && \
    apt install -y trilinos-all-dev libptscotch-dev

# Pythonのインストール
RUN apt install -y python3 python3-pip
# RUN pip3 install -r requirements.txt

# FrontISTRのリポジトリをクローン
# WORKDIR /root/
# RUN git clone  --recursive https://github.com/sasaco/FEMPython

# Pythonモジュールのインストール
# RUN pip3 install -r /root/FEMPython/requirements.txt
