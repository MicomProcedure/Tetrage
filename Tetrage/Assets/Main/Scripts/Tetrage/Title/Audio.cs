using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("UI Sliders (optional)")]
    [SerializeField] private Slider BGMSlider;
    [SerializeField] private Slider SESlider;

    private static AudioManager instance;

    // 保存用キー
    private const string BGM_KEY = "BGMVolume";
    private const string SE_KEY = "SEVolume";

    private void Awake()
    {
        // シングルトン化：重複したら破棄
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 保存された音量を読み込む（なければ0dBを初期値）
        float savedBGM = PlayerPrefs.GetFloat(BGM_KEY, 0f);
        float savedSE = PlayerPrefs.GetFloat(SE_KEY, 0f);

        audioMixer.SetFloat("BGM", savedBGM);
        audioMixer.SetFloat("SE", savedSE);

        // スライダーがある場合は値を反映し、リスナー登録
        if (BGMSlider != null)
        {
            BGMSlider.value = savedBGM;
            BGMSlider.onValueChanged.AddListener(SetBGM);
        }

        if (SESlider != null)
        {
            SESlider.value = savedSE;
            SESlider.onValueChanged.AddListener(SetSE);
        }
    }

    public void SetBGM(float volume)
    {
        audioMixer.SetFloat("BGM", volume);
        PlayerPrefs.SetFloat(BGM_KEY, volume);
    }

    public void SetSE(float volume)
    {
        audioMixer.SetFloat("SE", volume);
        PlayerPrefs.SetFloat(SE_KEY, volume);
    }

    public float GetBGMVolume()
    {
        audioMixer.GetFloat("BGM", out float value);
        return value;
    }

    public float GetSEVolume()
    {
        audioMixer.GetFloat("SE", out float value);
        return value;
    }
}
