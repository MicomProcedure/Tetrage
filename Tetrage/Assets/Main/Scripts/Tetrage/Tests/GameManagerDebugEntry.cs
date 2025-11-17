using UnityEngine;
using Tetrage.Managers;
using Tetrage.Core.DTO;
using System.Collections.Generic;
using Tetrage.Core.Enums;
using Tetrage.Network.Gameplay;
public class GameManagerDebugEntry : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    private bool _isInitialized = false;

    void Awake()
    {
        _isInitialized = false;
    }

    void Start()
    {

        // GameManagerの初期化
        if (!_isInitialized)
        {
            _gameManager.Initialize(CreateDebugPlayerInfo(), CreateDebugPlayerInfo()[0], new PhotonNetworkContext());
            _isInitialized = true;
        }

    }

    [ContextMenu("StartGame")]
    async void StartGame()
    {
        if (!_isInitialized)
        {
            Debug.LogError("GameManagerが初期化されていません");
            return;
        }
        await _gameManager.StartGame();
    }

    [ContextMenu("StopGame")]
    void StopGame()
    {
        _gameManager.StopGame();
    }

    [ContextMenu("Reset")]
    void Reset()
    {
        _gameManager.Reset();
        _isInitialized = false;
    }

    List<PlayerInfo> CreateDebugPlayerInfo()
    {
        return new List<PlayerInfo>
        {
            new PlayerInfo { UserId = "Player 1", PlayerType = PlayerType.Local },
            new PlayerInfo { UserId = "Player 2", PlayerType = PlayerType.Local },
            new PlayerInfo { UserId = "Player 3", PlayerType = PlayerType.Local },
            new PlayerInfo { UserId = "Player 4", PlayerType = PlayerType.Local }
        };
    }

}
