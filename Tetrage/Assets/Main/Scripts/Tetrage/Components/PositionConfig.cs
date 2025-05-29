using UnityEngine;
using Tetrage.Core.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Tetrage.Components
{
    public class PositionConfig : MonoBehaviour, IPositionConfig
    {
        [SerializeField]
        [Tooltip("The positions of the marker")]
        private List<Transform> positions;

        [Header("ビジュアル表示設定")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showLabels = true;
        [SerializeField] private bool showDirection = true;
        [SerializeField] private bool showAxes = false;
        [SerializeField] private bool showConnectionLines = true;
        [SerializeField] private bool showStatistics = false;

        [Header("表示色設定")]
        [SerializeField] private Color markerColor = Color.cyan;
        [SerializeField] private Color selectedMarkerColor = Color.green;
        [SerializeField] private Color directionColor = Color.yellow;
        [SerializeField] private Color connectionLineColor = Color.white;
        [SerializeField] private Color xAxisColor = Color.red;
        [SerializeField] private Color yAxisColor = Color.green;
        [SerializeField] private Color zAxisColor = Color.blue;

        [Header("表示サイズ設定")]
        [SerializeField] [Range(0.1f, 2.0f)] private float markerSize = 0.5f;
        [SerializeField] [Range(0.1f, 2.0f)] private float selectedMarkerSize = 0.8f;
        [SerializeField] [Range(0.1f, 3.0f)] private float directionLength = 0.75f;
        [SerializeField] [Range(0.1f, 3.0f)] private float axisLength = 1.0f;

        public List<Vector3> Position => positions.Select(p => p.position).ToList();

        public List<Quaternion> Rotation => positions.Select(p => p.rotation).ToList();

        public bool ValidateConfig(int requiredCount)
        {
            return Position.Count == requiredCount && Rotation.Count == requiredCount;
        }

        /// <summary>
        /// Inspector用デバッグ表示（常に表示）
        /// </summary>
        void OnDrawGizmos()
        {
            if (!showGizmos || positions == null || positions.Count == 0) return;

            // 各位置をギズモで表示
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i] == null) continue;

                // 位置マーカーを表示
                Gizmos.color = markerColor;
                Gizmos.DrawWireCube(positions[i].position, Vector3.one * markerSize);
                
                // 回転方向を矢印で表示
                if (showDirection)
                {
                    Gizmos.color = directionColor;
                    Vector3 forward = positions[i].forward * directionLength;
                    Gizmos.DrawRay(positions[i].position, forward);
                }
                
                // 簡単なラベル表示
                if (showLabels)
                {
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(positions[i].position + Vector3.up * (markerSize + 0.3f), $"Pos {i + 1}");
#endif
                }

                // 接続線を表示（複数位置がある場合）
                if (showConnectionLines && i < positions.Count - 1 && positions[i + 1] != null)
                {
                    Gizmos.color = connectionLineColor;
                    Gizmos.DrawLine(positions[i].position, positions[i + 1].position);
                }
            }
        }

        /// <summary>
        /// Inspector用デバッグ表示（選択時のみ表示）
        /// </summary>
        void OnDrawGizmosSelected()
        {
            if (!showGizmos || positions == null || positions.Count == 0) return;

            // 選択時により詳細な情報を表示
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i] == null) continue;

                // より大きなワイヤーフレームで強調表示
                Gizmos.color = selectedMarkerColor;
                Gizmos.DrawWireCube(positions[i].position, Vector3.one * selectedMarkerSize);
                
                // 座標軸を表示
                if (showAxes)
                {
                    // X軸（右方向）- 赤
                    Gizmos.color = xAxisColor;
                    Gizmos.DrawRay(positions[i].position, positions[i].right * axisLength);
                    
                    // Y軸（上方向）- 緑
                    Gizmos.color = yAxisColor;
                    Gizmos.DrawRay(positions[i].position, positions[i].up * axisLength);
                    
                    // Z軸（前方向）- 青
                    Gizmos.color = zAxisColor;
                    Gizmos.DrawRay(positions[i].position, positions[i].forward * axisLength);
                }

#if UNITY_EDITOR
                if (showLabels)
                {
                    // 詳細な位置情報をラベル表示
                    Vector3 pos = positions[i].position;
                    Vector3 euler = positions[i].eulerAngles;
                    string posText = $"Pos {i + 1}\nX:{pos.x:F2} Y:{pos.y:F2} Z:{pos.z:F2}\nRot:{euler.y:F0}°";
                    UnityEditor.Handles.Label(positions[i].position + Vector3.up * (selectedMarkerSize + 0.7f), posText);
                }
                
                // 位置間の接続線を表示（複数位置がある場合）
                if (showConnectionLines && i < positions.Count - 1 && positions[i + 1] != null)
                {
                    Gizmos.color = connectionLineColor;
                    Gizmos.DrawLine(positions[i].position, positions[i + 1].position);
                }
#endif
            }

#if UNITY_EDITOR
            // 全体の統計情報を表示
            if (showStatistics && positions.Count > 0)
            {
                var validPositions = positions.Where(p => p != null).ToList();
                if (validPositions.Count > 0)
                {
                    Vector3 center = validPositions.Aggregate(Vector3.zero, (sum, p) => sum + p.position) / validPositions.Count;
                    string statsText = $"Total Positions: {positions.Count}\nValid Positions: {validPositions.Count}\nCenter: ({center.x:F1}, {center.y:F1}, {center.z:F1})";
                    UnityEditor.Handles.Label(center + Vector3.up * 2f, statsText);
                }
            }
#endif
        }

        /// <summary>
        /// Inspector用の設定検証表示
        /// </summary>
        void OnValidate()
        {
            // nullエントリがないかチェック
            if (positions != null)
            {
                for (int i = positions.Count - 1; i >= 0; i--)
                {
                    if (positions[i] == null)
                    {
                        Debug.LogWarning($"PositionConfig '{gameObject.name}': Position[{i}] is null. Consider removing null entries.", this);
                    }
                }
            }
        }

        /// <summary>
        /// 全ての表示をオンにする
        /// </summary>
        [ContextMenu("全ての表示をオン")]
        public void EnableAllVisuals()
        {
            showGizmos = true;
            showLabels = true;
            showDirection = true;
            showAxes = true;
            showConnectionLines = true;
            showStatistics = true;
        }

        /// <summary>
        /// 全ての表示をオフにする
        /// </summary>
        [ContextMenu("全ての表示をオフ")]
        public void DisableAllVisuals()
        {
            showGizmos = false;
            showLabels = false;
            showDirection = false;
            showAxes = false;
            showConnectionLines = false;
            showStatistics = false;
        }

        /// <summary>
        /// デフォルト色に戻す
        /// </summary>
        [ContextMenu("デフォルト色に戻す")]
        public void ResetToDefaultColors()
        {
            markerColor = Color.cyan;
            selectedMarkerColor = Color.green;
            directionColor = Color.yellow;
            connectionLineColor = Color.white;
            xAxisColor = Color.red;
            yAxisColor = Color.green;
            zAxisColor = Color.blue;
        }
    }
}
