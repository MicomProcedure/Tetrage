using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class CountdownController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float interval = 1f;

    private void Start()
    {
        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        string[] sequence = { "3", "2", "1",};

        foreach (var text in sequence)
        {
            countdownText.text = text;
            countdownText.alpha = 0f;
            countdownText.transform.localScale = Vector3.zero;

            // 数字がふわっと拡大しながら現れる
            countdownText.DOFade(1f, 0.2f);
            countdownText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(interval * 0.7f);

            // 数字が少し小さくなりながらフェードアウト
            countdownText.DOFade(0f, 0.3f);
            countdownText.transform.DOScale(0.5f, 0.3f).SetEase(Ease.InQuad);

            yield return new WaitForSeconds(interval * 0.3f);
        }

        countdownText.gameObject.SetActive(false);
    }
}
