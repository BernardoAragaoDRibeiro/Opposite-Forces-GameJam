using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }

    private const string HIGHSCORE_KEY = "HighScore";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        HighScore = PlayerPrefs.GetInt(HIGHSCORE_KEY, 0);
    }

    public void AddScore(int points)
    {
        CurrentScore += points;

        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            PlayerPrefs.SetInt(HIGHSCORE_KEY, HighScore);
            PlayerPrefs.Save();
        }
        Debug.Log(CurrentScore);
    }

    public void ResetScore()
    {
        CurrentScore = 0;
    }
}