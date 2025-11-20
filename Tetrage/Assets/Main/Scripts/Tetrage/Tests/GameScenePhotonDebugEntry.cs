using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Tetrage.Managers;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tetrage.Models;
using Tetrage.Network.Gameplay;
using ExitGames.Client.Photon;
using Tetrage.Network;
using Tetrage.Tests.Data;

namespace Tetrage.Tests
{
    /// <summary>
    /// GameSceneのデバッグ用エントリーポイント
    /// MultiPlayModeで4人プレイをテストできるようにする
    /// </summary>
    public class GameScenPhotonDebugEntry : MonoBehaviourPunCallbacks
    {
        #region Serialized Fields

        [Header("Photon View")]
        [SerializeField] private PhotonView _photonView;

        [Header("Game Manager")]
        [SerializeField] private GameManager _gameManager;

        [Header("Debug Settings")]
        [SerializeField] private string _debugRoomCode = "DEBUG";
        [SerializeField] private int _defaultActorNumber = 1;
        [SerializeField] private int _maxPlayers = 4;
        [SerializeField] private bool _autoStartGame = true;
        [SerializeField] private float _waitForPlayersTimeout = 10f;
        [SerializeField] private string _fixedRegion = "jp"; // 固定リージョン（リージョンPingをスキップ）
        [SerializeField] private float _connectionTimeout = 15f; // 接続タイムアウト（秒）

        [Header("Player Debug Settings")]
        [Tooltip("Inspector上でプレイヤー情報をカスタマイズする（空の場合は自動設定）")]
        [SerializeField] private List<DebugPlayerInfo> _debugPlayerInfos = new List<DebugPlayerInfo>();
        [SerializeField] private bool _useDebugPlayerInfo = false; // デバッグプレイヤー情報を使用するかどうか

        [Header("Initial Cards Debug")]
        [SerializeField] private float _firstDealWaitTime = 2f; // FirstDeal完了待機時間（秒）
        [SerializeField] private bool _clearCardsBeforeSetup = true; // カード設定前に既存カードをクリア

        #endregion

        [Header("Debug Network Event (Inspector)")]
        [SerializeField] private bool _enableDebugEvents = false;
        [SerializeField] private Tetrage.Network.Gameplay.EventCode _debugEventCode = Tetrage.Network.Gameplay.EventCode.ActionResult;
        [SerializeField, TextArea(3, 10)] private string _debugJsonPayload = "{}";
        [SerializeField] private bool _sendToAll = true;
        [SerializeField] private int[] _targetActorNumbers;

        #region Private Fields

        private bool _isInitialized = false;
        private bool _isConnecting = false;
        private bool _roomCreatedOrJoined = false;
        private ISerializer _debugSerializer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _isInitialized = false;
            _isConnecting = false;
            _roomCreatedOrJoined = false;

            // PhotonViewの自動取得
            if (_photonView == null)
            {
                _photonView = GetComponent<PhotonView>();
                if (_photonView == null)
                {
                    Debug.LogWarning("[GameSceneDebugEntry] PhotonViewコンポーネントが見つかりません。RPCによるネットワーク同期が無効です。");
                }
            }

            // ===== MultiPlayMode対応設定 =====
            // バックグラウンドでも実行を継続（MultiPlayModeで必須）
            Application.runInBackground = true;

            // Photonのキープアライブ設定
            PhotonNetwork.KeepAliveInBackground = 60f; // 60秒間接続を維持

            // SendRateとSerializationRateも調整（オプション）
            PhotonNetwork.SendRate = 20; // 1秒あたりの送信回数
            PhotonNetwork.SerializationRate = 10; // 1秒あたりのシリアライゼーション回数

            Debug.Log("[GameSceneDebugEntry] MultiPlayMode設定完了");
            Debug.Log($"[GameSceneDebugEntry] runInBackground: {Application.runInBackground}");
            Debug.Log($"[GameSceneDebugEntry] KeepAliveInBackground: {PhotonNetwork.KeepAliveInBackground}s");
            _debugSerializer = new PhotonJsonSerializer();
        }

        private async void Start()
        {
            Debug.Log("[GameSceneDebugEntry] デバッグモード開始");
            await ConnectAndSetupRoom();
        }

