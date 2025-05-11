using System;
using Tetrage.UI;
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
        public void AddCardView(CardView cardView);

        /// <summary>カード表示用ViewをこのPileViewから外します。</summary>
        public void RemoveCardView(CardView cardView);

        /// <summary>オブジェクトの名前を変更します。</summary>
        public void RenameObject(string newName);

        /// <summary>ビューを更新します。</summary>
        public void RefreshView();

    }
}
