using UnityEngine;

public class Card : MonoBehaviour
{
    // カードのスートの定義
    public enum Suit
    {
        Spade,
        Heart,
        Diamond,
        Club
    }

    // カードのスートの初期値
    public Suit suit = Suit.Spade;

    // カードの数字
    private int _number;
    public int number
    {
        get{return _number;}
        set{_number = Mathf.Max(1, value);}//数字が1以上になるようにする
    }

    // カードの表示状態
    public bool isVisible = false;

   void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
