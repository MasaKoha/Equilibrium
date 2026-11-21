# カードゲーム AI バランス評価システム 研究・開発計画書 v0.2

| 項目 | 内容 |
|---|---|
| プロジェクト種別 | ゲーム AI・シミュレーション・統計解析研究 |
| 開発対象 | 自社 1vs1 デジタルカードゲーム |
| 当面の目標 | 自社プロダクトにおける実運用 |
| 将来の展望 | 他社ゲームにも対応可能な汎用バランス評価ツールへの発展 |
| 文書バージョン | v0.2 |
| 現在の状態 | 研究要件・評価指標の初期設計完了 |
| 次の作業 | Phase 1 — 評価理論の検証と最小実験環境の実装 |

章番号は原文のまま。実装の境界と設計判断は[設計書](../design.md)にある。

| ファイル | 章 |
|---|---|
| [01-purpose-and-game.md](01-purpose-and-game.md) | 1 研究目的 / 2 対象ゲームの基本仕様 |
| [02-card-and-synergy-metrics.md](02-card-and-synergy-metrics.md) | 3 評価システムの全体像 / 4 基本変数 / 5 カード単体評価 / 6 カード間シナジー評価 |
| [03-deck-dependency-economy-metrics.md](03-deck-dependency-economy-metrics.md) | 7 デッキ全体評価 / 8 特定カード依存率 / 9 レアリティ・経済性評価 |
| [04-tier-meta-statistics.md](04-tier-meta-statistics.md) | 10 Tier 評価 / 11 対戦環境全体の評価 / 12 統計的信頼性 |
| [05-system.md](05-system.md) | 13 シミュレーターの技術構成 / 14 実行モード / 15 使用技術 / 16 データ構造 |
| [06-learning.md](06-learning.md) | 17 学習すべき情報工学の科目 / 18 学習ロードマップ |
| [07-roadmap.md](07-roadmap.md) | 19 開発ロードマップ / 20 初期 MVP / 21 最初の研究実験 |
| [08-principles.md](08-principles.md) | 22 研究上の注意点 / 23 将来的な研究拡張 / 24 基本原則 / 25 最終目標 |
