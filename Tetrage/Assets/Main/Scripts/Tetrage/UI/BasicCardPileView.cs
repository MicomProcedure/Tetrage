using UnityEngine;
using System;
using Tetrage.Core.Contracts;
using System.Collections.Generic;
using Tetrage.Core.Constants;
using System.Linq;
using Tetrage.Core.Settings;

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
            LayoutCardView();
        }

        // 子 Transform に増減があった場合にも自動でレイアウト更新
        protected virtual void OnTransformChildrenChanged()
        {
            RefreshView();
        }

        /// <summary>カード表示用ViewのTransformを調整します。</summary>
        protected virtual void LayoutCardView()
        {
            UpdateCardViewObjects();
            int count = _cardViewObjects.Count;
            if (count == 0) return;

            // ① 共通情報を一度だけ算出
            float spacing, centerOffset;
            CalculateSpacingAndOffset(count, out spacing, out centerOffset);

            // ② 各カードの X 座標を純粋関数で得る
            SetCardViewPositions(count, spacing, centerOffset);
        }

        /// <summary>間隔と中心オフセットを算出</summary>
        protected void CalculateSpacingAndOffset(int totalCount, out float spacing, out float centerOffset)
        {
            float raw = _cardPileWidth / totalCount;
            spacing = Mathf.Clamp(raw, _cardViewMinSpacing, _cardViewMaxSpacing);
            centerOffset = spacing * (totalCount - 1) / 2f;
        }

        /// <summary>指定した回数分カード表示用Viewの座標を設定</summary>
        protected virtual void SetCardViewPositions(int count, float spacing, float centerOffset)
        {
            for (int i = 0; i < count; i++)
            {
                SetCardViewPositionByIndex(i, spacing, centerOffset);
            }
        }

        /// <summary>カード表示用Viewの座標を設定</summary>
        protected virtual void SetCardViewPositionByIndex(int index, float spacing, float centerOffset)
        {
            float x = index * spacing - centerOffset;
            _cardViewObjects[index].localPosition = new Vector3(x, 0, 0) + _cardViewPositionOffset;
        }

        /// <summary>カード表示用Viewを置いておく幅と最小間隔を設定</summary>
        /// <param name="cardPileWidth">カード表示用Viewを置いておく幅</param>
        /// <param name="cardViewMinSpacing">カード表示用Viewの最小間隔</param>
        /// <param name="cardViewMaxSpacing">カード表示用Viewの最大間隔</param>
        /// <param name="cardViewPositionOffset">カード表示用Viewの中心からのオフセット</param>
        public void SetCardViewLayoutInfo(
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

        public void SetCardViewLayoutInfo(CardPileLayoutSettings layoutSettings)
        {
            _cardPileWidth = layoutSettings.PileWidth;
            _cardViewMinSpacing = layoutSettings.MinSpacing;
            _cardViewMaxSpacing = layoutSettings.MaxSpacing;
            _cardViewPositionOffset = layoutSettings.PositionOffset;
        }

        /// <summary>カード表示用ViewのTransformリストを子オブジェクト子オブジェクトから取得</summary>
        protected virtual void UpdateCardViewObjects()
        {
            // カード表示用ViewのTransformリストをクリア
            _cardViewObjects.Clear();
            // カード表示用ViewのTransformリストをこのCardPileViewの子オブジェクトから取得
            foreach (Transform child in transform)
            {
                _cardViewObjects.Add(child);
            }
        }

    }
}
