using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsUI : MonoBehaviour
{
    [Header("Audio")]
    public Slider MasterSlider;
    public Slider SFXSlider;
    public Slider MusicSlider;

    [Header("Sensibilidade")]
    public Slider SensitivitySlider;
    public TextMeshProUGUI SensitivityValueText;

    private const string MASTER_KEY = "MasterVolume";
    private const string SFX_KEY    = "SFXVolume";
    private const string MUSIC_KEY  = "MusicVolume";
    private const string SENS_KEY   = "MouseSensitivity";

    private void OnEnable()
    {
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float sfx    = PlayerPrefs.GetFloat(SFX_KEY,    1f);
        float music  = PlayerPrefs.GetFloat(MUSIC_KEY,  1f);
        float sens   = PlayerPrefs.GetFloat(SENS_KEY,   1f);

        // Remove listeners before setting values so the slider reset to its
        // previous state (often 0 when re-enabled) doesn't fire OnValueChanged
        // with a stale value and mute the audio before we can push the real one.
        MasterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        SFXSlider.onValueChanged.RemoveListener(OnSFXChanged);
        MusicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        SensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);

        MasterSlider.value      = master;
        SFXSlider.value         = sfx;
        MusicSlider.value       = music;
        SensitivitySlider.value = sens;

        MasterSlider.onValueChanged.AddListener(OnMasterChanged);
        SFXSlider.onValueChanged.AddListener(OnSFXChanged);
        MusicSlider.onValueChanged.AddListener(OnMusicChanged);
        SensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        UpdateSensitivityText(sens);

        // Explicitly push correct values to AudioManager after sliders are set.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(master);
            AudioManager.Instance.SetMusicVolume(music);
            AudioManager.Instance.SetSFXVolume(sfx);
        }
        else
        {
            Debug.LogError("[OptionsUI] OnEnable: AudioManager.Instance is NULL — no AudioManager in scene or singleton not yet created.");
        }
    }

    public void OnMasterChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
    }

    public void OnSFXChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    public void OnMusicChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
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
