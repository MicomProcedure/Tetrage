using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Audio : MonoBehaviour
{
    //Audioミキサーを入れるところ
    [SerializeField] AudioMixer audioMixer;

    //BGMスライダーを入れるところ
    [SerializeField] Slider BGMSlider;

    private void Start()
    {
        //BGM
        audioMixer.GetFloat("BGM", out float bgmVolume);
        BGMSlider.value = bgmVolume;
    }

    //スライダーで使う部分です。
    public void SetBGM(float volume)
    {
        audioMixer.SetFloat("BGM", volume);
    }
}
