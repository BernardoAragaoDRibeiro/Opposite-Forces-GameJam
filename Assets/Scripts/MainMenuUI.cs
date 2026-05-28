using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public GameObject OptionsPanel;

    public void OnPlayButton()
    {
        GameManager.Instance.StartGame();
    }

    public void OnOptionsButton()
    {
        OptionsPanel.SetActive(true);
    }

    public void OnQuitButton()
    {
        GameManager.Instance.QuitGame();
    }
}