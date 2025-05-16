using UnityEngine;
using Tetrage.Animations;
using Cysharp.Threading.Tasks;

namespace Tetrage.Tests
{
    /// <summary>
    /// AnimationHelper の MoveTo 系メソッドをデバッグするクラス
    /// </summary>
    public class TransferAnimationDebugger : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject target;
        [Header("Move Settings")]
        [SerializeField] private Transform startPosition;
        [SerializeField] private Transform endPosition;
        [SerializeField] private float duration = 1f;

        private void Reset()
        {
            target = gameObject;
        }

        [ContextMenu("Move To")]
        private void MoveTo()
        {
            if (target == null)
            {
                Debug.LogWarning("TransferAnimationDebugger: ターゲットが設定されていません");
                return;
            }
            AnimationHelper.MoveTo(target, startPosition.position, endPosition.position, duration).Forget();
            Debug.Log($"TransferAnimationDebugger: MoveTo を実行しました (target: {target.name}, duration: {duration})");
        }

        [ContextMenu("Move To With Easing")]
        private void MoveToWithEasing()
        {
            if (target == null)
            {
                Debug.LogWarning("TransferAnimationDebugger: ターゲットが設定されていません");
                return;
            }
            AnimationHelper.MoveToWithEasing(target, startPosition.position, endPosition.position, duration).Forget();
            Debug.Log($"TransferAnimationDebugger: MoveToWithEasing を実行しました (target: {target.name}, duration: {duration})");
        }
    }
}
