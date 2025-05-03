using System.Collections.Generic;
using Tetrage.Models;
using UnityEngine;

namespace Tetrage.UI
{
    public class CardPileView : MonoBehaviour
    {
        // カードモデルと、そのカードのViewの辞書
        private Dictionary<Card, CardView> _views = new();

        [SerializeField] private bool keepWorldPosition = false;

        [SerializeField] private CardPile _model; // CardPileの純粋モデルへの参照
        [SerializeField] private GameObject _cardPrefab; //CardViewのプレハブ

        public void Awake()
        {
            // 現在カードパイルに存在しているCardモデルにCardViewを作成し、紐づける
            foreach (var cm in _model)
            {
                AddView(cm);
            }

            // CardPileモデルでCardが移動されたときのイベントを購読
            _model.CardTransferred += OnCardTransferred;

        }

        private void AddView(Card cardModel)
        {
            var gameObject = Instantiate(_cardPrefab, transform); // CardPileViewのアタッチされているゲームオブジェクトの位置にCardViewオブジェクトを生成
            var cardView = gameObject.GetComponent<CardView>();
            cardView.Initialize(cardModel);
            _views[cardModel] = cardView;
        }

        private void OnCardTransferred(Card card, CardPile from, CardPile to)
        {
            // ビューは既に生成済みとする
            var view = _views[card];
            // 新しい束のCardPileViewを探し、親子付けを付け替える
            var toView = 

            // シーン上の親子関係を、移動先のカードパイルへ移動させる
            transform.SetParent(, keepWorldPosition);
        }
   
    }
}
