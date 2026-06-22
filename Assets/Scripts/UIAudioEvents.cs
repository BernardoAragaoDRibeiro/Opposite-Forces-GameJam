using UnityEngine;
using UnityEngine.EventSystems;

public class UIAudioEvents : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public AudioClip HoverClip;
    public AudioClip ClickClip;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (HoverClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(HoverClip);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ClickClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(ClickClip);
    }
}
