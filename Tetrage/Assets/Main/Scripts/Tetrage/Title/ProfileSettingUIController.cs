using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tetrage.Title
{
    /// <summary>
    /// プロファイル編集 UI。確定時のみ <see cref="PlayerProfileManager"/> へ保存し、外部表示は ProfileChanged で更新する。
    /// </summary>
    public class ProfileSettingUIController : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Image _iconImage;
        [SerializeField] private Sprite[] _availableIcons;
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private PlayerProfileManager _profileManager;

        #endregion

        #region Private Fields

        private int _currentIconIndex;
        private readonly CompositeDisposable _disposables = new();

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (!Validate())
            {
                return;
            }

            _profileManager.ProfileChanged
                .Subscribe(ApplyToEditingUI)
                .AddTo(_disposables);

            // パネル再表示時も保存済みデータで編集 UI を同期する
            ApplyToEditingUI(_profileManager.Data);
        }

        private void OnDisable()
        {
            _disposables.Clear();
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 次のアイコンに切り替える（プレビューのみ、保存は OnConfirm 時）。
        /// </summary>
        public void OnNextIcon()
        {
            if (!Validate())
            {
                return;
            }

            _currentIconIndex = (_currentIconIndex + 1) % _availableIcons.Length;
            ApplyIconSprite(_currentIconIndex);
        }

        /// <summary>
        /// 前のアイコンに切り替える（プレビューのみ、保存は OnConfirm 時）。
        /// </summary>
        public void OnPrevIcon()
        {
            if (!Validate())
            {
                return;
            }

            _currentIconIndex = (_currentIconIndex - 1 + _availableIcons.Length) % _availableIcons.Length;
            ApplyIconSprite(_currentIconIndex);
        }

        /// <summary>
        /// プロファイルを確定して保存する。
        /// </summary>
        public void OnConfirm()
        {
            if (!Validate())
            {
                return;
            }

            SaveProfile();
            Debug.Log($"[Profile Confirmed] Name: {_nameInput.text}, Icon: {_currentIconIndex}");
        }

        #endregion

        #region Private Methods

        private bool Validate()
        {
            if (_profileManager == null)
            {
                _profileManager = FindFirstObjectByType<PlayerProfileManager>();
            }

            if (_profileManager == null)
            {
                Debug.LogWarning($"{nameof(ProfileSettingUIController)}: {nameof(PlayerProfileManager)} が未設定です。", this);
                return false;
            }

            if (_iconImage == null || _availableIcons == null || _availableIcons.Length == 0)
            {
                Debug.LogWarning($"{nameof(ProfileSettingUIController)}: アイコン表示が未設定です。", this);
                return false;
            }

            if (_nameInput == null)
            {
                Debug.LogWarning($"{nameof(ProfileSettingUIController)}: {nameof(TMP_InputField)} が未設定です。", this);
                return false;
            }

            return true;
        }

        private void ApplyToEditingUI(PlayerProfileData data)
        {
            if (data == null)
            {
                return;
            }

            ApplyIconSprite(ClampIconIndex(data.IconIndex));
            _nameInput.text = data.PlayerName;
        }

        private void ApplyIconSprite(int iconIndex)
        {
            _currentIconIndex = ClampIconIndex(iconIndex);
            _iconImage.sprite = _availableIcons[_currentIconIndex];
        }

        private int ClampIconIndex(int iconIndex)
        {
            return Mathf.Clamp(iconIndex, 0, _availableIcons.Length - 1);
        }

        private void SaveProfile()
        {
            _profileManager.UpdateProfile(_currentIconIndex, _nameInput.text);
        }

        #endregion
    }
}
