using System;
using Tetrage.Core.Enums;

namespace Tetrage.Network.Gameplay
{
    [Serializable]
    public struct GameStartedEvent
    {
        public int deckId;
        public byte[] suitOrder;
        public int minNumber;
        public int maxNumber;
        public int[] playerActorNumbers;
    }

    [Serializable]
    public struct TurnStartedEvent
    {
        public int sequence;
        public int stateVersion;
        public int currentPlayerActorNumber;
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
    public struct ActionRequestedEvent
    {
        public int sequence;
        public int actorPlayerId;
        public ActionType actionType;
        public CardId[] targetCardIds; // 単数 or 複数対象
    }

    [Serializable]
    public struct ActionResultEvent
    {
        public int sequence;
        public int actorPlayerId;
        public ActionType actionType;
        public bool accepted;      // 成否
        public string reason;      // 失敗時
        public CardId[] targetCardIds; // 影響対象（応答時に確定させたい場合）
    }
}


