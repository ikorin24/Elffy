# Elffy

これは作者がゲームエンジンの勉強のために作った、実験的な自作ゲームエンジンです。
実用性を考慮していません。Pull Request や issue は受け付けていません。

C# (.NET9) + OpenGL4 です。
.NET9 がインストールされていれば、ビルドや実行に特にセットアップは必要ありません。

Sandbox.csproj を実行すればサンプルが動きます。Windows のみ。(2025年現在、Mac で OpenGL が正常に動くかは分かりません)

```
> cd src/Sandbox
> dotnet run -c Release
```

Forward Rendering, Deferred Rendering, Shadow Mapping, PBR, SSAO など

![scene-image](./img/image.gif)

## Other licensed products

See [NOTICE](https://github.com/ikorin24/Elffy/blob/master/NOTICE.md) file.
