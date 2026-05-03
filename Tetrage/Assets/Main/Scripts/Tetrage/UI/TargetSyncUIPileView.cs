using Tetrage.Core.Ids;
using UnityEngine;

namespace Tetrage.UI
{
    /// <summary>
    /// PlayerUI内のマーカー位置へTargetカード山ビューを同期する。
    /// </summary>
    public sealed class TargetSyncUIPileView : BasicCardPileView
    {
        #region Private Fields
        
        private PlayerId? _ownerPlayerId;

        #endregion

        void FixedUpdate(){
            Debug.Log($"TargetSyncUIPileView(Id={OwnerPlayerId}): FixedUpdate: transform.position=" + transform.position);
        }

        #region Public API

        public PlayerId? OwnerPlayerId => _ownerPlayerId;


        /// <summary>
        /// 同期対象のPlayerIdを設定する。
        /// </summary>
        public void SetOwnerPlayerId(PlayerId playerId)
        {
            _ownerPlayerId = playerId;
        }

        /// <summary>
        /// 設定済みのOwnerPlayerIdを取得する。
        /// </summary>
        public bool TryGetOwnerPlayerId(out PlayerId? playerId)
        {
            playerId = _ownerPlayerId;
            return _ownerPlayerId != null;
 
        }

        /// <summary>
        /// 同期先マーカー（ワールド座標が既に正しい想定）へ自身の位置を合わせる。
        /// </summary>
        public void RefreshPosition(Transform marker)
        {
            if (marker == null)
            {
                Debug.LogWarning("TargetSyncUIPileView: marker is null");
                return;
            }

            if (transform.position != marker.position) Debug.Log($"TargetSyncUIPileView(Id={OwnerPlayerId}): RefreshPosition: transform.position=" + transform.position + " marker.position=" + marker.position + "で、位置が異なるため更新されることを期待");

            // marker は PlayerUI.targetMarkerTransform 等。射影は呼び出し側で済んでいる前提のため位置のみ複製する（山の向きは現状維持）。
            transform.position = marker.position;

            Debug.Log("<color=yellow>TargetSyncUIPileView: RefreshPosition: transform.position=" + transform.position + " marker.position=" + marker.position);
            RefreshView();
        }

        #endregion

        #region Override Methods

        public override async void AddCardView(CardView cardView, bool animate = true){
            base.AddCardView(cardView, false);
            Debug.Log($"<color=green>TargetSyncUIPileView(Id={OwnerPlayerId}): AddCardView: transform.position=" + transform.position);
        }

        #endregion
        }
}
