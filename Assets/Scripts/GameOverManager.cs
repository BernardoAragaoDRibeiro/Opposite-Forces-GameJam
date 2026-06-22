using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public float AutoLoadDelay = 3f;

    private void Start()
    {
        // Restore volumes that were ducked during the death sequence
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
            AudioManager.Instance.SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume",   1f));
            AudioManager.Instance.SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume",       1f));
        }

        StartCoroutine(AutoLoadMainMenu());
    }

    private IEnumerator AutoLoadMainMenu()
    {
        yield return new WaitForSecondsRealtime(AutoLoadDelay);
        GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            SceneManager.LoadScene("MainMenu");
    }
}
