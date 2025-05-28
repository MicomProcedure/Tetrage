using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Models;
using Tetrage.Core.Contracts;

namespace Tetrage.Tests
{
    /// <summary>
    /// FieldSetupManagerTesterと連携してカード配布とプレイヤー間転送をテストするクラス
    /// Stackからプレイヤーへのカード均等配布機能とプレイヤー間転送機能を提供
    /// </summary>
    public class CardDistributionTester : MonoBehaviour
    {
        [Header("カード配布設定")]
        [SerializeField] private int cardsPerPlayer = 13;
        [Tooltip("配布先を指定 (0: Hands, 1: Tmp, 2: Target)")]
        [SerializeField] private int targetPileIndex = 0; // 0=Hands, 1=Tmp, 2=Target
        
        [Header("プレイヤー間転送設定")]
        [SerializeField] private int fromPlayerIndex = 0;
        [SerializeField] private int toPlayerIndex = 1;
        [Tooltip("転送元カードパイル (0: Hands, 1: Tmp, 2: Target)")]
        [SerializeField] private int fromPileType = 0;
        [Tooltip("転送先カードパイル (0: Hands, 1: Tmp, 2: Target)")]
        [SerializeField] private int toPileType = 0;
        
        private FieldSetupManagerTester _fieldSetupTester;
        private readonly string[] _pileTypeNames = { "Hands", "Tmp", "Target" };
        
        void Start()
        {
            // 同じGameObjectのFieldSetupManagerTesterを取得
            _fieldSetupTester = GetComponent<FieldSetupManagerTester>();
            if (_fieldSetupTester == null)
            {
                Debug.LogWarning("CardDistributionTester: FieldSetupManagerTesterがアタッチされていません");
            }
        }
        
        /// <summary>
        /// Stackのカードを全プレイヤーに均等配布
        /// </summary>
        [ContextMenu("Stackから全プレイヤーに均等配布")]
        public void DistributeCardsToAllPlayers()
        {
            if (!ValidateFieldSetup())
            {
                return;
            }
            
            var manager = _fieldSetupTester.GetFieldSetupManager();
            var stack = manager.Stage.Stack;
            var players = manager.Players;
            
            Debug.Log($"=== カード配布開始 ===");
            Debug.Log($"配布前 - Stack: {stack.Cards.Count}枚");
            
            // 配布可能枚数の計算
            int totalCards = stack.Cards.Count;
            int distributableCards = Mathf.Min(totalCards, cardsPerPlayer * players.Count);
            int actualCardsPerPlayer = distributableCards / players.Count;
            
            Debug.Log($"配布予定: 各プレイヤーに{actualCardsPerPlayer}枚 (総計{actualCardsPerPlayer * players.Count}枚)");
            
            // 各プレイヤーに配布
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                var player = players[playerIndex];
                var targetPile = GetPlayerCardPile(player, targetPileIndex);
                
                if (targetPile == null)
                {
                    Debug.LogError($"プレイヤー{playerIndex + 1}の配布先カードパイルが取得できませんでした");
                    continue;
                }
                
                int distributedCount = 0;
                
                // 指定枚数分配布
                for (int cardIndex = 0; cardIndex < actualCardsPerPlayer; cardIndex++)
                {
                    if (stack.Cards.Count == 0)
                    {
                        Debug.LogWarning("Stackが空になりました");
                        break;
                    }
                    
                    var card = stack.Cards.First();
                    bool success = CardPile.TransferService.Transfer(stack, targetPile, card);
                    
                    if (success)
                    {
                        distributedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"プレイヤー{playerIndex + 1}への配布に失敗: {card.Suit} {card.Number}");
                    }
                }
                
                Debug.Log($"プレイヤー{playerIndex + 1} ({player.UserId}): {_pileTypeNames[targetPileIndex]}に{distributedCount}枚配布");
            }
            
            Debug.Log($"配布後 - Stack: {stack.Cards.Count}枚");
            Debug.Log($"=== カード配布完了 ===");
            
