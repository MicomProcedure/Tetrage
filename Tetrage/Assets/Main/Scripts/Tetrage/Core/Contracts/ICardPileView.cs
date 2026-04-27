using System;
using Tetrage.UI;
using UnityEngine;
using Tetrage.Core.DTO;

namespace Tetrage.Core.Contracts
{
    public interface ICardPileView
    {
        /// <summary>
        /// このビューが破棄されたときに発行されるイベント。
        /// Presenter はここを購読し Dispose を呼びます。
        /// </summary>
        public event Action Destroyed;

        /// <summary>カード表示用ViewをこのPileViewの子に設定します。</summary>
        /// <param name="cardView">追加するカード表示用View</param>
        /// <param name="animate">アニメーションを使用するかどうか</param>
        public void AddCardView(CardView cardView, bool animate = true);

        /// <summary>カード表示用ViewをこのPileViewから外します。</summary>
        public void RemoveCardView(CardView cardView);

        /// <summary>オブジェクトの名前を変更します。</summary>
        public void RenameObject(string newName);

        /// <summary>ビューを更新します。</summary>
        public void RefreshView();

        public void SetCardViewLayoutInfo(float pileWidth, float minSpacing, float maxSpacing, Vector3 positionOffset);

        public void SetCardViewLayoutInfo(CardPileLayoutSettings layoutSettings);

    }
}
