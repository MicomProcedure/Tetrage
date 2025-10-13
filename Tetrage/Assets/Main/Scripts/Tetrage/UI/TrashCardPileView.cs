using UnityEngine;
using Tetrage.Core;
using System.Collections.Generic;

namespace Tetrage.UI
{
    public class TrashCardPileView : BasicCardPileView
    {
        #region 設定
        [Header("Trash Layout Settings")]
        [SerializeField] private float _minZRotation = -30f; // 新規カードの最小Z回転
        [SerializeField] private float _maxZRotation = 30f;  // 新規カードの最大Z回転
        #endregion

        #region 内部状態
        // 既知の子Transformを記録して追加/削除を検知する
        private Dictionary<int, Transform> _knownChildren = new Dictionary<int, Transform>();
        #endregion

        #region Unityライフサイクル
        private void Awake()
        {
            InitializeKnownChildren();
        }
        #endregion

        public override void RefreshView()
        {
            base.RefreshView();
        }


        protected override void LayoutCardView()    // 重ねて表示をする。直近の1番上のカードを最前面に表示する。
        {
            // カード表示用ViewのTransformリストを更新
            UpdateCardViewObjects();
            int count = _cardViewObjects.Count;
            if (count == 0) return;

            SetCardViewPositions(count, 0, 0);
        }

        // トラッシュの場合はカード表示用Viewの座標を0,0,0にする。直近の1番上のカードを最前面に表示する。
        protected override void SetCardViewPositionByIndex(int index, float spacing, float centerOffset)
        {
            _cardViewObjects[index].localPosition = new Vector3(0, 0, 0) + _cardViewPositionOffset;
        }

        #region 追加検知（override）
        // 子の追加・削除があれば呼ばれる。新規追加の子だけ回転をランダム化する
        protected override void OnTransformChildrenChanged()
        {
            // 現在の子をマップ化しつつ、追加分は回転適用
            var currentMap = new Dictionary<int, Transform>();
            foreach (Transform child in transform)
            {
                int id = child.GetInstanceID();
                if (!_knownChildren.ContainsKey(id))
                {
                    ApplyRandomZRotation(child);
                }
                currentMap[id] = child;
            }

            // 削除された子を検出し、回転をリセット
            foreach (var kv in _knownChildren)
            {
                if (!currentMap.ContainsKey(kv.Key))
                {
                    ResetZRotation(kv.Value);
                }
            }

            _knownChildren = currentMap;

            base.OnTransformChildrenChanged();
        }
        #endregion

        #region ヘルパー
        private void InitializeKnownChildren()
        {
            _knownChildren.Clear();
            foreach (Transform child in transform)
            {
                _knownChildren[child.GetInstanceID()] = child;
            }
        }

        private void ApplyRandomZRotation(Transform t)
        {
            float angle = Random.Range(_minZRotation, _maxZRotation);
            var euler = t.localEulerAngles;
            euler.z = angle;
            t.localRotation = Quaternion.Euler(euler);
        }

        private void ResetZRotation(Transform t)
        {
            if (t == null) return;
            var euler = t.localEulerAngles;
            euler.z = 0f;
            t.localRotation = Quaternion.Euler(euler);
        }
        #endregion
    }
}