        #endregion

        #region Debug Network Event Sender
        [ContextMenu("Debug/Send Network Event")]
        public void DebugSendNetworkEvent()
        {
            if (!_enableDebugEvents)
            {
                Debug.LogWarning("[GameSceneDebugEntry] Debug events are disabled.");
                return;
            }
            if (!Photon.Pun.PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogWarning("[GameSceneDebugEntry] Not connected to Photon.");
                return;
            }

            byte[] bytes;
            try
            {
                bytes = System.Text.Encoding.UTF8.GetBytes(_debugJsonPayload ?? "{}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameSceneDebugEntry] Payload encode failed: {ex.Message}");
                return;
            }

            var opts = new RaiseEventOptions();
            if (_sendToAll)
            {
                opts.Receivers = ReceiverGroup.All;
            }
            else
            {
                if (_targetActorNumbers == null || _targetActorNumbers.Length == 0)
                {
                    Debug.LogWarning("[GameSceneDebugEntry] TargetActors is empty; nothing to send.");
                    return;
                }
                opts.TargetActors = _targetActorNumbers;
            }
            var sendOpts = new SendOptions { Reliability = true };

            Photon.Pun.PhotonNetwork.RaiseEvent((byte)_debugEventCode, bytes, opts, sendOpts);
            Debug.Log($"[GameSceneDebugEntry] Raised event: {_debugEventCode} to {(_sendToAll ? "All" : string.Join(",", _targetActorNumbers ?? System.Array.Empty<int>()))}\nPayload: {_debugJsonPayload}");
        }
        #endregion

        #region Network Setup

        /// <summary>
        /// PUN2に接続してルームをセットアップ
        /// </summary>
        private async UniTask ConnectAndSetupRoom()
        {
            if (_isConnecting)
            {
                Debug.LogWarning("[GameSceneDebugEntry] 既に接続処理中です");
                return;
            }

            _isConnecting = true;

            // PUN2に接続
            if (!PhotonNetwork.IsConnected)
            {
                // 固定リージョンを設定してリージョンPingをスキップ
                if (!string.IsNullOrEmpty(_fixedRegion))
                {
                    Debug.Log($"[GameSceneDebugEntry] 固定リージョンを設定: {_fixedRegion}");
                    PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = _fixedRegion;
                }

                Debug.Log("[GameSceneDebugEntry] Photonサーバーに接続中...");
                PhotonNetwork.ConnectUsingSettings();

                // 接続完了を待機（タイムアウト付き）
                float elapsed = 0f;
                while (!PhotonNetwork.IsConnectedAndReady && elapsed < _connectionTimeout)
                {
                    await UniTask.Delay(100);
                    elapsed += 0.1f;
                }

                if (!PhotonNetwork.IsConnectedAndReady)
                {
                    Debug.LogError("[GameSceneDebugEntry] Photonサーバーへの接続がタイムアウトしました");
                    _isConnecting = false;
                    return;
                }

                Debug.Log("[GameSceneDebugEntry] Photonサーバーに接続完了");
            }

            // ロビーに参加
            if (!PhotonNetwork.InLobby)
            {
                Debug.Log("[GameSceneDebugEntry] ロビーに参加中...");
                PhotonNetwork.JoinLobby();

                // ロビー参加を待機（タイムアウト付き）
                float elapsed = 0f;
                while (!PhotonNetwork.InLobby && elapsed < _connectionTimeout)
                {
                    await UniTask.Delay(100);
                    elapsed += 0.1f;
                }

                if (!PhotonNetwork.InLobby)
                {
                    Debug.LogError("[GameSceneDebugEntry] ロビーへの参加がタイムアウトしました");
                    _isConnecting = false;
                    return;
                }

                Debug.Log("[GameSceneDebugEntry] ロビーに参加完了");
            }

            // ルームに参加または作成
            await JoinOrCreateRoom();
        }

