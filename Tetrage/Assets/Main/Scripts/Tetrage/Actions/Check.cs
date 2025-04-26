using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Tetrage.Models;

public class Check : MonoBehaviour
{
    private Player player1;
    public GameObject cardPrefab;
    public GameObject CheckButton;
    

    //チェックボタンが押されたかどうか
    public bool CheckButtonPushed = false;

    [Header("デバック用")]
    public bool DebugMode = true;
    public Transform[] MyCardSpawn; //自分のカード配置場所を設定
    public Transform[] OtherCardSpawn; //相手のカード配置場所

    void Start()
    {
        player1 = new Player();

        if (DebugMode)
        {
            AddCardToPlayer(Card.Suit.Spade, 7, true);
            AddCardToPlayer(Card.Suit.Spade, 3, true);
            AddCardToPlayer(Card.Suit.Spade, 5, true);
        }

        //自分の手札が一致しているかどうか取得
        if(CheckAllCardSame(player1)){
            //手札が一致していたらチェックボタンを出す
            CheckButton.SetActive(true);
        }
        
    }

    void Update()
    {
        //チェックボタンが押されたら相手のカードを選択させるフェーズに入る
        if(CheckButtonPushed){

        }
    }

    private void AddCardToPlayer(Card.Suit suit, int number, bool isVisible)
    {
        // カードオブジェクトを生成
        GameObject cardObj = Instantiate(cardPrefab);

        // スポーン位置に配置（適宜調整）
        int index = player1.hands.Count;
        if (index < MyCardSpawn.Length)
        {
            cardObj.transform.position = MyCardSpawn[index].position;
        }

        // Card スクリプトを取得して初期化
        Card card = cardObj.GetComponent<Card>();
        card.Initialize(suit, number, isVisible);

        // 手札に追加
        player1.hands.Add(card);
    }

    private bool CheckAllCardSame(Player player)
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

    //チェックボタンの機能
    public void DoCheck()
    {
        CheckButton.SetActive(false);
        CheckButtonPushed = true;
    }
}
