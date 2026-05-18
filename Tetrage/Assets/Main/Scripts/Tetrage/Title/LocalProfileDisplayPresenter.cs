using R3;
using Tetrage.UI;
using UnityEngine;

namespace Tetrage.Title
{
    /// <summary>
    /// ローカル保存プロファイルを <see cref="ProfileDisplayView"/> に反映する Presenter。
    /// </summary>
    public class LocalProfileDisplayPresenter : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private PlayerProfileManager profileManager;
        [SerializeField] private ProfileDisplayView profileDisplayView;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private bool _isBound;

        #endregion

        #region Public Methods

        /// <summary>
        /// プレイヤー表示用オブジェクトに Presenter を後付けして購読を開始する。
        /// </summary>
        public static LocalProfileDisplayPresenter Attach(GameObject target, PlayerProfileManager manager = null)
        {
            if (target == null)
            {
                Debug.LogWarning($"{nameof(LocalProfileDisplayPresenter)}: target が null です。");
                return null;
            }

            var view = target.GetComponent<ProfileDisplayView>();
            if (view == null)
            {
                Debug.LogWarning(
                    $"{nameof(LocalProfileDisplayPresenter)}: {nameof(ProfileDisplayView)} が見つかりません。",
                    target);
                return null;
            }

            var presenter = target.GetComponent<LocalProfileDisplayPresenter>()
                            ?? target.AddComponent<LocalProfileDisplayPresenter>();
            presenter.Configure(manager, view);
            return presenter;
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        #endregion

        #region Private Methods

        private void Configure(PlayerProfileManager manager, ProfileDisplayView view)
        {
            if (manager != null)
            {
                profileManager = manager;
            }

            if (view != null)
            {
                profileDisplayView = view;
            }

            Bind();
        }

        private void Bind()
        {
            if (_isBound)
            {
                return;
            }

            if (!Validate())
            {
                return;
            }

            profileManager.ProfileChanged
                .Subscribe(ApplyProfile)
                .AddTo(_disposables);

            ApplyProfile(profileManager.Data);
            _isBound = true;
        }

        private void Unbind()
        {
            _disposables.Clear();
            _isBound = false;
        }

        private bool Validate()
        {
            if (profileManager == null)
            {
                profileManager = FindFirstObjectByType<PlayerProfileManager>();
            }

            if (profileDisplayView == null)
            {
                profileDisplayView = GetComponent<ProfileDisplayView>();
            }

            if (profileManager == null)
            {
                Debug.LogWarning($"{nameof(LocalProfileDisplayPresenter)}: {nameof(PlayerProfileManager)} が見つかりません。", this);
                return false;
            }

            if (profileDisplayView == null)
            {
                Debug.LogWarning($"{nameof(LocalProfileDisplayPresenter)}: {nameof(ProfileDisplayView)} が見つかりません。", this);
                return false;
            }

            return true;
        }

        private void ApplyProfile(PlayerProfileData data)
        {
            if (data == null || profileDisplayView == null)
            {
                return;
            }

            profileDisplayView.SetProfile(data.IconIndex, data.PlayerName);
        }

        #endregion
    }
}
