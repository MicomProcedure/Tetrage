namespace Tetrage.Audio
{
    /// <summary>
    /// Game全体で使用する AudioClip の識別子。
    /// </summary>
    public enum SEClipId
    {
        GameStart = 0,
        CardMove = 1,
        CardFlip = 2,
        ButtonClick = 3,
    }

    public enum BgmClipId
    {
        Normal = 0,
        AfterReach = 1,
        ScanPhase = 2,
    }

    public enum JingleClipId
    {
        Lose = 0,
        Win = 1,

    }
}
