using System.Collections.Generic;
using UnityEngine;

namespace Tetrage.Services
{
    /// <summary>
    /// 同一親配下のTransform群について、現在の兄弟順スロット配置を
    /// 指定順へ再割り当てするユーティリティ。
    /// </summary>
    public static class TransformReorderPlacementService
    {
        #region Nested Types

        private readonly struct LocalPose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;
            public readonly int SiblingIndex;

            public LocalPose(Transform transform)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
                Scale = transform.localScale;
                SiblingIndex = transform.GetSiblingIndex();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定したGameObject順に、現在の兄弟順スロット配置を再割り当てする。
        /// </summary>
        /// <param name="orderedObjects">配置先の順序（この順にスロット0,1,2...を割り当てる）</param>
        /// <param name="alsoReorderSiblingIndex">true の場合、兄弟インデックスも同時に並べ替える</param>
        public static bool TryReassignLocalPlacementsBySiblingOrder(
            IReadOnlyList<GameObject> orderedObjects,
            bool alsoReorderSiblingIndex = false)
        {
            if (orderedObjects == null || orderedObjects.Count == 0)
            {
                Debug.LogWarning("TransformReorderPlacementService: orderedObjects が null または空です。");
                return false;
            }

            var transforms = new List<Transform>(orderedObjects.Count);
            for (int i = 0; i < orderedObjects.Count; i++)
            {
                var gameObject = orderedObjects[i];
                if (gameObject == null)
                {
                    Debug.LogWarning($"TransformReorderPlacementService: orderedObjects[{i}] が null です。");
                    return false;
                }

                transforms.Add(gameObject.transform);
            }

            return TryReassignLocalPlacementsBySiblingOrder(transforms, alsoReorderSiblingIndex);
        }

        /// <summary>
        /// 指定したTransform順に、現在の兄弟順スロット配置を再割り当てする。
        /// </summary>
        /// <param name="orderedTransforms">配置先の順序（この順にスロット0,1,2...を割り当てる）</param>
        /// <param name="alsoReorderSiblingIndex">true の場合、兄弟インデックスも同時に並べ替える</param>
        public static bool TryReassignLocalPlacementsBySiblingOrder(
            IReadOnlyList<Transform> orderedTransforms,
            bool alsoReorderSiblingIndex = false)
        {
            if (!ValidateInput(orderedTransforms, out var parent))
            {
                return false;
            }

            // 同一親配下の「現在の兄弟順」を、元スロット配置として固定する。
            var sourceBySibling = new List<Transform>(orderedTransforms);
            sourceBySibling.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

            var snapshot = new List<LocalPose>(sourceBySibling.Count);
            for (int i = 0; i < sourceBySibling.Count; i++)
            {
                snapshot.Add(new LocalPose(sourceBySibling[i]));
            }

            // orderedTransforms[i] に、兄弟順スロット i のローカル配置を割り当てる。
            for (int i = 0; i < orderedTransforms.Count; i++)
            {
                var target = orderedTransforms[i];
                var pose = snapshot[i];

                target.localPosition = pose.Position;
                target.localRotation = pose.Rotation;
                target.localScale = pose.Scale;
            }

            if (alsoReorderSiblingIndex)
            {
                // スロットの並びも一致させたい場合のみ、兄弟インデックスを更新する。
                for (int i = 0; i < orderedTransforms.Count; i++)
                {
                    orderedTransforms[i].SetSiblingIndex(snapshot[i].SiblingIndex);
                }
            }

            return true;
        }

        #endregion

        #region Private Methods

        private static bool ValidateInput(IReadOnlyList<Transform> orderedTransforms, out Transform parent)
        {
            parent = null;

            if (orderedTransforms == null || orderedTransforms.Count == 0)
            {
                Debug.LogWarning("TransformReorderPlacementService: orderedTransforms が null または空です。");
                return false;
            }

            var seen = new HashSet<Transform>();
            for (int i = 0; i < orderedTransforms.Count; i++)
            {
                var transform = orderedTransforms[i];
                if (transform == null)
                {
                    Debug.LogWarning($"TransformReorderPlacementService: orderedTransforms[{i}] が null です。");
                    return false;
                }

                // 同一要素が重複するとスロット割り当てが不定になるため拒否する。
                if (!seen.Add(transform))
                {
                    Debug.LogWarning($"TransformReorderPlacementService: 重複した Transform が指定されました ({transform.name})。");
                    return false;
                }

                if (i == 0)
                {
                    parent = transform.parent;
                    continue;
                }

                // 兄弟順スロットを使うため、同一親配下であることを前提にする。
                if (transform.parent != parent)
                {
                    Debug.LogWarning("TransformReorderPlacementService: すべて同一の parent を持つ Transform を指定してください。");
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
