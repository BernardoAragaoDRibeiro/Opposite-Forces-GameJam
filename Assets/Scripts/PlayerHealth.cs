using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public float MaxHP = 100f;
    [field: SerializeField]
    public float CurrentHP { get; private set; }

    [Header("Morte")]
    public AudioClip[] DeathClips;
    [Range(0f, 1f)] public float DeathVolume = 1f;
    public TMPro.TextMeshProUGUI GameOverText;

    [Header("Fora dos limites")]
    public float MinYBoundary = -10f;
    public Transform SpawnPoint;

    private bool _isDead = false;
    private CharacterController _cc;

    private void Start()
    {
        CurrentHP = MaxHP;
        _cc       = GetComponent<CharacterController>();

        if (GameOverText != null)
            GameOverText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isDead && transform.position.y < MinYBoundary)
            Respawn();
    }

    public void TakeDamage(float damage)
    {
        if (_isDead) return;
        CurrentHP = Mathf.Max(CurrentHP - damage, 0f);
        if (CurrentHP <= 0f) Die();
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        // Duck all audio immediately
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(0.1f);
            AudioManager.Instance.SetSFXVolume(0.1f);
        }

        // Play death clip directly — bypasses any IsDucked guard
        if (DeathClips.Length > 0 && AudioManager.Instance != null)
        {
            AudioClip clip = DeathClips[Random.Range(0, DeathClips.Length)];
            AudioManager.Instance.SFX.PlayOneShot(clip, DeathVolume);
        }

        // Fade in GAME OVER text
        if (GameOverText != null)
            yield return FadeInGameOver(GameOverText, 1f);
        else
            yield return new WaitForSecondsRealtime(1f);

        // Hold for one more second
        yield return new WaitForSecondsRealtime(1f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator FadeInGameOver(TMPro.TextMeshProUGUI text, float duration)
    {
        Color c = text.color;
        c.a = 0f;
        text.color = c;
        text.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a      = Mathf.Clamp01(elapsed / duration);
            text.color = c;
            yield return null;
        }
        c.a = 1f;
        text.color = c;
    }

    private void Respawn()
    {
        Vector3 safePos = SpawnPoint != null ? SpawnPoint.position : new Vector3(0f, 2f, 0f);
        if (_cc != null) _cc.enabled = false;
        transform.position = safePos;
        if (_cc != null) _cc.enabled = true;
    }
}
