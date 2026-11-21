# 技術構成・実行モード・データ構造

## 13. シミュレーターの技術構成

### 13.1 基本アーキテクチャ

初期構成は以下を想定する。

1. Game Core
2. AI Engine
3. Simulation Runner
4. Balance Analyzer
5. Result Storage
6. Dashboard / Replay Viewer

### 13.2 Game Core

ゲームルールを管理する。

主な責務：

- ターン管理
- ゲーム状態管理
- 合法手判定
- カード効果解決
- ランダム処理
- 勝敗判定

Unityの描画処理から独立させる。

### 13.3 AI Engine

AIの行動選択を管理する。

初期段階ではルールベースAIを実装する。

将来的には以下を検討する。

- MCTS
- 不完全情報ゲーム向け探索
- 遺伝的アルゴリズム
- 強化学習
- ニューラルネットワーク

### 13.4 Simulation Runner

大量の試合を実行する。

主な機能：

- 複数試合の並列実行
- 乱数シード管理
- ジョブ管理
- 実験条件管理
- 結果集計
- 実行速度計測

### 13.5 Balance Analyzer

対戦結果から以下を算出する。

- カード評価
- デッキ評価
- シナジー
- キーカード依存率
- レアリティ・経済性
- Tier表
- 対戦環境分析

### 13.6 Dashboard

評価結果をグラフィカルに表示する。

主な画面：

- カード性能一覧
- デッキTier表
- カードTier表
- デッキ相性マトリクス
- キーカード依存度
- 勝率推移
- 予算別Tier
- バランス変更前後の比較
- 実験履歴

## 14. 実行モード

### 14.1 Visual Mode

Unityを使用して試合を表示する。

用途：

- AI同士の対戦観察
- AIの判断の確認
- バグ調査
- ランダム効果の確認
- リプレイ再生

### 14.2 Headless Mode

描画を行わず、ゲームロジックのみで対戦を実行する。

用途：

- 大量対戦
- 勝率集計
- デッキ探索
- カード差し替え評価
- 統計実験

初期段階ではCPU並列処理を採用する。

### 14.3 GPU Compute Mode

将来の高速化研究として検討する。

候補：

- Windows：CUDA
- Mac：Metal
- Unity：Compute Shader

利用候補：

- 大量の盤面評価
- 確率計算
- ニューラルネットワーク推論
- 機械学習

複雑なゲームルール全体をGPUで実行するかは、実測結果に基づいて判断する。

## 15. 使用技術

| 分野 | 初期採用候補 |
|---|---|
| ゲームエンジン | Unity |
| 正規ゲームロジック | C# / .NET |
| 対戦AI | C# |
| 高速シミュレーション | C# / CPU並列 |
| 統計解析 | Python |
| 統計ライブラリ | SciPy |
| データ処理 | pandas |
| 結果保存 | SQLite / Parquet |
| グラフィカル表示 | Unity |
| GPU高速化 | CUDA / Metal（将来） |
| 別言語での高速化 | Rust / C++（必要時） |

ゲームロジックを最初から複数言語で重複実装しない。

高速化の前にプロファイリングを実施し、ボトルネックを特定する。

## 16. データ構造

### 16.1 SimulationRun

実験条件を管理する。

- RunId
- GameVersion
- CardDataVersion
- AIVersion
- EnvironmentId
- SeedConfiguration
- EvaluationDefinitionVersion

### 16.2 GameResult

試合結果を保持する。

- GameId
- RunId
- PlayerDeckIds
- StartingPlayer
- Winner
- TurnCount
- EndReason
- RandomSeedIds

### 16.3 CardUsage

カードの使用情報。

- GameId
- PlayerId
- CardId
- CardInstanceId
- AcquisitionMethod
- FirstAcquiredTurn
- FirstPlayedTurn
- UseCount

### 16.4 TurnState

ターン単位の分析情報。

- GameId
- TurnNumber
- PlayerId
- HandSize
- AvailableResources
- LegalActionCount
- PlayedCardCount
- BoardStateSummary

### 16.5 ReplayEvent

試合の再現・調査用。

- EventSequence
- GameStateReference
- ActionId
- SourceCardId
- TargetIds
- RandomStreamId
- RandomDrawIndex
- Result

### 16.6 ログ保存方針

大量対戦では詳細ログをすべて保存するとストレージ負荷が大きくなる。

そのため、以下を分離する。

- 軽量な試合結果
- 評価用の統計データ
- 詳細な行動ログ
- 再現用の乱数・初期状態
- デバッグ用リプレイデータ

通常は軽量ログを保存し、異常な試合や調査対象試合のみ詳細ログを保存する。
