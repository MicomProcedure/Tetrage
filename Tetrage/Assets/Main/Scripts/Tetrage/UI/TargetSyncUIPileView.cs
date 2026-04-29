using Tetrage.Core.Constants;
using Tetrage.Core.Ids;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Tetrage.UI
{
    /// <summary>
    /// PlayerUI内のマーカー位置へTargetカード山ビューを同期する。
    /// </summary>
    public sealed class TargetSyncUIPileView : BasicCardPileView
    {
        #region Serialized Fields

        [Header("Sync Settings")]
        [SerializeField] private bool resolveMarkerOnEnable = true;
        [SerializeField] private int resolveRetryFrameCount = 5;

        #endregion

        #region Private Fields

        private int ownerPlayerId = -1;
        private Transform syncMarker;
        private PlayerUIPanelManager playerUIPanelManager;
        private int resolveRetryVersion;

        /// <summary>シーン由来のゲームカメラ（自動解決でキャッシュする）</summary>
        private Camera resolvedWorldCamera;

        #endregion

        #region Public API

        /// <summary>
        /// 同期対象のPlayerIdを設定する。
        /// </summary>
        public void SetOwnerPlayerId(PlayerId playerId)
        {
            ownerPlayerId = playerId.Value;
        }

        /// <summary>
        /// 同期対象のPlayerIdを設定する。
        /// </summary>
        public void SetOwnerPlayerId(int playerId)
        {
            ownerPlayerId = playerId;
        }

        /// <summary>
        /// 同期先マーカーを直接設定する。
        /// </summary>
        public void SetSyncTarget(Transform marker)
        {
            syncMarker = marker;
        }

        /// <summary>
        /// PlayerUIからマーカーを再解決して位置同期する。
        /// </summary>
        public void RefreshBindingAndPosition()
        {
            // Debug.Log("TargetSyncUIPileView: RefreshBindingAndPosition");
            if (!TryResolveMarker())     return;
            Debug.Log("<color=green>TargetSyncUIPileView: TryResolveMarker success</color>");
            RefreshPosition();
        }

        /// <summary>
        /// 現在の同期先マーカーへ位置を反映する。
        /// </summary>
        public void RefreshPosition()
        {
            if (syncMarker == null)
            {
                return;
            }

            if (!TryConvertMarkerToWorldPosition(out var targetPosition))
            {
                return;
            }

            transform.position = targetPosition;
            Debug.Log($"<color=#FFD700>RefreshPosition: position set to {transform.position}</color>");
            RefreshView();  // 位置が変更されたのでViewを更新する。
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (!resolveMarkerOnEnable)
            {
                return;
            }

            RefreshBindingAndPosition();
            StartDelayedResolveIfNeeded();
        }

        private void OnDisable()
        {
            resolveRetryVersion++;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// PlayerIdを使ってPlayerUI側マーカーを取得する。
        /// </summary>
        private bool TryResolveMarker()
        {
            if (ownerPlayerId <= 0) return false;

            if (playerUIPanelManager == null)   playerUIPanelManager = FindFirstObjectByType<PlayerUIPanelManager>();

            if (playerUIPanelManager == null) return false;

            if (!playerUIPanelManager.TryGetTargetPileMarker(ownerPlayerId, out var marker)) return false;

            syncMarker = marker;
            return true;
        }

        /// <summary>
        /// UI上のマーカー位置を、TargetPileが描画されるワールド座標へ変換する。
        /// </summary>
        private bool TryConvertMarkerToWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = transform.position;

            if (!TryGetWorldCamera(out var worldCamera))
            {
                Debug.LogWarning("TargetSyncUIPileView: TargetPile用カメラが見つからないため位置同期をスキップします。");
                return false;
            }

            Vector2 screenPoint = GetMarkerScreenPoint();
            Ray ray = worldCamera.ScreenPointToRay(screenPoint);
            Plane targetPlane = new Plane(-worldCamera.transform.forward, transform.position);

            if (!targetPlane.Raycast(ray, out float enter))
            {
                Debug.LogWarning("TargetSyncUIPileView: UIマーカー座標をTargetPile平面へ投影できませんでした。");
                return false;
            }

            worldPosition = ray.GetPoint(enter);
            return true;
        }

        /// <summary>
        /// シーンからゲームカメラを取得する。（MainCameraタグのGameObjectをGetComponent → 失敗時はCamera.main）
        /// </summary>
        private bool TryGetWorldCamera(out Camera camera)
        {
            if (resolvedWorldCamera == null || !resolvedWorldCamera.isActiveAndEnabled)
            {
                var mainCameraGO = GameObject.FindGameObjectWithTag(InGameConsts.UnityBuiltInTags.MainCamera);
                Camera found = mainCameraGO != null ? mainCameraGO.GetComponent<Camera>() : null;
                found ??= Camera.main;
                resolvedWorldCamera = found;
            }

            camera = resolvedWorldCamera;
            return camera != null;
        }

        /// <summary>
        /// マーカーのRectTransform/Transform位置をスクリーン座標として取得する。
        /// </summary>
        private Vector2 GetMarkerScreenPoint()
        {
            var rectTransform = syncMarker as RectTransform;
            if (rectTransform == null)
            {
                return RectTransformUtility.WorldToScreenPoint(null, syncMarker.position);
            }

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector3 markerCenter = (corners[0] + corners[2]) * 0.5f;

            return RectTransformUtility.WorldToScreenPoint(uiCamera, markerCenter);
        }

        /// <summary>
        /// 初期化順のズレを吸収するため、数フレーム遅延で再解決する。
        /// </summary>
        private void StartDelayedResolveIfNeeded()
        {
            if (syncMarker != null)
            {
                return;
            }

            if (resolveRetryFrameCount <= 0)
            {
                return;
            }

            resolveRetryVersion++;
            ResolveMarkerWithRetryAsync(resolveRetryVersion).Forget();
        }

        /// <summary>
        /// 指定フレーム内でマーカー解決を再試行し、成功したら位置同期する。
        /// </summary>
        private async UniTaskVoid ResolveMarkerWithRetryAsync(int version)
        {
            for (int i = 0; i < resolveRetryFrameCount; i++)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);

                if (version != resolveRetryVersion)
                {
                    return;
                }

                if (!isActiveAndEnabled)
                {
                    return;
                }

                if (!TryResolveMarker())
                {
                    continue;
                }

                RefreshPosition();
                return;
            }
        }

        #endregion
    }
}
