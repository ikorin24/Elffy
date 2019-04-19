# 開発メモ

## 既知のバグ

- DirectInputでの右スティックの入力がおかしい。

とりあえずXInputにすれば動く模様。
GamePadクラスじゃなくてもう一つの方のクラスに変えてみれば生けるかも？(未検証)

- Positionable.MultiplyScaleが正しく動いていない
  