# 使用するubuntuのバージョンを指定
FROM ubuntu:18.04
FROM registry.gitlab.com/frontistr-commons/frontistr/runtime:mkl

# Pythonのインストール
RUN apt install -y python3 python3-pip
# RUN pip3 install -r requirements.txt
