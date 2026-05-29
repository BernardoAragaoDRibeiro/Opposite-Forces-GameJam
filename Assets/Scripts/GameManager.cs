using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetScore();
        SceneManager.LoadScene("Game");
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToGameOver()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameOver");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}