            // 配布結果の詳細表示
            LogDistributionResults();
        }
        
        /// <summary>
        /// 指定プレイヤー間でカードを1枚転送
        /// </summary>
        [ContextMenu("プレイヤー間でカード転送")]
        public void TransferCardBetweenPlayers()
        {
            if (!ValidateFieldSetup())
            {
                return;
            }
            
            var manager = _fieldSetupTester.GetFieldSetupManager();
            var players = manager.Players;
            
            // インデックス範囲チェック
            if (fromPlayerIndex >= players.Count || toPlayerIndex >= players.Count ||
                fromPlayerIndex < 0 || toPlayerIndex < 0)
            {
                Debug.LogError($"プレイヤーインデックスが範囲外です。利用可能プレイヤー数: {players.Count}");
                return;
            }
            
            if (fromPlayerIndex == toPlayerIndex)
            {
                Debug.LogWarning("同じプレイヤーが指定されています");
                return;
            }
            
            var fromPlayer = players[fromPlayerIndex];
            var toPlayer = players[toPlayerIndex];
            
            var fromPile = GetPlayerCardPile(fromPlayer, fromPileType);
            var toPile = GetPlayerCardPile(toPlayer, toPileType);
            
            if (fromPile == null || toPile == null)
            {
                Debug.LogError("転送元または転送先のカードパイルが取得できませんでした");
                return;
            }
            
            if (fromPile.Cards.Count == 0)
            {
                Debug.LogWarning($"転送元 {fromPlayer.UserId}の{_pileTypeNames[fromPileType]}にカードがありません");
                return;
            }
            
            var card = fromPile.Cards.First();
            bool success = CardPile.TransferService.Transfer(fromPile, toPile, card);
            
            if (success)
            {
                Debug.Log($"カード転送成功: {card.Suit} {card.Number}");
                Debug.Log($"  {fromPlayer.UserId}の{_pileTypeNames[fromPileType]} → {toPlayer.UserId}の{_pileTypeNames[toPileType]}");
            }
            else
            {
                Debug.LogWarning("カード転送に失敗しました");
            }
            
            // 転送後の状態をログ出力
            LogTransferResults(fromPlayer, toPlayer);
        }
        
        /// <summary>
        /// 配布状況を全プレイヤー分表示
        /// </summary>
        [ContextMenu("配布状況を表示")]
        public void ShowDistributionStatus()
        {
            if (!ValidateFieldSetup())
            {
                return;
            }
            
            var manager = _fieldSetupTester.GetFieldSetupManager();
            var players = manager.Players;
            var stack = manager.Stage.Stack;
            var trash = manager.Stage.Trash;
            
            Debug.Log("=== 現在の配布状況 ===");
            Debug.Log($"Stack: {stack.Cards.Count}枚");
            Debug.Log($"Trash: {trash.Cards.Count}枚");
            
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                Debug.Log($"プレイヤー{i + 1} ({player.UserId}):");
                Debug.Log($"  Hands: {player.Hands.Cards.Count}枚");
                Debug.Log($"  Tmp: {player.Tmp.Cards.Count}枚");
                Debug.Log($"  Target: {player.Target.Cards.Count}枚");
            }
            Debug.Log("======================");
        }
        
        /// <summary>
        /// 全カードをStackに戻す
        /// </summary>
        [ContextMenu("全カードをStackに戻す")]
        public void ReturnAllCardsToStack()
        {
            if (!ValidateFieldSetup())
            {
                return;
            }
            
            var manager = _fieldSetupTester.GetFieldSetupManager();
            var players = manager.Players;
            var stack = manager.Stage.Stack;
            var trash = manager.Stage.Trash;
            
            Debug.Log("=== 全カードをStackに回収開始 ===");
            
            int totalReturned = 0;
            
            // 各プレイヤーのカードを回収
            foreach (var player in players)
            {
                totalReturned += ReturnCardsFromPile(player.Hands, stack, $"{player.UserId}のHands");
                totalReturned += ReturnCardsFromPile(player.Tmp, stack, $"{player.UserId}のTmp");
                totalReturned += ReturnCardsFromPile(player.Target, stack, $"{player.UserId}のTarget");
            }
            
            // Trashからも回収
            totalReturned += ReturnCardsFromPile(trash, stack, "Trash");
            
            Debug.Log($"回収完了: 合計{totalReturned}枚をStackに戻しました");
            Debug.Log($"最終 Stack: {stack.Cards.Count}枚");
            Debug.Log("=== 全カード回収完了 ===");
        }
        
        /// <summary>
        /// 指定カードパイルからStackに全カードを戻す
        /// </summary>
        private int ReturnCardsFromPile(CardPile fromPile, CardPile stack, string pileName)
        {
            int returnedCount = 0;
            var cardsToReturn = fromPile.Cards.ToList(); // コピーを作成
            
            foreach (var card in cardsToReturn)
            {
                bool success = CardPile.TransferService.Transfer(fromPile, stack, card);
                if (success)
                {
                    returnedCount++;
                }
            }
            
            if (returnedCount > 0)
            {
                Debug.Log($"{pileName}から{returnedCount}枚回収");
            }
            
            return returnedCount;
        }
        
        /// <summary>
        /// プレイヤーの指定カードパイルを取得
        /// </summary>
        private CardPile GetPlayerCardPile(IPlayer player, int pileIndex)
        {
            switch (pileIndex)
            {
                case 0: return player.Hands;
                case 1: return player.Tmp;
                case 2: return player.Target;
                default:
                    Debug.LogError($"無効なカードパイルインデックス: {pileIndex}");
                    return null;
            }
        }
        
        /// <summary>
        /// FieldSetupManagerの状態を検証
        /// </summary>
        private bool ValidateFieldSetup()
        {
            if (_fieldSetupTester == null)
            {
                Debug.LogError("FieldSetupManagerTesterがアタッチされていません");
                return false;
            }
            
            var manager = _fieldSetupTester.GetFieldSetupManager();
            if (manager == null)
            {
                Debug.LogError("FieldSetupManagerがセットアップされていません。先にフィールドセットアップを実行してください");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 配布結果を詳細ログ出力
        /// </summary>
        private void LogDistributionResults()
        {
            var manager = _fieldSetupTester.GetFieldSetupManager();
            var players = manager.Players;
            
            Debug.Log("--- 配布結果詳細 ---");
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var targetPile = GetPlayerCardPile(player, targetPileIndex);
                
                Debug.Log($"プレイヤー{i + 1} ({player.UserId}) - {_pileTypeNames[targetPileIndex]}: {targetPile.Cards.Count}枚");
                
                // 最初の3枚のカードを表示
                var cardsToShow = targetPile.Cards.Take(3);
                foreach (var card in cardsToShow)
                {
                    Debug.Log($"  - {card.Suit} {card.Number}");
                }
                
                if (targetPile.Cards.Count > 3)
                {
                    Debug.Log($"  ... 他{targetPile.Cards.Count - 3}枚");
                }
            }
        }
        
        /// <summary>
        /// 転送結果をログ出力
        /// </summary>
        private void LogTransferResults(IPlayer fromPlayer, IPlayer toPlayer)
        {
            Debug.Log("--- 転送後の状態 ---");
            
            var fromPile = GetPlayerCardPile(fromPlayer, fromPileType);
            var toPile = GetPlayerCardPile(toPlayer, toPileType);
            
            Debug.Log($"{fromPlayer.UserId}の{_pileTypeNames[fromPileType]}: {fromPile.Cards.Count}枚");
            Debug.Log($"{toPlayer.UserId}の{_pileTypeNames[toPileType]}: {toPile.Cards.Count}枚");
        }
        
        /// <summary>
        /// Inspector設定値の検証
        /// </summary>
        void OnValidate()
        {
            // カードパイルインデックスの範囲チェック
            targetPileIndex = Mathf.Clamp(targetPileIndex, 0, 2);
            fromPileType = Mathf.Clamp(fromPileType, 0, 2);
            toPileType = Mathf.Clamp(toPileType, 0, 2);
            
            // カード枚数の最小値チェック
            cardsPerPlayer = Mathf.Max(1, cardsPerPlayer);
            
            // プレイヤーインデックスの最小値チェック
            fromPlayerIndex = Mathf.Max(0, fromPlayerIndex);
            toPlayerIndex = Mathf.Max(0, toPlayerIndex);
        }
    }
} 