using UnityEngine;

public class MainMenuMusic : MonoBehaviour
{
    public AudioClip MenuMusicClip;

    private void Start()
    {
        if (MenuMusicClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(MenuMusicClip);
    }
}
