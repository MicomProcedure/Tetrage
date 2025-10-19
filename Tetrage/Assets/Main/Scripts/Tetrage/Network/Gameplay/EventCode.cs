namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ネットワークイベントの種類（RaiseEvent の code）
    /// </summary>
    public enum EventCode : byte
    {
        GameStarted = 1,
        TurnStarted = 2,
        CardMoved = 3,
        TurnEnded = 4,
        GameEnded = 5,
        Snapshot = 6,
        CardVisibilityChanged = 7,
        ReachDeclared = 8,
        ActionRequested = 20,
        ActionResult = 21,
        PileShuffledWithSeed = 22,
        ListOrderDeclared = 23,
    }
}


