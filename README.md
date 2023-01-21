# pybind11を使ってpythonで利用可能なc++とeigenライブラリの開発環境をDockerで作ってほしい
pybind11を使ってc++の共有ライブラリを作成し、Pythonから実行できる環境を提供する。  
開発においてはVSCodeからPython, c++それぞれブレークポイントを指定してデバッグをする事ができる。

# Requirement
Windowsローカルに下記アプリケーションがインストールされている事
+ WSL
+ Docker Desktop
+ VSCode
  

VSCodeの拡張機能DevContainersがインストールされている事<br>
<img src="../images/996d3e84cad564f5a892b56590533432de657899685e8683d3a765ca07459435.png" width="40%">

# Installation

1. zipファイルをローカルに展開(下記フォルダパスは一例)
```
C:\FEMPython
```

2. WSLでDockerfileをbuild、runする
```
cd /mnt/c/FEMPython
docker build -t fem .
docker run -name fem -dit fem
```

3. VSCodeでRemoteログインする
+ VSCodeの左下の緑色のボタンをクリック<br>
<img src="../images/85ebb6babf8cf5b55f9620fda26f7a40d485f0f1b84416357d4de2bd429d634e.png" width="20%">
+ 出てきたメニューからAttach to Running Container...をクリック<br>
<img src="../images/862c2efd61bc650aa99391bcf1087352fbe4c3e9b4fb6a48ca9deebf6b3783dd.png" width="40%">
+ /fem を選択してコンテナ内にVSCodeで入る<br>
<img src="../images/e8bee52d8ba2d799a8795557e9402dc4093cd39318841eafe0ffa45d2101ec71.png" width="20%">
<br>
<br>
4. 拡張機能で必要な機能をDevContainerにインストール
+ Python
+ C/C++
+ C/C++ Runner
+ Makefile Tools<br>
<img src="../images/67ee7b930c82e0c800220efcd33066a5b6ba843eecd36e585ab8c89b55d5cda4.png" width="30%">

# Usage
### デバッグ方法
1. /FEMPython/tests/test_001.py を開く
1. 実行とデバッグをクリック
1. Pythonもしくはc++を選択してデバッグ実行(F5を押した場合は直近選ばれていたデバッガが選択される)
1. 指定したデバッガに対応するPythonコードもしくはc++コード内のブレークポイントでコード実行を一時停止できる<br>
<img src="../images/134296c4294aaf063f84064bde0391c30b6d63240f97ed609d19237b2261f4b0.png" width="40%">

# Note
### 各ファイルの説明
+ Dockerfile
  + PythonとEigenをインストール
  + Pythonの仮想環境構築は今回見送り(複雑になるため)
  + Eigenはgit cloneのみ。コンパイル時に組み込むためbuildは不要
<br>
<br>
+ cppファイル
  + FileIO.cpp
    + readFemModelメソッドを定義
    + Eigenも読み込んでおり、プログラム内で使用可能(実際に呼び出しはしていない)
    + pybind11形式でreadFemModel関数とFemDataModelクラスを参照ファイル化する
  + FemDataModel.h FemDataModel.cpp
    + readFemModelのreturnとして返すクラスを定義。メソッドはかなり簡素化+private変数に文字列を設定、読み出せる関数を追加。
<br>
<br>
+ Makefile
  + SRCSにコンパイルする(#includeでつながっている)cppファイルを全て定義の上pybind11の共有ライブラリ形式(.so)でコンパイル
  + コンパイルのキーワードに-gが入っていることでデバッグが可能になる。
<br>
<br>
+ Pythonファイル
  + pybind11でコンパイルした参照ファイルを同じフォルダ内に配置することでimport FileIOでコンパイルしたメソッド、クラスを呼び出すことができる。
  + Pythonで読み込んだテキストをreadFemModelメソッドの引数に指定して、返り値となるFemDataModelクラスのインスタンスのprivate変数に格納して返している。
  + Python上ではFileIO.cppで指定したメソッド(インスタンス内変数の読み出しなど)を使用することができる。
<br>
<br>
+ /FEMPython/.vscode (デバッグ設定が記述されており重要)
  + launch.json
    + Pythonのデバッガにc++のデバッガを組み込む形になっている。
    + pybindで生成される共有ライブラリ形式(.so)ファイルは実行ファイルではないので通常のコンパイルはできない。
    + pythonとの混合デバッグモードにより、ブレークポイントを指定したデバッグが可能になる。
    + <b>pythonコードからしかデバッグできないので注意！</b>
  + tasks.json
    + デバッグを行う際に事前に行う処理などを記載しておける
    + ここでは下記の処理をlaunch.jsonの"preLaunchTask"(デバッグ前に実施する処理)に指定してある
      + 共有ライブラリ形式(.so)ファイルのコンパイル
      + /FEMPython/testsフォルダへのリネーム＆移動

### プログラム拡張時のメモ
+ cppファイルを共有ライブラリ(.so)に追加する際はMakefileのSRCSにファイル名を追加する事で、コンパイル、リンクができるようになる<br>
<img src="../images/439418f8b43890f1ade8099accd2018f83e1bdb1714291dbcb497bb586f55370.png" width="40%">
+ Python側で呼び出したいクラスがある場合は使用するメソッドを全てFileIO.cppファイルに書き込んでおく。ここに記載がないとc++側で定義していてもPython側で呼び出せない。<br>
<img src="../images/d5682ec7bec09c21fdcdc2e96b8e4f003c8edc89e21c60f66a7a7cd135f12d6d.png" width="40%">
+ 共有ライブラリ名はコンパイルするファイル名と同じにする必要がある(この場合はFileIO.cpp)
変更する場合はMakefileのFileIOの記載を変更してコンパイル<br>
<img src="../images/07166a9c471b8b61710f189a16e1fd16fd1b51814a45996139b8fd94541fba5a.png" width="80%">
+ 今回は仮想環境に直接インストールしたPythonを使用したが、仮想環境を作成する場合はMakefileにPythonパスの指定が必要(今回未検証)  
参考URL：https://qiita.com/exy81/items/e309df7e33d4ff20a91a<br>
<img src="../images/97ccf09be61b0bbe4d2ddffd2de34d0cb9fd275417e8721b3d005db3cf81b474.png" width="40%">


# Author
+ 作成者 : [Konihey](https://crowdworks.jp/public/employees/4822755)