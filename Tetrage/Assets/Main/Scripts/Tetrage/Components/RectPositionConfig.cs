using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;
using UnityEngine;

namespace Tetrage.Components
{
    /// <summary>
    /// UI用RectTransformのアンカー位置と回転を位置設定として提供する。
    /// </summary>
    public class RectPositionConfig : MonoBehaviour, IPositionConfig
    {
        #region Serialized Fields

        [SerializeField]
        [Tooltip("UI配置に使用するRectTransformのリスト")]
        private List<RectTransform> positions;

        [Header("ビジュアル表示設定")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showLabels = true;
        [SerializeField] private bool showConnectionLines = true;
        [SerializeField] private bool showStatistics = false;

        [Header("表示色設定")]
        [SerializeField] private Color markerColor = Color.cyan;
        [SerializeField] private Color selectedMarkerColor = Color.green;
        [SerializeField] private Color connectionLineColor = Color.white;

        [Header("表示サイズ設定")]
        [SerializeField] [Range(10.0f, 200.0f)] private float markerWidth = 60.0f;
        [SerializeField] [Range(10.0f, 200.0f)] private float markerHeight = 36.0f;
        [SerializeField] [Range(10.0f, 240.0f)] private float selectedMarkerWidth = 80.0f;
        [SerializeField] [Range(10.0f, 240.0f)] private float selectedMarkerHeight = 48.0f;

        #endregion

        #region Properties

        public List<Vector3> Position => positions == null
            ? new List<Vector3>()
            : positions
                .Where(p => p != null)
                .Select(GetLocalPosition)
                .ToList();

        public List<Quaternion> Rotation => positions == null
            ? new List<Quaternion>()
            : positions
                .Where(p => p != null)
                .Select(p => p.localRotation)
                .ToList();

        #endregion

        #region Public Methods

        public bool ValidateConfig(int requiredCount)
        {
            return positions != null
                && positions.Count == requiredCount
                && positions.All(p => p != null)
                && Rotation.Count == requiredCount;
        }

        /// <summary>
        /// 全ての表示をオンにする。
        /// </summary>
        [ContextMenu("全ての表示をオン")]
        public void EnableAllVisuals()
        {
            showGizmos = true;
            showLabels = true;
            showConnectionLines = true;
            showStatistics = true;
        }

        /// <summary>
        /// 全ての表示をオフにする。
        /// </summary>
        [ContextMenu("全ての表示をオフ")]
        public void DisableAllVisuals()
        {
            showGizmos = false;
            showLabels = false;
            showConnectionLines = false;
            showStatistics = false;
        }

        /// <summary>
        /// デフォルト色に戻す。
        /// </summary>
        [ContextMenu("デフォルト色に戻す")]
        public void ResetToDefaultColors()
        {
            markerColor = Color.cyan;
            selectedMarkerColor = Color.green;
            connectionLineColor = Color.white;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake(){
            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// Inspector用デバッグ表示を常に描画する。
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos || positions == null || positions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                RectTransform rectTransform = positions[i];
                if (rectTransform == null)
                {
                    continue;
                }

                Vector3 center = GetGizmoCenter(rectTransform);

                Gizmos.color = markerColor;
                Gizmos.DrawWireCube(center, new Vector3(markerWidth, markerHeight, 1.0f));

                if (showLabels)
                {
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(center + Vector3.up * (markerHeight * 0.5f + 12.0f), $"Rect {i + 1}");
#endif
                }

                if (showConnectionLines && i < positions.Count - 1 && positions[i + 1] != null)
                {
                    Gizmos.color = connectionLineColor;
                    Gizmos.DrawLine(center, GetGizmoCenter(positions[i + 1]));
                }
            }
        }

        /// <summary>
        /// Inspector用デバッグ表示を選択時に描画する。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || positions == null || positions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                RectTransform rectTransform = positions[i];
                if (rectTransform == null)
                {
                    continue;
                }

                Vector3 center = GetGizmoCenter(rectTransform);

                Gizmos.color = selectedMarkerColor;
                Gizmos.DrawWireCube(center, new Vector3(selectedMarkerWidth, selectedMarkerHeight, 1.0f));

#if UNITY_EDITOR
                if (showLabels)
                {
                    Vector2 anchoredPosition = rectTransform.anchoredPosition;
                    Vector3 euler = rectTransform.localEulerAngles;
                    string positionText = $"Rect {i + 1}\nX:{anchoredPosition.x:F2} Y:{anchoredPosition.y:F2}\nRot:{euler.z:F0}°";
                    UnityEditor.Handles.Label(center + Vector3.up * (selectedMarkerHeight * 0.5f + 18.0f), positionText);
                }
#endif
            }

#if UNITY_EDITOR
            if (showStatistics)
            {
                DrawStatisticsLabel();
            }
#endif
        }

        /// <summary>
        /// Inspector上で設定不備を検出する。
        /// </summary>
        private void OnValidate()
        {
            ValidatePositions();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// RectTransformのローカルUI座標をGizmo描画用のワールド座標へ変換する。
        /// </summary>
        private static Vector3 GetGizmoCenter(RectTransform rectTransform)
        {
            return rectTransform.position;
        }

        /// <summary>
        /// RectTransformの実表示位置を設定ルートのローカル座標へ変換する。
        /// </summary>
        private Vector3 GetLocalPosition(RectTransform rectTransform)
        {
            RectTransform rootRect = transform as RectTransform;
            if (rootRect == null)
            {
                return rectTransform.localPosition;
            }

            return rootRect.InverseTransformPoint(rectTransform.position);
        }

        /// <summary>
        /// 未設定のRectTransformを検出する。
        /// </summary>
        private void ValidatePositions()
        {
            if (positions == null)
            {
                return;
            }

            for (int i = positions.Count - 1; i >= 0; i--)
            {
                if (positions[i] == null)
                {
                    Debug.LogWarning($"RectPositionConfig '{gameObject.name}': Position[{i}] is null. Consider removing null entries.", this);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 登録済みRectTransformの統計情報をSceneビューへ表示する。
        /// </summary>
        private void DrawStatisticsLabel()
        {
            var validPositions = positions.Where(p => p != null).ToList();
            if (validPositions.Count == 0)
            {
                return;
            }

            Vector3 center = validPositions
                .Aggregate(Vector3.zero, (sum, p) => sum + GetLocalPosition(p)) / validPositions.Count;

            RectTransform rootRect = transform as RectTransform;
            Vector3 labelPosition = rootRect != null
                ? rootRect.TransformPoint(center)
                : validPositions[0].position;

            string statsText = $"Total Positions: {positions.Count}\nValid Positions: {validPositions.Count}\nCenter: ({center.x:F1}, {center.y:F1})";
            UnityEditor.Handles.Label(labelPosition + Vector3.up * 40.0f, statsText);
        }
#endif

        #endregion
    }
}
