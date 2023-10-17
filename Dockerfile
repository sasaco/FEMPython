# 使用するubuntuのバージョンを指定
FROM registry.gitlab.com/frontistr-commons/frontistr/runtime:mkl

# FrontISTRをコンパイルするのに必要なツールやパッケージをインストール
RUN apt update && \
    apt -y upgrade

# Pythonのインストール
RUN apt install -y python3 python3-pip
# RUN pip3 install -r requirements.txt
