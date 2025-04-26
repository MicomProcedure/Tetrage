using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Tetrage.Models;

public class Check : MonoBehaviour
{
    public GameObject cardPrefab;
    public GameObject CheckButton;
    

    //チェックボタンが押されたかどうか
    public bool CheckButtonPushed = false;

    [Header("デバック用")]
    public bool DebugMode = true;
    private Player player1;
    private Player player2;
    public Transform[] MyCardSpawn; //自分のカード配置場所を設定
    public Transform[] OtherCardSpawn; //相手のカード配置場所
    public Transform OtherTargetCardSpawn; //相手のターゲットカード配置場所

    void Start()
    {
        if (DebugMode)
        {
            //自分
            player1 = new Player(1);
            //相手
            player2 = new Player(2);

            //最初のターゲットカードを簡易的に設定する
            GameObject cardObj = Instantiate(cardPrefab);
            cardObj.transform.position = OtherTargetCardSpawn.position;
            //カードの内容を設定
            Card Player2TargetCard = cardObj.GetComponent<Card>();
            Player2TargetCard.Initialize(Card.Suit.Spade,1,false);
            Player2TargetCard.owner = player2; //持ち主を設定
            Player2TargetCard.canFlip = false; //ターゲットカードは勝手にめくれないようにする
            player2.target = Player2TargetCard;
            

            //自分のカードを追加
            AddCardToPlayer(player1,MyCardSpawn,Card.Suit.Spade, 7, true);
            AddCardToPlayer(player1,MyCardSpawn,Card.Suit.Spade, 3, true);
            AddCardToPlayer(player1,MyCardSpawn,Card.Suit.Spade, 5, true);

            //相手のカードを追加
            AddCardToPlayer(player2,OtherCardSpawn,Card.Suit.Heart, 12, false);
            AddCardToPlayer(player2,OtherCardSpawn,Card.Suit.Spade, 1, false);
            AddCardToPlayer(player2,OtherCardSpawn,Card.Suit.Club, 13, false);
        }        
    }

    void Update()
    {
        //自分の手札が一致しているかどうか取得
        if(CheckAllCardSame(player1)){
            if(CheckButtonPushed == false){
                CheckButton.SetActive(true);
            }
        }

        //チェックボタンが押されたら相手を選択させる→本来のスートと一致しているかを返す
        if(CheckButtonPushed){
            if(player1.hands[0].suit == player2.target.suit){
                player2.target.canFlip = true;
                Debug.Log("一致");
            }
            else{
                Debug.Log("不一致");
            }
        }
    }

    private void AddCardToPlayer(Player player,Transform[] CardPos,Card.Suit suit, int number, bool isVisible)
    {
        // カードオブジェクトを生成
        GameObject cardObj = Instantiate(cardPrefab);

        // スポーン位置に配置（適宜調整）
        int index = player.hands.Count;
        if (index < CardPos.Length)
        {
            cardObj.transform.position = CardPos[index].position;
        }

        // Card スクリプトを取得して初期化
        Card card = cardObj.GetComponent<Card>();
        card.Initialize(suit, number, isVisible);

        // 手札に追加
        player.hands.Add(card);
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
