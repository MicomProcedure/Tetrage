using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GamePhase currentPhase;

    /// <summary>
    /// 現在のフェーズを外部から参照するためのプロパティ（読み取り専用）
    /// </summary>
    public GamePhase CurrentPhase
    {
        get { return currentPhase; }
        set { currentPhase = value; }
    }


    void StartGame()
    {

    }

    void StartRound()
    {

    }

    void EndRound()
    {

    }

    void TransitionPhase()
    {

    }

    void DisplayResults()
    {

    }

    void RestartOrQuit()
    {

    }
}