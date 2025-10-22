using System;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ListOrder の用途を定義するキー
    /// </summary>
    public enum ListOrderKey
    {
        TurnOrder = 1,
        UiSeats = 2,
        // 必要に応じて追加: PileCardOrder 等は複合キーが必要なため別途
    }
    /// <summary>
    /// ListOrderDeclaredEvent で伝える ID の種類
    /// </summary>
    public enum ListOrderIdKind
    {
        Int = 0,
        PlayerId = 1,
        CardId = 2,
        PileId = 3,
        DeckId = 4,
    }

    [Serializable]
    public struct GameStartedEvent
    {
        public int deckId;
        public byte[] suitOrder;
        public int minNumber;
        public int maxNumber;
        public int[] playerActorNumbers; // ターン順/座席順
    }

    [Serializable]
    public struct TurnStartedEvent
    {
        public int sequence;
        public int stateVersion;
        public int currentPlayerActorNumber;
    }

    [Serializable]
    public struct TurnEndedEvent
    {
        public int sequence;
        public int stateVersion;
        public int previousPlayerActorNumber;
    }


    [Serializable]
    public struct ListOrderDeclaredEvent
    {
        public int sequence;
        public int stateVersion;
        /// <summary>並べ替え対象のID種別（IIdentifiableのIdに対応）</summary>
        public ListOrderIdKind idKind;
        public ListOrderKey listKey; // 用途固定の列挙型
        public int[] orderedIds; // 並び順（IDラッパの実体はint。PlayerId/CardId/PileId等）
    }

    [Serializable]
    public struct CardMovedEvent
    {
        public int sequence;
        public int stateVersion;
        public int cardId;
        public int fromPileId;
        public int toPileId;
    }

    [Serializable]
    public struct CardVisibilityChangedEvent
    {
        public int sequence;
        public int stateVersion;
        public int cardId;
        public bool isVisible;
    }

    [Serializable]
    public struct PileShuffledWithSeedEvent
    {
        public int sequence;
        public int stateVersion;
        public int pileId;
        public int seed;
    }

    [Serializable]
    public struct ActionRequestedEvent
    {
        public int sequence;
        public int clientSequence; // クライアント側の識別用
        public int actorPlayerId;
        public ActionType actionType;
        public CardId[] targetCardIds; // 単数 or 複数対象
    }

    [Serializable]
    public struct ActionResultEvent
    {
        public int sequence;
        public int clientSequence; // エコーバック（関連付け）
        public int actorPlayerId;
        public ActionType actionType;
        public bool accepted;      // 成否
        public string reason;      // 失敗時
        public CardId[] targetCardIds; // 影響対象（応答時に確定させたい場合）
    }
}


