using System.Collections.Generic;
using Tetrage.Core.Ids;

namespace Tetrage.Core.DTO
{
    /// <summary>
    /// ディーラーによる効果（モデル変更）を表現するDTO群。副作用なしで返す。
    /// </summary>
    public struct CardMoveEffect
    {
        public CardId CardId;       // 対象カード
        public PileId FromPileId;   // 移動元
        public PileId ToPileId;     // 移動先
    }

    public struct VisibilityEffect
    {
        public CardId CardId;       // 対象カード
        public bool IsVisible;      // 変更後の可視状態
    }

    /// <summary>
    /// 山札や任意のパイルに対して、決定論シャッフル用のSeedを配布する効果。
    /// </summary>
    public struct PileShuffleSeedEffect
    {
        public PileId PileId;   // 対象パイル
        public int Seed;        // シャッフルSeed（Fisher–Yates 用）
    }

    public struct TurnOrderEffect
    {
        public PlayerId PlayerId;   // 対象プレイヤー
        public int Order;        // プレイヤー順
    }

    /// <summary>
    /// 初期セットアップや配布など、複数の効果を束ねるプラン。
    /// </summary>
    public struct DealerPlan
    {
        public IReadOnlyList<CardMoveEffect> Moves;          // カード移動の集合
        public IReadOnlyList<VisibilityEffect> Visibility;   // 可視変更の集合
        public IReadOnlyList<PileShuffleSeedEffect> ShuffleSeeds; // 決定論シャッフルSeedの配布
        public IReadOnlyList<TurnOrderEffect> TurnOrder; // ターン順
    }
}


