using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Constants;
using UnityEngine;

/// <summary>
/// ゲーム中のカットイン演出と開始演出の発火を集約するコントローラ。
/// </summary>
public class CutInAnimationController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Cut In Views")]
    [SerializeField] private CutInAnimationView _tetrageSoloCutIn;
    [SerializeField] private CutInAnimationView _tetrageMultiCutIn;
    [SerializeField] private CutInAnimationView _tetrageReachCutIn;

    [Header("Game Start Animation")]
    [SerializeField] private GameStartAnimation _gameStartAnimation;

    #endregion

    #region Private Fields

    private CancellationTokenSource _cutInCts;

    #endregion

    #region Public Methods

    /// <summary>
    /// TetrageSolo用のカットインを再生する。
    /// </summary>
    [ContextMenu("Play Tetrage Solo CutIn")]
    public void PlayTetrageSoloCutIn()
    {
        PlayCutIn(
            _tetrageSoloCutIn,
            nameof(_tetrageSoloCutIn),
            InGameConsts.CutInAnimationDuration.TETRAGE_SOLO_TOTAL_DURATION_MS);
    }

    /// <summary>
    /// TetrageMulti用のカットインを再生する。
    /// </summary>
    [ContextMenu("Play Tetrage Multi CutIn")]
    public void PlayTetrageMultiCutIn()
    {
        PlayCutIn(
            _tetrageMultiCutIn,
            nameof(_tetrageMultiCutIn),
            InGameConsts.CutInAnimationDuration.TETRAGE_MULTI_TOTAL_DURATION_MS);
    }

    /// <summary>
    /// TetrageReach用のカットインを再生する。
    /// </summary>
    [ContextMenu("Play Tetrage Reach CutIn")]
    public void PlayTetrageReachCutIn()
    {
        PlayCutIn(
            _tetrageReachCutIn,
            nameof(_tetrageReachCutIn),
            InGameConsts.CutInAnimationDuration.TETRAGE_REACH_TOTAL_DURATION_MS);
    }

    /// <summary>
    /// ゲーム開始演出を再生する。
    /// </summary>
    [ContextMenu("Play Game Start Animation")]
    public void PlayGameStartAnimation()
    {
        gameObject.SetActive(true);

        if (_gameStartAnimation == null)
        {
            Debug.LogWarning("CutInAnimationController: _gameStartAnimation が未設定です。ゲーム開始演出をスキップします。", this);
            return;
        }

        _gameStartAnimation.PlayAnimation();
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnDisable()
    {
        CancelCutIn();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 指定したカットインViewを再生する。
    /// </summary>
    private void PlayCutIn(CutInAnimationView cutInView, string cutInName, int totalDurationMs)
    {
        gameObject.SetActive(true);

        if (!ValidateCutIn(cutInView, cutInName))
        {
            return;
        }

        ResetCutInCancellationToken();
        cutInView.StartCutIn(totalDurationMs, _cutInCts.Token).Forget(HandleCutInError);
    }

    /// <summary>
    /// 再生中のカットインを停止して、新しいキャンセルTokenを発行する。
    /// </summary>
    private void ResetCutInCancellationToken()
    {
        CancelCutIn();
        _cutInCts = new CancellationTokenSource();
    }

    /// <summary>
    /// 再生中のカットインをキャンセルしてリソースを破棄する。
    /// </summary>
    private void CancelCutIn()
    {
        _cutInCts?.Cancel();
        _cutInCts?.Dispose();
        _cutInCts = null;
    }

    /// <summary>
    /// カットイン再生に必要な参照を検証する。
    /// </summary>
    private bool ValidateCutIn(CutInAnimationView cutInView, string cutInName)
    {
        if (cutInView != null)
        {
            return true;
        }

        Debug.LogWarning($"CutInAnimationController: {cutInName} が未設定です。カットインをスキップします。", this);
        return false;
    }

    /// <summary>
    /// Inspector参照の設定漏れを検出する。
    /// </summary>
    private void ValidateReferences()
    {
        ValidateCutIn(_tetrageSoloCutIn, nameof(_tetrageSoloCutIn));
        ValidateCutIn(_tetrageMultiCutIn, nameof(_tetrageMultiCutIn));
        ValidateCutIn(_tetrageReachCutIn, nameof(_tetrageReachCutIn));

        if (_gameStartAnimation == null)
        {
            Debug.LogWarning("CutInAnimationController: _gameStartAnimation が未設定です。ゲーム開始演出をスキップします。", this);
        }
    }

    /// <summary>
    /// カットイン再生中のキャンセル以外の例外をログに出す。
    /// </summary>
    private static void HandleCutInError(Exception error)
    {
        if (error is OperationCanceledException)
        {
            return;
        }

        Debug.LogError(error);
    }

    #endregion
}
