using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Tetrage.Animations;
using Tetrage.Components;
using Tetrage.Core.Contracts;
using UnityEngine;

namespace Tetrage.UI
{
    public class TmpCardPileView : BasicCardPileView
    {
        #region Inspector
        [Header("Tmp Pile Custom Settings")]
        [SerializeField] private PositionConfig _tmpCardPositionConfig;
        #endregion

        #region Runtime Cache
        private readonly Dictionary<CardView, Vector3> _defaultCardScaleMap = new();
        public IPositionConfig TmpCardPositionConfig => _tmpCardPositionConfig;
        #endregion

        #region Public API
        public override void RefreshView()
        {
            base.RefreshView();
        }
        #endregion

        #region Unity Lifecycle

        #endregion

        // void FixedUpdate(){
        //     Debug.Log($"<color=black>{name}: FixedUpdate: transform.position=" + transform.position);
        // }

        #region Override: Card In/Out
        /// <summary>Tmpに入るカードは拡大して表示します。</summary>
        public override void AddCardView(CardView cardView, bool animate = true)
        {
            if (cardView == null)
            {
                return;
            }

            // 親変更前のワールド位置を保持します。
            Vector3 startWorldPosition = cardView.transform.position;

            // TmpCardPileView配下へ移動しつつワールド位置を維持します。
            cardView.transform.SetParent(transform, worldPositionStays: true);

            // Tmp配下での基準スケールを保存します（親スケール差の影響を排除）。
            if (!_defaultCardScaleMap.ContainsKey(cardView))
            {
                _defaultCardScaleMap[cardView] = cardView.transform.localScale;
            }

            // SetParent 直後に OnTransformChildrenChanged から RefreshView が走り、レイアウト先の座標が付く。
            // ここで await するとその姿が1フレーム描画されチラつき、直後に開始位置へ戻すと二重移動に見える。
            // 同一フレーム内で終了ワールド座標だけ確定し、すぐ開始位置へ戻す。
            Vector3 endWorldPosition = cardView.transform.position;
            cardView.transform.position = startWorldPosition;

            // アニメーション実行
            if (animate)
            {
                // 位置移動と拡大を同時に適用
                AnimationHelper.MoveToWithEasing(
                        cardView.gameObject,
                        startWorldPosition,
                        endWorldPosition,
                        0.5f
                    ).Forget();
                
                cardView.ScaleUpAnimation();

            }
            else
            {
                cardView.transform.position = endWorldPosition;
            }
        }

        /// <summary>Tmpから出るカードは元サイズへ戻します。</summary>
        public override void RemoveCardView(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }
            cardView.ScaleDownAnimation();  // 元のサイズに戻すアニメーションを再生

            cardView.transform.SetParent(null);
            RefreshView();
        }
        #endregion

        #region Override: Layout

        protected override void SetCardViewPositionByIndex(int index, float spacing, float centerOffset)
        {
            if (TryGetConfiguredPosition(index, out var configuredWorldPosition))
            {
                // PositionConfig使用時はワールド座標を直接適用
                _cardViewObjects[index].position = configuredWorldPosition;
                return;
            }

            base.SetCardViewPositionByIndex(index, spacing, centerOffset);
        }
        #endregion

        #region Private Methods
        private bool TryGetConfiguredPosition(int index, out Vector3 worldPosition)
        {
            worldPosition = default;
            if (TmpCardPositionConfig == null || TmpCardPositionConfig.Position == null)
            {
                Debug.LogWarning($"{name}: PositionConfig が null または Position が null です。");
                return false;
            }

            if (index < 0 || index >= TmpCardPositionConfig.Position.Count)
            {
                Debug.LogWarning($"{name}: Index {index} が PositionConfig.Position の範囲外です。");
                return false;
            }

            // TmpCardPileViewのワールド座標を引いて、TmpCardPositionConfigのローカル座標を取得し、それをワールド座標として代入
            worldPosition = TmpCardPositionConfig.Position[index] - transform.position;
            return true;
        }

        #endregion
    }
}

