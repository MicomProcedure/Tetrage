using UnityEngine;
using System;
using Tetrage.Core.Contracts;
using System.Collections.Generic;
using Tetrage.Core.Constants;
using Tetrage.Core.DTO;
using Cysharp.Threading.Tasks;
using Tetrage.Animations;

namespace Tetrage.UI
{
    public class BasicCardPileView : MonoBehaviour, ICardPileView
    {
        #region イベント
        /// <summary>
        /// このビューが破棄されたときに発行されるイベント。
        /// Presenter はここを購読し Dispose を呼びます。
        /// </summary>
        public event Action Destroyed;
        #endregion

        #region レイアウト設定 / フィールド
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
        #endregion

        #region Unityライフサイクル
        private void OnDestroy()
        {
            Destroyed?.Invoke();
        }
        #endregion

        #region Public API（非override）
        /// <summary>カード表示用ViewをこのPileViewの子に設定します。</summary>
        public async void AddCardView(CardView cardView)
        {
            // 移動元の位置を保存
            Vector3 startPosition = cardView.transform.position;
            
            // 親を変更（worldPositionStaysをtrueにして位置を保持）
            cardView.transform.SetParent(transform, worldPositionStays: true);
            
            // レイアウト更新（最終位置を計算）
            RefreshView();
            
            // 1フレーム待機してレイアウトが確定するのを待つ
            await UniTask.Yield();
            Vector3 endPosition = cardView.transform.localPosition;
            
            // 元の位置に戻す
            cardView.transform.position = startPosition;
            
            // アニメーション実行
            await AnimationHelper.MoveToWithEasing(
                cardView.gameObject,
                startPosition,
                cardView.transform.parent.TransformPoint(endPosition), // ローカル→ワールド座標変換
                0.5f
            );
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
        #endregion

        #region Public API（override可）
        /// <summary>ビューを更新します。</summary>
        public virtual void RefreshView()
        {
            LayoutCardView();
        }
        #endregion

        #region Unityライフサイクル（override可）
        // 子 Transform に増減があった場合にも自動でレイアウト更新
        protected virtual void OnTransformChildrenChanged()
        {
            RefreshView();
        }
        #endregion

        #region レイアウトAPI（override可）
        /// <summary>カード表示用ViewのTransformを調整します。カードの表示位置を変更する場合はオーバーライドしてください。</summary>
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
        #endregion

        #region ヘルパー（非override）
        /// <summary>間隔と中心オフセットを算出</summary>
        protected void CalculateSpacingAndOffset(int totalCount, out float spacing, out float centerOffset)
        {
            float raw = _cardPileWidth / totalCount;
            spacing = Mathf.Clamp(raw, _cardViewMinSpacing, _cardViewMaxSpacing);
            centerOffset = spacing * (totalCount - 1) / 2f;
        }
        #endregion

        #region レイアウトAPI（override可）
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
        #endregion

        #region レイアウト設定API（非override）
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
        #endregion

        #region レイアウトAPI（override可）
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
        #endregion

    }
}
