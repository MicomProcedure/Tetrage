namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ネットワーク境界で扱うカード状態種別コード
    /// </summary>
    public enum CardStateCode : byte
    {
        FaceUp = 1,
        IsSuitVisible = 2,
        IsHighlighted = 3,
    }

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
        StartScanPhase = 9,
        EndScanPhase = 10,
        FinishingGame = 11,
        ActionRequested = 20,
        ActionResult = 21,
        PileShuffledWithSeed = 22,
        ListOrderDeclared = 23,
        ScanTargetSelected = 24,
        ScanResult = 25,
    }
}


