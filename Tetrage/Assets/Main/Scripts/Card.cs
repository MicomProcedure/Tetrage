using UnityEngine;
using UnityEngine.EventSystems;


public class Card : MonoBehaviour, IPointerClickHandler
{

    public Suit suit;

    // カードの表示状態
    public bool isVisible = true;
    public bool canFlip = true;

    public Card(Suit suit, int number, bool isVisible)
    {
        this.suit = suit;
        this.number = number;
        this.isVisible = isVisible;
    }

    // カードの数字
    private int _Number;
    public int number
    {
        get{return _Number;}
        set{_Number = Mathf.Max(1, value);}//数字が1以上になるようにする
    }

    public void Flip()
    {
        isVisible = !isVisible;
        Debug.Log($"今は {(isVisible ? "表" : "裏")}.");
    }

    // 裏返し可能の場合クリックされたらカードを裏返す
    public void OnPointerClick(PointerEventData eventData)
    {
        if(canFlip)
        {
            Flip();
        }
    }
}
