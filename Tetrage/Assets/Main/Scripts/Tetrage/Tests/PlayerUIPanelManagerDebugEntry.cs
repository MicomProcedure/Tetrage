using UnityEngine;
using Tetrage.UI;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using System.Collections.Generic;

namespace Tetrage.Tests
{
    /// <summary>
    /// PlayerUIPanelManagerの単独テスト用エントリーポイント
    /// Canvas内の固定位置に4人全員のパネルを常に表示
    /// </summary>
    public class PlayerUIPanelManagerDebugEntry : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerUIPanelManager _playerUIPanelManager;
        
        [Header("Settings")]
        [SerializeField] private bool autoSetupOnStart = true;
        
        private bool _isInitialized = false;

        void Awake()
        {
            _isInitialized = false;
        }

        void Start()
        {
            if (autoSetupOnStart)
            {
                SetupPanels();
            }
        }

        [ContextMenu("パネルをセットアップ")]
        void SetupPanels()
        {
            if (_playerUIPanelManager == null)
            {
                Debug.LogError("PlayerUIPanelManagerが設定されていません");
                return;
            }

            _playerUIPanelManager.SetupPanels(CreateDebugPlayerInfo());
            _isInitialized = true;
            Debug.Log("PlayerUIPanelManagerDebugEntry: 4人分のパネルをセットアップしました");
        }

        [ContextMenu("現在のプレイヤー: Player 0")]
        void SetCurrentPlayer0()
        {
            if (!CheckInitialized()) return;
            _playerUIPanelManager.SetCurrentPlayer(0);
        }

        [ContextMenu("現在のプレイヤー: Player 1")]
        void SetCurrentPlayer1()
        {
            if (!CheckInitialized()) return;
            _playerUIPanelManager.SetCurrentPlayer(1);
        }

        [ContextMenu("現在のプレイヤー: Player 2")]
        void SetCurrentPlayer2()
        {
            if (!CheckInitialized()) return;
            _playerUIPanelManager.SetCurrentPlayer(2);
        }

        [ContextMenu("現在のプレイヤー: Player 3")]
        void SetCurrentPlayer3()
        {
            if (!CheckInitialized()) return;
            _playerUIPanelManager.SetCurrentPlayer(3);
        }

        [ContextMenu("パネルをクリア")]
        void ClearPanels()
        {
            if (_playerUIPanelManager == null)
            {
                Debug.LogError("PlayerUIPanelManagerが設定されていません");
                return;
            }
            
            _playerUIPanelManager.ClearPanels();
            _isInitialized = false;
            Debug.Log("PlayerUIPanelManagerDebugEntry: パネルをクリアしました");
        }

        [ContextMenu("リセット（クリア→再セットアップ）")]
        void ResetPanels()
        {
            ClearPanels();
            SetupPanels();
        }

        /// <summary>
        /// テスト用のプレイヤー情報を生成（4人固定）
        /// </summary>
        List<PlayerInfo> CreateDebugPlayerInfo()
        {
            return new List<PlayerInfo>
            {
                new PlayerInfo { 
                    Id = new PlayerId(0),
                    UserId = "Mira.", 
                    PlayerType = PlayerType.Local,
                    PlayerIconIndex = 0
                },
                new PlayerInfo { 
                    Id = new PlayerId(1),
                    UserId = "Takakusaki", 
                    PlayerType = PlayerType.Local,
                    PlayerIconIndex = 1 
                },
                new PlayerInfo { 
                    Id = new PlayerId(2),
                    UserId = "Yuto77", 
                    PlayerType = PlayerType.Local,
                    PlayerIconIndex = 2
                },
                new PlayerInfo { 
                    Id = new PlayerId(3),
                    UserId = "283", 
                    PlayerType = PlayerType.Local,
                    PlayerIconIndex = 3
                }
            };
        }

        /// <summary>
        /// 初期化済みかチェック
        /// </summary>
        private bool CheckInitialized()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("PlayerUIPanelManagerDebugEntry: まずパネルをセットアップしてください");
                return false;
            }
            return true;
        }
    }
}