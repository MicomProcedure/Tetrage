using UnityEngine;
using Tetrage.Core.Contracts;

namespace Tetrage.UI
{
    /// <summary>
    /// IDebuggableインターフェースを持つコンポーネントのデバッグ情報を表示するためのウィンドウを管理します。
    /// このコンポーネントは、IDebuggableを実装したコンポーネントと同じGameObjectにアタッチする必要があります。
    /// </summary>
    [RequireComponent(typeof(IDebuggable))]
    public class DebugDisplay : MonoBehaviour
    {
        [Header("表示設定")]
        [Tooltip("デバッグウィンドウの表示/非表示")]
        [SerializeField] private bool _showWindow = true;

        [Header("ウィンドウ設定")]
        [SerializeField] private Rect _windowRect = new Rect(10, 10, 350, 500);
        [SerializeField] private string _windowTitle = "デバッグウィンドウ";

        private IDebuggable _debuggableTarget;

        private void Awake()
        {
            // 同じGameObjectにアタッチされたIDebuggableコンポーネントを取得
            _debuggableTarget = GetComponent<IDebuggable>();
            if (_debuggableTarget == null)
            {
                Debug.LogError("DebugDisplay: 同じGameObjectにIDebuggableを実装したコンポーネントが見つかりません。", this);
                enabled = false; // エラー時はコンポーネントを無効化
            }
        }

        private void OnGUI()
        {
            // ウィンドウが表示設定でない、または対象が見つからない場合は何もしない
            if (!_showWindow || _debuggableTarget == null)
            {
                return;
            }

            // GUILayout.Windowを使用して、ドラッグ可能でタイトル付きのウィンドウを作成
            _windowRect = GUILayout.Window(0, _windowRect, (id) =>
            {
                // IDebuggableターゲットの描画処理を呼び出す
                _debuggableTarget.DrawDebugGUI();

                // ウィンドウをドラッグ可能にする
                GUI.DragWindow();
            }, _windowTitle);
        }

        /// <summary>
        /// デバッグウィンドウの表示・非表示を切り替えます
        /// </summary>
        public void ToggleWindow()
        {
            _showWindow = !_showWindow;
        }

        /// <summary>
        /// Inspectorからウィンドウの表示状態を設定します
        /// </summary>
        public void SetWindowVisibility(bool isVisible)
        {
            _showWindow = isVisible;
        }
    }
}