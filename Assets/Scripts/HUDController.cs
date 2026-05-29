using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("HP")]
    public Image HPFill;
    public TextMeshProUGUI HPText;

    [Header("Score")]
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI HighScoreText;

    private PlayerHealth _playerHealth;

    private void Start()
    {
        _playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (_playerHealth != null)
        {
            float ratio = _playerHealth.CurrentHP / _playerHealth.MaxHP;
            HPFill.fillAmount = ratio;
            HPText.text = $"{Mathf.CeilToInt(_playerHealth.CurrentHP)} / {_playerHealth.MaxHP}";
        }

        if (ScoreManager.Instance != null)
        {
            ScoreText.text = $"Score: {ScoreManager.Instance.CurrentScore}";
            HighScoreText.text = $"Best: {ScoreManager.Instance.HighScore}";
        }
    }
}