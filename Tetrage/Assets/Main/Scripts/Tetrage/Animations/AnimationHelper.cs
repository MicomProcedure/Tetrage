using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Tetrage.Animations
{
    /// <summary>
    /// アニメーション関連のヘルパークラス
    /// </summary>
    public static class AnimationHelper
    {
        /// <summary>
        /// 指定されたGameObjectをA地点からB地点まで移動させます
        /// Time.timeScaleに依存しない実装（unscaledDeltaTimeを使用）
        /// </summary>
        /// <param name="target">移動させるGameObject</param>
        /// <param name="startPosition">開始位置</param>
        /// <param name="endPosition">終了位置</param>
        /// <param name="duration">移動にかかる時間（秒）</param>
        public static async UniTask MoveTo(GameObject target, Vector3 startPosition, Vector3 endPosition, float duration = 1.0f)
        {
            if (target == null) return;

            float elapsedTime = 0f;
            target.transform.position = startPosition;

            while (elapsedTime < duration)
            {
                if (target == null) return;

                elapsedTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);

                // 線形補間で位置を計算
                target.transform.position = Vector3.Lerp(startPosition, endPosition, t);

                await UniTask.Yield();
            }

            // 確実に終了位置に移動させる
            if (target != null)
            {
                target.transform.position = endPosition;
            }
        }

        /// <summary>
        /// 指定されたGameObjectをA地点からB地点までイージングをかけて移動させます
        /// Time.timeScaleに依存しない実装（unscaledDeltaTimeを使用）
        /// </summary>
        /// <param name="target">移動させるGameObject</param>
        /// <param name="startPosition">開始位置</param>
        /// <param name="endPosition">終了位置</param>
        /// <param name="duration">移動にかかる時間（秒）</param>
        public static async UniTask MoveToWithEasing(GameObject target, Vector3 startPosition, Vector3 endPosition, float duration = 1.0f)
        {
            if (target == null) return;

            float elapsedTime = 0f;
            target.transform.position = startPosition;

            while (elapsedTime < duration)
            {
                if (target == null) return;

                elapsedTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);

                // イージング関数を適用（easeInOutQuad）
                t = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

                // 補間で位置を計算
                target.transform.position = Vector3.Lerp(startPosition, endPosition, t);

                await UniTask.Yield();
            }

            // 確実に終了位置に移動させる
            if (target != null)
            {
                target.transform.position = endPosition;
            }
        }
    }
}