using UnityEngine;

public class GameMusic : MonoBehaviour
{
    public AudioClip GameMusicClip;

    private void Start()
    {
        if (GameMusicClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(GameMusicClip);
    }
}
