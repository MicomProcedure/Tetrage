using UnityEngine;
using System;
using Tetrage.Core.Contracts;
using System.Collections.Generic;
using Tetrage.Core.Constants;
using System.Linq;

namespace Tetrage.UI
{
    public class BasicCardPileView : MonoBehaviour, ICardPileView
    {
        /// <summary>
        /// このビューが破棄されたときに発行されるイベント。
        /// Presenter はここを購読し Dispose を呼びます。
        /// </summary>
        public event Action Destroyed;
        /// <summary>
        /// このcardpileviewObjectのシーン上の子であるカード表示用ViewのTransformリスト
        /// </summary>
        protected List<Transform> _cardViewObjects = new List<Transform>();
        /// <summary>
        /// このcardpileviewObjectのシーン上の子であるカード表示用Viewを置いておく幅
        /// </summary>
        [SerializeField] protected float _cardPileWidth = InGameConsts.DEFAULT_CARD_PILE_WIDTH;
        public float CardPileWidth => _cardPileWidth;
        [SerializeField] protected float _cardViewMinSpacing = InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING;
        public float CardViewMinSpacing => _cardViewMinSpacing;
        [SerializeField] protected float _cardViewMaxSpacing = InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING;
        public float CardViewMaxSpacing => _cardViewMaxSpacing;
        [SerializeField] protected Vector3 _cardViewPositionOffset = InGameConsts.DEFAULT_CARD_VIEW_POSITION_OFFSET;
        public Vector3 CardViewPositionOffset => _cardViewPositionOffset;

        private void OnDestroy()
        {
            Destroyed?.Invoke();
        }

        /// <summary>カード表示用ViewをこのPileViewの子に設定します。</summary>
        public void AddCardView(CardView cardView)
        {
            Debug.Log($"{this.GetType().Name}: AddCardView {cardView.name}");
            cardView.transform.SetParent(transform, worldPositionStays: false);
            // 子オブジェクトが増えたのでレイアウト更新
            RefreshView();
        }

        /// <summary>カード表示用ViewをこのPileViewから外します。</summary>
        public void RemoveCardView(CardView cardView)
        {
            cardView.transform.SetParent(null);
            // 子オブジェクトが減ったのでレイアウト更新
            RefreshView();
        }

        /// <summary>オブジェクトの名前を変更します。</summary>
        public void RenameObject(string newName) => gameObject.name = "PileView." + newName;

        /// <summary>ビューを更新します。</summary>
        public virtual void RefreshView()
        {
            UpdateCardViewLayout();
        }

        /// <summary>カード表示用ViewのTransformを調整します。</summary>
        protected virtual void UpdateCardViewLayout()
        {
            // カード表示用ViewのTransformリストをクリア
            _cardViewObjects.Clear();
            // カード表示用ViewのTransformリストをこのCardPileViewの子オブジェクトから取得
            foreach (Transform child in transform)
            {
                _cardViewObjects.Add(child);
            }

            // カード表示用Viewの数を取得
            var cardViewCount = _cardViewObjects.Count;

            if (cardViewCount == 0) return; // カード表示用Viewがない場合は何もしない（関数を終了）

            // カード表示用Viewの幅を計算
            var cardViewWidth = _cardPileWidth / cardViewCount;
            // カード表示用Viewの幅を 最小値<=幅<=最大値 に制限
            float spacing = Mathf.Clamp(cardViewWidth, _cardViewMinSpacing, _cardViewMaxSpacing);
            // カード表示用Viewの中心からのオフセットを計算
            float centerOffset = spacing*(cardViewCount - 1) / 2f;

            // カード表示用ViewのTransformを調整
            for (int i = 0; i < cardViewCount; i++)
            {
                Transform cardView = _cardViewObjects[i];
                float x = i * spacing - centerOffset;
                cardView.localPosition = new Vector3(x, 0, 0) + _cardViewPositionOffset;
            }
        }

        /// <summary>カード表示用Viewを置いておく幅と最小間隔を設定します。</summary>
        /// <param name="cardPileWidth">カード表示用Viewを置いておく幅</param>
        /// <param name="cardViewMinSpacing">カード表示用Viewの最小間隔</param>
        /// <param name="cardViewMaxSpacing">カード表示用Viewの最大間隔</param>
        /// <param name="cardViewPositionOffset">カード表示用Viewの中心からのオフセット</param>
        protected virtual void SetCardViewLayoutInfo(
        float cardPileWidth = InGameConsts.DEFAULT_CARD_PILE_WIDTH,
        float cardViewMinSpacing = InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING,
        float cardViewMaxSpacing = InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING,
        Vector3 cardViewPositionOffset = default)
        {
            _cardPileWidth = cardPileWidth;
            _cardViewMinSpacing = cardViewMinSpacing;
            _cardViewMaxSpacing = cardViewMaxSpacing;
            _cardViewPositionOffset = cardViewPositionOffset;
        }

        // 子 Transform に増減があった場合にも自動でレイアウト更新
        protected virtual void OnTransformChildrenChanged()
        {
            RefreshView();
        }
    }
}
