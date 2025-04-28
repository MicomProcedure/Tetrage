using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Tetrage.Models;

public class Check : MonoBehaviour
{
    //public GameObject cardPrefab;
    //public GameObject CheckButton;
    

    ////チェックボタンが押されたかどうか
    //public bool CheckButtonPushed = false;

    //[Header("デバック用")]
    //public bool DebugMode = true;
    //private Player player1;
    //private Player player2;
    //private Player player3;
    //private Player player4;
    //public Transform[] MyCardSpawn; //自分のカード配置場所を設定
    //public Transform[] OtherCardSpawn; //相手のカード配置場所
    //public Transform Other2Pos; //相手のターゲットカード配置場所
    //public Transform Other3Pos; //相手のターゲットカード配置場所
    //public Transform Other4Pos; //相手のターゲットカード配置場所

    //void Start()
    //{
    //    if (DebugMode)
    //    {
    //        //自分
    //        player1 = new Player(1);
    //        //相手
    //        player2 = new Player(2);
    //        player3 = new Player(3);
    //        player4 = new Player(4);
            
    //        //相手のターゲットカードを簡易的に決める
    //        GenerateOthersTargetCard(player2,Other2Pos,Card.Suit.Spade,2,false);
    //        GenerateOthersTargetCard(player3,Other3Pos,Card.Suit.Club,2,false);
    //        GenerateOthersTargetCard(player4,Other4Pos,Card.Suit.Diamond,1,false);

    //        //自分のカードを追加
    //        AddCardToPlayer(player1,MyCardSpawn,Card.Suit.Spade, 7, true);
    //        AddCardToPlayer(player1,MyCardSpawn,Card.Suit.Spade, 3, true);
    //        AddCardToPlayer(player1,MyCardSpawn,Card.Suit.Spade, 5, true);

    //        //相手のカードを追加
    //        AddCardToPlayer(player2,OtherCardSpawn,Card.Suit.Heart, 12, false);
    //        AddCardToPlayer(player2,OtherCardSpawn,Card.Suit.Spade, 1, false);
    //        AddCardToPlayer(player2,OtherCardSpawn,Card.Suit.Club, 13, false);
    //    }        
    //}

    //void Update()
    //{
    //    //自分の手札が一致しているかどうか取得
    //    if(CheckAllCardSame(player1)){
    //        if(CheckButtonPushed == false){
    //            CheckButton.SetActive(true);
    //        }
    //    }

    //    //チェックボタンが押されたら相手を選択させる→本来のスートと一致しているかを返す
    //    if(CheckButtonPushed){
    //        //自分が揃えたカードとタップしたターゲットカードが一致しているか取得
    //        if(GetTargetCardOwner() != null){
    //            if(player1.hands[0].suit == GetTargetCardOwner().target.suit){
    //                Debug.Log("一致");
    //                GetTargetCardOwner().target.canFlip = true;
    //            }
    //            else{
    //                Debug.Log("不一致");
    //            }
    //        }
    //    }
    //}

    //private Player? GetTargetCardOwner()
    //{
    //    Vector2 tapPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    RaycastHit2D hit = Physics2D.Raycast(tapPosition, Vector2.zero);

    //    if (hit.collider != null)
    //    {
    //        Card tappedCard = hit.collider.GetComponent<Card>();

    //        if (tappedCard != null && tappedCard.owner != null && tappedCard.owner.target != null)
    //        {
    //            return tappedCard.owner;
    //        }
    //    }

    //    return null; // nullable型だからOK
    //}


    //private void AddCardToPlayer(Player player,Transform[] CardPos,Card.Suit suit, int number, bool isVisible)
    //{
    //    // カードオブジェクトを生成
    //    GameObject cardObj = Instantiate(cardPrefab);

    //    // スポーン位置に配置（適宜調整）
    //    int index = player.hands.Count;
    //    if (index < CardPos.Length)
    //    {
    //        cardObj.transform.position = CardPos[index].position;
    //    }

    //    // Card スクリプトを取得して初期化
    //    Card card = cardObj.GetComponent<Card>();
    //    card.Initialize(suit, number, isVisible);

    //    // 手札に追加
    //    player.hands.Add(card);
    //}

    //private bool CheckAllCardSame(Player player)
    //{
    //    if (player.hands == null || player.hands.Count == 0)
    //    {
    //        Debug.Log("手札がありません。");
    //        return false;
    //    }

    //    Card.Suit firstSuit = player.hands[0].suit;

    //    foreach (Card card in player.hands)
    //    {
    //        if (card.suit != firstSuit)
    //        {
    //            return false;
    //        }
    //    }

    //    return true;
    //}

    //void GenerateOthersTargetCard(Player player,Transform CardPos,Card.Suit suit, int number, bool isVisible)
    //{
    //    //最初のターゲットカードを簡易的に設定する
    //    GameObject cardObj = Instantiate(cardPrefab);
    //    cardObj.transform.position = CardPos.position;
    //    //カードの内容を設定
    //    Card PlayerCard = cardObj.GetComponent<Card>();
    //    PlayerCard.Initialize(suit,number,isVisible);
    //    PlayerCard.owner = player; //持ち主を設定
    //    PlayerCard.canFlip = false; //ターゲットカードは勝手にめくれないようにする
    //    player.target = PlayerCard;
    //}

    ////チェックボタンの機能
    //public void DoCheck()
    //{
    //    CheckButton.SetActive(false);
    //    CheckButtonPushed = true;
    //}
}
