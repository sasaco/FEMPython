Docker Compose では、docker run時に`-it`オプションをつける代わりに、docker-compose.yml ファイルに、`tty: true` と `stdin_open: true` を書きます。
`tty: true` はdocker run時の`-t`オプションにあたいするもので、 `stdin_open: true`はdocker run時の`-i`オプションにあたいするものです。
つまり、Docker Composeを使う場合でも、指定方法が異なるだけで、やっていることは同じで、コンテナとホストマシンの標準入出力をつないでいます。

```
version: '3'
services:
  py:
    image: python:3.7
    tty: true
    stdin_open: true
```


[FrontISTR v5.0 を ubuntu 18.04 LTS(等)でcmakeを使って動かす手順](https://qiita.com/sakurano/items/1dc321a9b9d7fd035396) より 

**システム共通バイナリとしてインストール を行った**

# docker 操作

## ベースイメージの 取得

```
docker pull python:buster
```

#  ベースイメージに対して行ったこと

## パッケージインストール

ビルド環境及び並列計算の構築

```
apt install build-essential cmake gfortran git curl ruby libopenmpi-dev 
```

## オプショナルのライブラリ

```
apt install libmetis5 libopenblas-dev libmumps-dev libmetis-dev 
apt install trilinos-all-dev libptscotch-dev
```

## FrontISTRをコンパイルとインストール

### システム共通バイナリとしてインストール

```
git clone https://github.com/FrontISTR/FrontISTR
cd FrontISTR/; mkdir build; cd build
cmake ../
make -j 16
su
make install 
```