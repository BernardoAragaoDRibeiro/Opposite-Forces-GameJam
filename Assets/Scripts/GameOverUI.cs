using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI HighScoreText;

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreText.text = $"Score: {ScoreManager.Instance.CurrentScore}";
            HighScoreText.text = $"Best: {ScoreManager.Instance.HighScore}";
        }
    }
}