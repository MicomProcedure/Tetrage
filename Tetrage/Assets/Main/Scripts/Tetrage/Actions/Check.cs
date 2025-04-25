using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Tetrage.Models;

public class Check : MonoBehaviour
{
    private Player player1;

    public bool DebugMode = true;

    public GameObject cardPrefab; // Inspector で Card プレハブを設定
    public Transform[] cardSpawnPoints; // 配置場所を設定

    void Start()
    {
        player1 = new Player();

        if (DebugMode)
        {
            AddCardToPlayer(Card.Suit.Spade, 7, true);
            AddCardToPlayer(Card.Suit.Spade, 3, true);
            AddCardToPlayer(Card.Suit.Spade, 5, true);
        }

        CheckAllCard(player1);
    }

    private void AddCardToPlayer(Card.Suit suit, int number, bool isVisible)
    {
        // カードオブジェクトを生成
        GameObject cardObj = Instantiate(cardPrefab);

        // スポーン位置に配置（適宜調整）
        int index = player1.hands.Count;
        if (index < cardSpawnPoints.Length)
        {
            cardObj.transform.position = cardSpawnPoints[index].position;
        }

        // Card スクリプトを取得して初期化
        Card card = cardObj.GetComponent<Card>();
        card.Initialize(suit, number, isVisible);

        // 手札に追加
        player1.hands.Add(card);
    }

    private bool CheckAllCard(Player player)
    {
        if (player.hands == null || player.hands.Count == 0)
        {
            Debug.Log("手札がありません。");
            return false;
        }

        Card.Suit firstSuit = player.hands[0].suit;

        foreach (Card card in player.hands)
        {
            if (card.suit != firstSuit)
            {
                return false;
            }
        }

        return true;
    }
}