        /// <summary>
        /// デバッグルームに参加または作成
        /// </summary>
        private async UniTask JoinOrCreateRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.Log($"[GameSceneDebugEntry] 既にルームに参加済み: {PhotonNetwork.CurrentRoom.Name}");
                _roomCreatedOrJoined = true;
                await WaitForPlayersAndStartGame();
                return;
            }

            // まず参加を試みる
            Debug.Log($"[GameSceneDebugEntry] ルーム '{_debugRoomCode}' への参加を試行中...");
            PhotonNetwork.JoinRoom(_debugRoomCode);

            // ルーム参加の結果を待機
            await UniTask.WaitUntil(() => _roomCreatedOrJoined || PhotonNetwork.InRoom, cancellationToken: this.GetCancellationTokenOnDestroy());

            if (PhotonNetwork.InRoom)
            {
                await WaitForPlayersAndStartGame();
            }
        }

        /// <summary>
        /// プレイヤーが揃うのを待ってゲーム開始
        /// </summary>
        private async UniTask WaitForPlayersAndStartGame()
        {
            Debug.Log($"[GameSceneDebugEntry] プレイヤー待機中... (現在: {PhotonNetwork.CurrentRoom.PlayerCount}/{_maxPlayers})");

            // タイムアウト付きで全プレイヤーが揃うまで待機
            float elapsed = 0f;
            while (PhotonNetwork.CurrentRoom.PlayerCount < _maxPlayers && elapsed < _waitForPlayersTimeout)
            {
                await UniTask.Delay(100);
                elapsed += 0.1f;
            }

            if (PhotonNetwork.CurrentRoom.PlayerCount < _maxPlayers)
            {
                Debug.LogWarning($"[GameSceneDebugEntry] タイムアウト。現在のプレイヤー数でゲーム開始: {PhotonNetwork.CurrentRoom.PlayerCount}");
            }
            else
            {
                Debug.Log($"[GameSceneDebugEntry] 全プレイヤーが揃いました: {PhotonNetwork.CurrentRoom.PlayerCount}");
            }

            // 全てのクライアントがゲームを初期化
            if (!_isInitialized)
            {
                InitializeGame();
            }

            // ホストがゲームを開始
            if (PhotonNetwork.IsMasterClient)
            {
                await UniTask.Delay(500); // 初期化完了を待つ

                if (_autoStartGame)
                {
                    StartGame();

                    // ゲーム開始後、FirstDealの完了を待ってからデバッグカード設定を適用
                    // 少なくとも1人がSetInitialCards=trueなら実行
                    bool hasCardSetup = _useDebugPlayerInfo && _debugPlayerInfos != null &&
                                       _debugPlayerInfos.Any(p => p.SetInitialCards);

                    if (hasCardSetup)
                    {
                        Debug.Log($"[GameSceneDebugEntry] FirstDealの完了を待機中... ({_firstDealWaitTime}秒)");
                        await UniTask.Delay((int)(_firstDealWaitTime * 1000)); // FirstDealの完了を待つ
                        Debug.Log("[GameSceneDebugEntry] FirstDeal完了後、デバッグカード設定を適用します");
                        SetupDebugInitialCards();
                    }
                }
            }
        }

        #endregion

        #region Game Initialization

        /// <summary>
        /// ゲームを初期化
        /// </summary>
        private void InitializeGame()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[GameSceneDebugEntry] 既に初期化済みです");
                return;
            }

            Debug.Log("[GameSceneDebugEntry] GameManager初期化開始");

            // 参加者情報を作成
            List<PlayerInfo> participantInfos = CreateParticipantInfos();

            // ローカルプレイヤーの情報を取得
            PlayerInfo localPlayerInfo = participantInfos.FirstOrDefault(p => p.Id.Value == PhotonNetwork.LocalPlayer.ActorNumber);

            // GameManagerを初期化
            _gameManager.Initialize(participantInfos, localPlayerInfo, new VirtualNetworkContext(_defaultActorNumber, true), NetworkMode.RealPhoton);
            _isInitialized = true;

            Debug.Log("[GameSceneDebugEntry] GameManager初期化完了");
        }

        /// <summary>
        /// ルーム内のプレイヤーから参加者情報を作成
        /// </summary>
        private List<PlayerInfo> CreateParticipantInfos()
        {
            var participantInfos = new List<PlayerInfo>();

            // PhotonNetwork.PlayerListは入室順（ActorNumber順）にソート済み
            var sortedPhotonPlayers = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();

            for (int i = 0; i < sortedPhotonPlayers.Length; i++)
            {
                var photonPlayer = sortedPhotonPlayers[i];

                // 入室順インデックスiに対応するデバッグ設定を取得
                DebugPlayerInfo debugInfo = null;
                if (_useDebugPlayerInfo && _debugPlayerInfos != null && i < _debugPlayerInfos.Count)
                {
                    debugInfo = _debugPlayerInfos[i];
                    Debug.Log($"[GameSceneDebugEntry] 入室順 {i} (ActorNumber {photonPlayer.ActorNumber}): Element {i} のデバッグ設定を使用 - Name={debugInfo.PlayerName}, Icon={debugInfo.IconIndex}");
                }

                var playerInfo = new PlayerInfo
                {
                    Id = new PlayerId(photonPlayer.ActorNumber),
                    // デバッグ設定があればそれを使用、なければデフォルト
                    UserId = debugInfo?.PlayerName ?? photonPlayer.UserId ?? $"Player_{photonPlayer.ActorNumber}",
                    PlayerType = photonPlayer.IsLocal ? PlayerType.Local : PlayerType.Remote,
                    // デバッグ設定があればそれを使用、なければActorNumberベース
                    PlayerIconIndex = debugInfo?.IconIndex ?? ((photonPlayer.ActorNumber - 1) % 4)
                };

                participantInfos.Add(playerInfo);
            }

            // プレイヤー情報ログ出力
            Debug.Log("[GameSceneDebugEntry] 最終的なプレイヤーリスト（入室順）:");
            for (int i = 0; i < participantInfos.Count; i++)
            {
                Debug.Log($"[GameSceneDebugEntry] 入室順 {i} (ActorNumber {participantInfos[i].Id.Value}): {participantInfos[i].UserId} (Type: {participantInfos[i].PlayerType}, Icon: {participantInfos[i].PlayerIconIndex})");
            }

            return participantInfos;
        }

        #endregion

        #region Debug Card Setup

        /// <summary>
        /// デバッグ用の初期カードを設定（FirstDeal後に実行）
        /// </summary>
        private void SetupDebugInitialCards()
        {
            if (!_useDebugPlayerInfo || _debugPlayerInfos == null || _debugPlayerInfos.Count == 0)
            {
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[GameSceneDebugEntry] 初期カード設定はホストのみ実行できます");
                return;
            }

            Debug.Log("[GameSceneDebugEntry] デバッグ用初期カード設定開始（ホスト）");

            // カード配置情報をJSON文字列にシリアライズして全クライアントに送信
            var cardPlacementJson = SerializeCardPlacementsToJson();
            if (!string.IsNullOrEmpty(cardPlacementJson))
            {
                // PhotonViewがない場合はローカルのみで実行
                if (_photonView == null)
                {
                    Debug.LogWarning("[GameSceneDebugEntry] PhotonViewが設定されていません。ローカルのみで実行します");
                    ApplyCardPlacementsFromJson(cardPlacementJson);
                }
                else
                {
                    // RPCで全クライアントに送信
                    _photonView.RPC(nameof(RPC_ApplyCardPlacements), RpcTarget.All, cardPlacementJson);
                    Debug.Log("[GameSceneDebugEntry] カード配置情報を全クライアントに送信しました");
                }
            }
        }

        /// <summary>
        /// カード配置情報をJSON文字列にシリアライズ
        /// </summary>
        private string SerializeCardPlacementsToJson()
        {
            var gameContext = _gameManager.GameContext;
            if (gameContext == null)
            {
                Debug.LogError("[GameSceneDebugEntry] GameContextが見つかりません");
                return null;
            }

            var players = gameContext.Players.ToList();

            // ActorNumber順にソート（入室順と一致）
            var sortedPlayers = players.OrderBy(p => p.PlayerId).ToArray();

            var data = new CardPlacementData();
            data.Players = new List<PlayerCardData>();

            // 全プレイヤー分のカード設定を作成
            for (int i = 0; i < _debugPlayerInfos.Count; i++)
            {
                var debugInfo = _debugPlayerInfos[i];

                // SetInitialCardsがfalseの場合はスキップ
                if (!debugInfo.SetInitialCards)
                {
                    Debug.Log($"[GameSceneDebugEntry] Element {i}: SetInitialCards=falseのためスキップ");
                    continue;
                }

                // 入室順インデックスiに対応するプレイヤーを取得
                if (i >= sortedPlayers.Length)
                {
                    Debug.LogWarning($"[GameSceneDebugEntry] Element {i} に対応するプレイヤーが見つかりません");
                    continue;
                }

                var targetPlayer = sortedPlayers[i];

                var playerData = new PlayerCardData();
                playerData.PlayerId = targetPlayer.PlayerId;

                Debug.Log($"[GameSceneDebugEntry] Element {i} → ActorNumber {targetPlayer.PlayerId}");

                // Target カード
                if (debugInfo.TargetCard != null && debugInfo.TargetCard.IsValid)
                {
                    playerData.TargetSuit = (int)debugInfo.TargetCard.Suit;
                    playerData.TargetNumber = debugInfo.TargetCard.Number;
                    playerData.HasTarget = true;
                    Debug.Log($"[GameSceneDebugEntry] Element {i} Target設定: {debugInfo.TargetCard.Suit} {debugInfo.TargetCard.Number} → シリアライズ: Suit={(int)debugInfo.TargetCard.Suit}, Number={debugInfo.TargetCard.Number}");
                }
                else
                {
                    playerData.HasTarget = false;
                    Debug.Log($"[GameSceneDebugEntry] Element {i} Target無効またはnull");
                }

                // Hands カード
                playerData.HandSuits = new List<int>();
                playerData.HandNumbers = new List<int>();
                var validHandCards = debugInfo.HandCards.Where(c => c != null && c.IsValid).ToList();
                Debug.Log($"[GameSceneDebugEntry] Element {i} Handsカード数: 全{debugInfo.HandCards?.Count ?? 0}枚, 有効{validHandCards.Count}枚");
                foreach (var cardSpec in validHandCards)
                {
                    playerData.HandSuits.Add((int)cardSpec.Suit);
                    playerData.HandNumbers.Add(cardSpec.Number);
                    Debug.Log($"[GameSceneDebugEntry] Element {i} Hand追加: {cardSpec.Suit} {cardSpec.Number} → シリアライズ: Suit={(int)cardSpec.Suit}, Number={cardSpec.Number}");
                }

                data.Players.Add(playerData);
            }

            var json = JsonUtility.ToJson(data, true); // prettifyを有効化
            Debug.Log($"[GameSceneDebugEntry] シリアライズ完了: {data.Players.Count}人分");
            Debug.Log($"[GameSceneDebugEntry] JSON内容:\n{json}");
            return json;
        }

        /// <summary>
        /// RPC: 全クライアントでカード配置を適用
        /// </summary>
        [PunRPC]
        public void RPC_ApplyCardPlacements(string cardPlacementJson)
        {
            Debug.Log($"[GameSceneDebugEntry] カード配置情報を受信しました (クライアント: {PhotonNetwork.LocalPlayer.ActorNumber})");
            ApplyCardPlacementsFromJson(cardPlacementJson);
        }

        /// <summary>
        /// JSON文字列からカード配置を適用
        /// </summary>
        private void ApplyCardPlacementsFromJson(string cardPlacementJson)
        {
            Debug.Log($"[GameSceneDebugEntry] 受信したJSON:\n{cardPlacementJson}");

            var gameContext = _gameManager.GameContext;
            if (gameContext == null)
            {
                Debug.LogError("[GameSceneDebugEntry] GameContextが見つかりません");
                return;
            }

            var data = JsonUtility.FromJson<CardPlacementData>(cardPlacementJson);
            if (data == null || data.Players == null)
            {
                Debug.LogError("[GameSceneDebugEntry] カード配置データの解析に失敗しました");
                return;
            }

            Debug.Log($"[GameSceneDebugEntry] デシリアライズ完了: {data.Players.Count}人分のデータ");

            // デシリアライズ結果を詳細表示
            for (int i = 0; i < data.Players.Count; i++)
            {
                var pd = data.Players[i];
                Debug.Log($"[GameSceneDebugEntry] Players[{i}]: PlayerId={pd.PlayerId}, HasTarget={pd.HasTarget}, TargetSuit={pd.TargetSuit}, TargetNumber={pd.TargetNumber}, HandSuits=[{string.Join(",", pd.HandSuits ?? new List<int>())}], HandNumbers=[{string.Join(",", pd.HandNumbers ?? new List<int>())}]");
            }

            var stack = gameContext.Stage.Stack;
            var players = gameContext.Players;

            // カード設定前にクリアする場合
            if (_clearCardsBeforeSetup)
            {
                Debug.Log("[GameSceneDebugEntry] 既存のカードを山札に戻します");
                foreach (var player in players)
                {
                    // Target のカードを山札に戻す
                    while (player.Target.Count > 0)
                    {
                        var card = player.Target.Cards[0];
                        CardPile.TransferService.Transfer(player.Target, stack, card);
                    }

                    // Hands のカードを山札に戻す
                    while (player.Hands.Count > 0)
                    {
                        var card = player.Hands.Cards[0];
                        CardPile.TransferService.Transfer(player.Hands, stack, card);
                    }
                }
                Debug.Log("[GameSceneDebugEntry] カードクリア完了");
            }

            // 各プレイヤーのカードを設定
            foreach (var playerData in data.Players)
            {
                Debug.Log($"[GameSceneDebugEntry] PlayerData処理開始: ActorNumber={playerData.PlayerId}, HasTarget={playerData.HasTarget}");
                Debug.Log($"[GameSceneDebugEntry]   TargetSuit={playerData.TargetSuit}, TargetNumber={playerData.TargetNumber}");
                Debug.Log($"[GameSceneDebugEntry]   HandSuitsCount={playerData.HandSuits?.Count ?? 0}, HandNumbersCount={playerData.HandNumbers?.Count ?? 0}");

                // PlayerIdはActorNumberと同じ値
                var player = players.FirstOrDefault(p => p.PlayerId == playerData.PlayerId);
                if (player == null)
                {
                    Debug.LogWarning($"[GameSceneDebugEntry] ActorNumber {playerData.PlayerId} のプレイヤーが見つかりません");
                    continue;
                }

                // Target カード
                if (playerData.HasTarget)
                {
                    Suit targetSuit = (Suit)playerData.TargetSuit;
                    Debug.Log($"[GameSceneDebugEntry] Target検索: Suit={(int)targetSuit}({targetSuit}), Number={playerData.TargetNumber}");

                    var targetCard = FindCardInStackBySuitNumber(stack, targetSuit, playerData.TargetNumber);
                    if (targetCard != null)
                    {
                        CardPile.TransferService.Transfer(stack, player.Target, targetCard);
                        Debug.Log($"[GameSceneDebugEntry] ✅ ActorNumber {playerData.PlayerId} の Target に {targetCard.Suit} {targetCard.Number} を設定");
                    }
                    else
                    {
                        Debug.LogWarning($"[GameSceneDebugEntry] ❌ Target カード {targetSuit} {playerData.TargetNumber} が山札に見つかりません");
                    }
                }

                // Hands カード
                if (playerData.HandSuits != null && playerData.HandNumbers != null)
                {
                    Debug.Log($"[GameSceneDebugEntry] Hands処理: {playerData.HandSuits.Count}枚");
                    for (int i = 0; i < playerData.HandSuits.Count && i < playerData.HandNumbers.Count; i++)
                    {
                        Suit handSuit = (Suit)playerData.HandSuits[i];
                        int handNumber = playerData.HandNumbers[i];

                        Debug.Log($"[GameSceneDebugEntry] Hand検索 [{i}]: Suit={playerData.HandSuits[i]}({handSuit}), Number={handNumber}");

                        var handCard = FindCardInStackBySuitNumber(stack, handSuit, handNumber);
                        if (handCard != null)
                        {
                            CardPile.TransferService.Transfer(stack, player.Hands, handCard);
                            Debug.Log($"[GameSceneDebugEntry] ✅ ActorNumber {playerData.PlayerId} の Hands に {handCard.Suit} {handCard.Number} を追加");
                        }
                        else
                        {
                            Debug.LogWarning($"[GameSceneDebugEntry] ❌ Hand カード {handSuit} {handNumber} が山札に見つかりません");
                        }
                    }
                }
            }

            Debug.Log("[GameSceneDebugEntry] デバッグ用初期カード設定完了");
        }

        /// <summary>
        /// 山札から指定されたカードを検索（CardSpec版）
        /// </summary>
        private Card FindCardInStack(CardPile stack, CardSpec spec)
        {
            return FindCardInStackBySuitNumber(stack, spec.Suit, spec.Number);
        }

        /// <summary>
        /// 山札から指定されたカードを検索（Suit/Number版）
        /// </summary>
        private Card FindCardInStackBySuitNumber(CardPile stack, Suit suit, int number)
        {
            foreach (var card in stack.Cards)
            {
                if (card.Suit == suit && card.Number == number)
                {
                    return card;
                }
            }

            Debug.LogWarning($"[GameSceneDebugEntry] 山札に {suit} {number} が見つかりません");
            return null;
        }

        #endregion

        #region Game Control

        /// <summary>
        /// ゲームを開始
        /// </summary>
        [ContextMenu("Start Game")]
        public async void StartGame()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[GameSceneDebugEntry] ゲームが初期化されていません");
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[GameSceneDebugEntry] ゲーム開始はホストのみ実行できます");
                return;
            }

            Debug.Log("[GameSceneDebugEntry] ゲーム開始");
            await _gameManager.StartGame();
        }

        /// <summary>
        /// ゲームを停止
        /// </summary>
        [ContextMenu("Stop Game")]
        public void StopGame()
        {
            Debug.Log("[GameSceneDebugEntry] ゲーム停止");
            _gameManager.StopGame();
        }

        /// <summary>
        /// ゲームをリセット
        /// </summary>
        [ContextMenu("Reset Game")]
        public void ResetGame()
        {
            Debug.Log("[GameSceneDebugEntry] ゲームリセット");
            _gameManager.Reset();
            _isInitialized = false;
        }

        #endregion

        #region Photon Callbacks

        public override void OnJoinedRoom()
        {
            Debug.Log($"[GameSceneDebugEntry] ルーム参加成功: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"[GameSceneDebugEntry] ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
            _roomCreatedOrJoined = true;
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[GameSceneDebugEntry] ルーム参加失敗。ルームを作成します: {message}");

            // ルームが存在しない場合は作成
            RoomOptions options = new RoomOptions
            {
                MaxPlayers = (byte)_maxPlayers,
                IsVisible = false,  // デバッグルームなので非表示
                IsOpen = true
            };

            PhotonNetwork.CreateRoom(_debugRoomCode, options);
        }

        public override void OnCreatedRoom()
        {
            Debug.Log($"[GameSceneDebugEntry] ルーム作成成功: {PhotonNetwork.CurrentRoom.Name}");
            _roomCreatedOrJoined = true;
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[GameSceneDebugEntry] ルーム作成失敗: {message} (Code: {returnCode})");
            _isConnecting = false;
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            Debug.Log($"[GameSceneDebugEntry] プレイヤー参加: {newPlayer.NickName} (ActorNumber: {newPlayer.ActorNumber})");
            Debug.Log($"[GameSceneDebugEntry] 現在のプレイヤー数: {PhotonNetwork.CurrentRoom.PlayerCount}/{_maxPlayers}");
        }

        #endregion
    }



    /// <summary>
    /// ネットワーク送信用のカード配置データ
    /// </summary>
    [System.Serializable]
    public class CardPlacementData
    {
        public List<PlayerCardData> Players;
    }

    /// <summary>
    /// プレイヤー単位のカードデータ（ネットワーク送信用）
    /// </summary>
    [System.Serializable]
    public class PlayerCardData
    {
        public int PlayerId;  // PhotonのActorNumberと同じ値（入室順から解決される）
        public bool HasTarget;
        public int TargetSuit;
        public int TargetNumber;
        public List<int> HandSuits;
        public List<int> HandNumbers;
    }
}
