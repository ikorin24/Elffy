# Elffy

## ***Now Developing !!! Coming Soon...***

## 概要

OpenGL ベースの C# 製の ゲームエンジン

現在開発中ですので、容易に実装やAPIは変更されます。

.NET Core 3.1 + OpenGL のため、理論上はクロスプラットフォームで動作しますが未確認です。

## ディレクトリ構造

`src/Elffy/Elffy.sln` : 全体のソリューション

`src/Elffy/Elffy/Elffy.csproj` : ゲームエンジン本体のプロジェクト (dll)

`src/Elffy/Sandbox/Sandbox.csproj` : ゲームエンジンを実際に使った動作確認サンプル (WinExe)

`src/Elffy/`以下にその他関連プロジェクト、関連ファイル

ビルド及び実行は Windows10, Visual Studio 2019 からしか確認していません。

## サンプル

上記の`Sandbox.csproj`をビルドして実行してください。

プロジェクト内の`Sandbox/externalExe/erc.exe`は`src/Elffy/ElffyResourceCompiler.csproj`をビルドしたもので、`Sandbox.csproj`のビルド時にゲームのリソースを zip に固めるためのものです。
