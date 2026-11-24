# Equilibrium

カードを 1 枚足すと、開発者が想定していない組み合わせが強くなることがある。
人のプレイテストだけでは、デッキとカードの組み合わせを数えきれない。

Equilibrium は、AI 同士の対戦を大量に実行し、勝率・相性・特定カードへの依存を
信頼区間つきで出すための評価基盤である。対象は 1vs1 のデジタルカードゲーム。

## 現在の状態

**Phase 1 の見本が動く。** ToyGame の固定デッキで、カード 1 枚の差し替え前後を
先攻・後攻に分けて比較できる。自社ゲームの評価ではない。製品用のログ保存・統計解析と
Unity のビューはまだ無い。

## このリポジトリに入れる範囲

ゲームに依存しない部分だけを置く。自社ゲームのルール・カードデータ・AI の評価関数は
別の private リポジトリに置き、そちらがこのリポジトリを参照する。逆向きの参照は作らない。

| 置き場所 | 入れるもの |
|---|---|
| このリポジトリ | ゲームの契約、乱数、対戦の実行、実験条件の記録、結果の保存、統計解析、見本ゲーム |
| private 側（公開範囲外） | 自社ゲームのルール実装、カードデータ、ルールベース AI、実験の設定、評価レポート |

## 構成

```
src/
  Equilibrium.Cli/          ToyGame の比較とリプレイを起動するコマンド（net10.0）
  Equilibrium.Core/         ゲームの契約・乱数・リプレイの型（netstandard2.1 / C# 9）
  Equilibrium.Simulation/   対戦の実行・並列・実験条件の記録（net10.0）
  Equilibrium.Storage/      結果の書き出し（net10.0）
samples/ToyGame/            契約を実装する見本ゲーム。テストと Phase 1 の対象
python/                     統計解析。C# が書いた結果ファイルを読むだけ
tests/Equilibrium.Smoke/      依存パッケージを増やさず実行できる回帰テスト
docs/                       計画書と設計書
```

## ビルド

.NET 10 SDK が要る。

```bash
dotnet build Equilibrium.slnx -nologo -v q -clp:ErrorsOnly
```

## 見本を動かす

```bash
dotnet build Equilibrium.slnx -c Release
dotnet run --project tests/Equilibrium.Smoke -c Release --no-build
mkdir -p runs
dotnet run --project src/Equilibrium.Cli -c Release --no-build -- compare 10000 2026 > runs/toy-comparison.json
dotnet run --project src/Equilibrium.Cli -c Release --no-build -- replay runs/toy-comparison.json 0
```

`compare` は「ブロック数、シード」の順。1 ブロックで元デッキと差し替えデッキを
先攻・後攻でそれぞれ 1 回ずつ、計 4 試合行う。JSON の `BaselineMinusReplacement`
は元デッキから差し替え後を引いた値（0.05 は 5 ポイント）。95% 区間は試行単位を独立と
みなした場合の保守的な区間で、AI や相手デッキを替えたときの差は説明しない。
先後・デッキ別の各集計には、負けの内訳 `LossesByLifeZero`（自分のライフが 0）
と `LossesByDeckEmpty`（自分が引けない）がある。勝ちも相手側の同じ理由で分ける。
これらの内訳は全試合を対象とし、詳細ログは従来どおり初回ブロックだけを保存する。
`replay` は初回ブロックの実対戦 0〜3 を再現し、保存済みイベント列と勝敗を照合する。

## 文書

- [設計書](docs/design.md) — 公開範囲の境界と、決めた設計判断・未決の項目
- [研究・開発計画書 v0.2](docs/plan/README.md) — 評価指標の定義、技術構成、ロードマップ
