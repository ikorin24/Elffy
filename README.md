# Elffy

## ***Now Developing !!! Coming Soon...***

## 概要

OpenGL ベースの C# 製の ゲームエンジン

現在開発中ですので、容易に実装やAPIは変更されます。

.NET5 + OpenGL のため、理論上はクロスプラットフォームで動作しますが未確認です。

## ディレクトリ構造

`src/Elffy.sln` : 全体のソリューション

`src/Elffy/Elffy.csproj` : ゲームエンジン本体のプロジェクト (dll)

`src/Sandbox/Sandbox.csproj` : ゲームエンジンを実際に使った動作確認サンプル (実行可能バイナリ)

`src/`以下にその他関連プロジェクト、関連ファイル

ビルド及び実行は Windows10, Visual Studio 2019 からしか確認していません。

## サンプル

上記の`Sandbox.csproj`をビルドして実行してください。

```sh
$ git clone https://github.com/ikorin24/Elffy.git
$ dotnet run -c Release -p Elffy/src/Sandbox/Sandbox.csproj
```

## Other licensed products

See [CREDITS](https://github.com/ikorin24/Elffy/blob/master/CREDITS.md)
