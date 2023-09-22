# frontistr-python
FrontISTR を クラウドで用いる

# [Lambda コンテナイメージの作成](https://docs.aws.amazon.com/ja_jp/lambda/latest/dg/images-create.html#images-create-from-alt)

1. docker build コマンドを使用して Docker イメージをビルドします。イメージの名前を入力します。次の例では、イメージに hello-world という名前を付けています。

```
docker build -t hello-world .    
```

2. Amazon ECR レジストリに対し、Docker CLI を認証します。

```
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 123456789012.dkr.ecr.us-east-1.amazonaws.com    
```

3. create-repository コマンドを使用して、Amazon ECR にリポジトリを作成します。

```
aws ecr create-repository --repository-name hello-world --image-scanning-configuration scanOnPush=true --image-tag-mutability MUTABLE
```

4. リポジトリ名と一致するタグをイメージに付け、docker push コマンドを使用してそのイメージを Amazon ECR にデプロイします。

```
docker tag  hello-world:latest 123456789012.dkr.ecr.us-east-1.amazonaws.com/hello-world:latest
docker push 123456789012.dkr.ecr.us-east-1.amazonaws.com/hello-world:latest     
```