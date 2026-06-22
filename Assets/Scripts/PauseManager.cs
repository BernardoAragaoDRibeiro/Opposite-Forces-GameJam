using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    public GameObject PauseCanvas;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (PauseCanvas != null)
            PauseCanvas.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        if (IsPaused) Unpause();
        else          Pause();
    }

    public void Pause()
    {
        IsPaused         = true;
        Time.timeScale   = 0f;
        if (PauseCanvas != null) PauseCanvas.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Unpause()
    {
        IsPaused       = false;
        Time.timeScale = 1f;
        if (PauseCanvas != null) PauseCanvas.SetActive(false);
        StartCoroutine(LockCursorNextFrame());
    }

    public void ReturnToMainMenu()
    {
        IsPaused              = false;
        Time.timeScale        = 1f;
        AudioListener.pause   = false;
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator LockCursorNextFrame()
    {
        yield return null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        yield return null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}