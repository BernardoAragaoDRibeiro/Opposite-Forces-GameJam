using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class OptionsUI : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer AudioMixer;
    public Slider MasterSlider;
    public Slider SFXSlider;
    public Slider MusicSlider;

    [Header("Sensibilidade")]
    public Slider SensitivitySlider;
    public TextMeshProUGUI SensitivityValueText;

    private const string MASTER_KEY = "MasterVolume";
    private const string SFX_KEY = "SFXVolume";
    private const string MUSIC_KEY = "MusicVolume";
    private const string SENS_KEY = "MouseSensitivity";

    private void OnEnable()
    {
        MasterSlider.value = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        SFXSlider.value = PlayerPrefs.GetFloat(SFX_KEY, 1f);
        MusicSlider.value = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        SensitivitySlider.value = PlayerPrefs.GetFloat(SENS_KEY, 1f);
        UpdateSensitivityText(SensitivitySlider.value);
    }

    public void OnMasterChanged(float value)
    {
        AudioMixer.SetFloat(MASTER_KEY, Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat(MASTER_KEY, value);
    }

    public void OnSFXChanged(float value)
    {
        AudioMixer.SetFloat(SFX_KEY, Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat(SFX_KEY, value);
    }

    public void OnMusicChanged(float value)
    {
        AudioMixer.SetFloat(MUSIC_KEY, Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat(MUSIC_KEY, value);
    }

    public void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat(SENS_KEY, value);
        PlayerPrefs.Save();
        UpdateSensitivityText(value);
    }

    private void UpdateSensitivityText(float value)
    {
        SensitivityValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    public void OnBackButton()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}