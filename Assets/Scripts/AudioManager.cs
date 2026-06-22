using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Canais")]
    public AudioSource Music;
    public AudioSource SFX;

    [Header("Mixer")]
    public AudioMixer Mixer;

    private const string MASTER_KEY = "MasterVolume";
    private const string SFX_KEY    = "SFXVolume";
    private const string MUSIC_KEY  = "MusicVolume";

    // Runs once before any scene loads — instantiates from Resources/AudioManager
    // if no instance exists yet (e.g. entering Game scene directly in the Editor).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null) return;

        var prefab = Resources.Load<GameObject>("AudioManager");
        if (prefab != null)
        {
            Instantiate(prefab); // Awake sets Instance + DontDestroyOnLoad
        }
        else
        {
            Debug.LogWarning("[AudioManager] No instance found and no prefab at Resources/AudioManager. " +
                             "Place AudioManager prefab in Assets/Resources/ or add it to every starting scene.");
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log($"[AudioManager] Duplicate on '{gameObject.name}' — destroying. Surviving instance is '{Instance.gameObject.name}'.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[AudioManager] Initialized on '{gameObject.name}'. Music={Music != null}, SFX={SFX != null}, Mixer={Mixer != null}");
        LoadVolumes();

        // Re-apply volumes on every scene load — AudioMixer parameters can reset across scenes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsDucked = false;
        LoadVolumes();
    }

    // ── Playback ─────────────────────────────────────────────────────────────

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)  { Debug.LogWarning("[AudioManager] PlayMusic called with null clip"); return; }
        if (Music == null) { Debug.LogError("[AudioManager] PlayMusic: Music AudioSource is not assigned"); return; }
        if (Music.clip == clip && Music.isPlaying) return;
        Debug.Log($"[AudioManager] PlayMusic: {clip.name} (loop={loop})");
        Music.clip = clip;
        Music.loop = loop;
        Music.Play();
    }

    public void StopMusic() => Music.Stop();

    // Blocked while IsDucked — general game SFX should not play during death.
    public static bool IsDucked = false;

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (IsDucked) return;
        if (clip == null) { Debug.LogWarning("[AudioManager] PlaySFX called with null clip"); return; }
        if (SFX == null)  { Debug.LogError("[AudioManager] PlaySFX: SFX AudioSource is not assigned"); return; }
        Debug.Log($"[AudioManager] PlaySFX: {clip.name}");
        SFX.PlayOneShot(clip, volume);
    }

    // Bypasses IsDucked — use for the death clip itself.
    public void PlaySFXDirect(AudioClip clip, float volume = 1f)
    {
        if (clip == null) { Debug.LogWarning("[AudioManager] PlaySFXDirect called with null clip"); return; }
        if (SFX == null)  { Debug.LogError("[AudioManager] PlaySFXDirect: SFX AudioSource is not assigned"); return; }
        SFX.PlayOneShot(clip, volume);
    }

    // ── Ducking ───────────────────────────────────────────────────────────────

    // Lowers mixer levels without writing to PlayerPrefs so user settings are preserved.
    public void DuckAudio(float musicLevel = 0.1f, float sfxLevel = 0.1f)
    {
        IsDucked = true;
        if (Mixer != null)
        {
            Mixer.SetFloat(MUSIC_KEY, ToDb(musicLevel));
            Mixer.SetFloat(SFX_KEY,   ToDb(sfxLevel));
        }
    }

    // Restores mixer to whatever PlayerPrefs say (untouched by ducking).
    public void RestoreAudio()
    {
        IsDucked = false;
        LoadVolumes();
    }

    // ── Volume ────────────────────────────────────────────────────────────────

    public void SetMasterVolume(float v)
    {
        if (Instance == null) { Debug.LogError("[AudioManager] SetMasterVolume called but Instance is null"); return; }
        if (Mixer != null) Mixer.SetFloat(MASTER_KEY, ToDb(v));
        else Debug.LogWarning("[AudioManager] SetMasterVolume: Mixer is not assigned");
        PlayerPrefs.SetFloat(MASTER_KEY, v);
    }

    public void SetMusicVolume(float v)
    {
        if (Instance == null) { Debug.LogError("[AudioManager] SetMusicVolume called but Instance is null"); return; }
        if (Mixer != null) Mixer.SetFloat(MUSIC_KEY, ToDb(v));
        else Debug.LogWarning("[AudioManager] SetMusicVolume: Mixer is not assigned");
        PlayerPrefs.SetFloat(MUSIC_KEY, v);
    }

    public void SetSFXVolume(float v)
    {
        if (Instance == null) { Debug.LogError("[AudioManager] SetSFXVolume called but Instance is null"); return; }
        if (Mixer != null) Mixer.SetFloat(SFX_KEY, ToDb(v));
        else Debug.LogWarning("[AudioManager] SetSFXVolume: Mixer is not assigned");
        PlayerPrefs.SetFloat(SFX_KEY, v);
    }

    private void LoadVolumes()
    {
        if (Mixer == null) return;
        Mixer.SetFloat(MASTER_KEY, ToDb(PlayerPrefs.GetFloat(MASTER_KEY, 1f)));
        Mixer.SetFloat(MUSIC_KEY,  ToDb(PlayerPrefs.GetFloat(MUSIC_KEY,  1f)));
        Mixer.SetFloat(SFX_KEY,    ToDb(PlayerPrefs.GetFloat(SFX_KEY,    1f)));
    }

    private static float ToDb(float linear) =>
        Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
}
