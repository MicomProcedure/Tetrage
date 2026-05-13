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
    public struct GameStartedEventPacket
    {
        public int deckId;
        public byte[] suitOrder;
        public int minNumber;
        public int maxNumber;
        public int[] playerActorNumbers; // ターン順/座席順
    }

    [Serializable]
    public struct TurnStartedEventPacket
    {
        public int sequence;
        public int stateVersion;
        public int currentPlayerActorNumber;
    }

    [Serializable]
    public struct TurnEndedEventPacket
    {
        public int sequence;
        public int stateVersion;
        public int previousPlayerActorNumber;
    }


    [Serializable]
    public struct ListOrderDeclaredEventPacket
    {
        public int sequence;
        public int stateVersion;
        /// <summary>並べ替え対象のID種別（IIdentifiableのIdに対応）</summary>
        public ListOrderIdKind idKind;
        public ListOrderKey listKey; // 用途固定の列挙型
        public int[] orderedIds; // 並び順（IDラッパの実体はint。PlayerId/CardId/PileId等）
    }

    [Serializable]
    public struct CardMovedEventPacket
    {
        public int sequence;
        public int stateVersion;
        public int cardId;
        public int fromPileId;
        public int toPileId;
    }

    [Serializable]
    public struct CardStateChangedEventPacket
    {
        public int sequence;
        public int stateVersion;
        public int cardId;
        public CardStateCode stateCode;
        public bool stateValue;
    }

    [Serializable]
    public struct StartScanPhaseEventPacket
    {
        public int sequence;
        public int stateVersion;
        public int userPlayerActorNumber;
        public int[] playerActorNumbers;
    }

    [Serializable]
    public struct EndScanPhaseEventPacket
    {
        public int sequence;
        public int stateVersion;
    }

    [Serializable]
    public struct ScanTargetSelectedEventPacket
    {
        public int sequence;
        public int actorPlayerId; // ActorNumber
        public int selectedTargetActorNumber;
    }

    [Serializable]
    public struct ScanResultEventPacket
    {
        public int sequence;
        public int targetActorNumber;
        public int targetSuit;
    }

    [Serializable]
    public struct FinishingGameEventPacket
    {
        public int sequence;
        public int stateVersion;
        public int[] winnerActorNumbers; // -1 なら未定/引き分け等
    }

    [Serializable]
    public struct GameEndedEventPacket
    {
        public int sequence;
        public int stateVersion;
        public int[] winnerActorNumbers; // -1 なら未定/引き分け等
    }

    [Serializable]
    public struct PileShuffledWithSeedPacket
    {
        public int sequence;
        public int stateVersion;
        public int pileId;
        public int seed;
    }

    [Serializable]
    public struct ActionRequestedEventPacket
    {
        public int sequence;
        public int clientSequence; // クライアント側の識別用
        public int actorPlayerId; // ActorNumber（ネットワーク境界はActorNumberで統一）
        public ActionType actionType;
        public int[] targetCardIds; // 単数 or 複数対象（CardId.Valueの配列）
        public int actionStatusInt; // 追加ステータス（結果の分岐判定用）
    }

    [Serializable]
    public struct ActionResultEventPacket
    {
        public int sequence;
        public int clientSequence; // エコーバック（関連付け）
        public int actorPlayerId; // ActorNumber（ネットワーク境界はActorNumberで統一）
        public ActionType actionType;
        public bool accepted;      // 成否
        public string reason;      // 失敗時
        public int[] targetCardIds; // 影響対象（応答時に確定させたい場合）
        public int actionStatusInt; // 追加ステータス（結果の分岐判定用）
    }

    /// <summary>
    /// ActionResult.AdditionalData に格納し、NetworkActionBase で取り出して送信するためのDTO
    /// </summary>
    [Serializable]
    public struct ActionRequestDescriptorPacket
    {
        public ActionType actionType;
        public int actorPlayerId; // PlayerId.Value（ドメイン内部の識別値）
        public CardId[] targetCardIds;
        public int actionStatusInt; // 追加ステータス（結果の分岐判定用）
    }
}


