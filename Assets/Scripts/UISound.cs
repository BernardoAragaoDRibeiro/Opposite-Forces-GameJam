using UnityEngine;

public class UISound : MonoBehaviour
{
    public AudioClip AcceptClip;
    public AudioClip BackClip;
    public AudioClip SelectClip;

    public void PlayAccept()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AcceptClip);
    }

    public void PlayBack()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(BackClip);
    }

    public void PlaySelect()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SelectClip);
    }
}