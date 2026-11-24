# CLAUDE.md — Equilibrium

カードゲームのバランスを AI 対戦と統計で評価する基盤（public）。Unity は使わない。Unity のビューは後で別に作る。

- 設計の正本: `docs/design.md`。仕様を変えるときは先にここを直す
- 計画書: `docs/plan/`（v0.2。章ごとに分割、索引は `docs/plan/README.md`）
- ビルド: `dotnet build Equilibrium.slnx -nologo -v q -clp:ErrorsOnly`
- テスト: `dotnet run --project tests/Equilibrium.Smoke -c Release`（Phase 1 の回帰テスト）
- C# 規約は `~/.claude/rules/coding-principles.md`。`unity-csharp.md` の Unity 固有項目は対象外。命名・ブレース・`None = 0` enum・省略形禁止は適用する

## このリポジトリ固有の決まり

- **public。自社ゲームのルール・カードデータ・評価関数・実験結果を置かない。** それらは private 側に置き、private がこのリポジトリを参照する。逆向きの参照を作らない
- **`Equilibrium.Core` と `samples/ToyGame` は netstandard2.1 / C# 9 に固定する。** Unity が同じ DLL を読むため。ファイルスコープ namespace・`record struct`・`required` は使えない。全プロジェクトでブロック namespace に統一する
- **Core の中では Observable を使わない。** 1 試合は 1 スレッドで順に進む同期の関数にする（理由は `docs/design.md` の設計判断 3）。Rx.NET は実行側の進捗とジョブの通知に使う
- **乱数は必ず Core の乱数の型を通す。** `System.Random`・`Guid.NewGuid`・時刻を試合の中で使わない。再現できなくなる
- **AI には全体の状態を渡さない。** そのプレイヤーから見える情報の型だけを渡す

## 学びの記録

確率・統計・実験計画・シミュレーション・ゲーム AI で分かったことは `Dev-notes` に残す。

- 日々の記録: `Dev-notes/journal/YYYY/YYYY-MM-DD.md`
- 育った知見: `Dev-notes/notes/game-balance/` に 1 トピック 1 ファイル。最初の 1 本を書くときにカテゴリを作り、`notes/README.md` に足す
- 書き方は `Dev-notes/README.md` に従う。自社ゲームの仕様に触れる内容は Dev-notes（private）にだけ書く
