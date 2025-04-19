using UnityEngine;

public class Card : MonoBehaviour
{


    // カードのスートの初期値
    public Suit suit = Suit.Spade;

    // カードの数字
    private int _Number;
    public int Number
    {
        get{return _Number;}
        set{_Number = Mathf.Max(1, value);}//数字が1以上になるようにする
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
