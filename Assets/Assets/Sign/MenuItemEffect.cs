using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class MenuItemEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referências")]
    public TextMeshProUGUI label;

    [Header("Cores")]
    public Color colorNormal    = Color.white;
    public Color colorHover     = new Color(1f, 0.85f, 0.4f); // amarelo dourado
    public Color colorClick     = new Color(1f, 0.4f, 0.4f);  // vermelho

    [Header("Escala")]
    public float hoverScale     = 1.08f;
    public float clickScale     = 0.95f;
    public float scaleSpeed     = 10f;

    [Header("Offset de hover (pixels)")]
    public float hoverOffsetX   = 6f;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private Vector3 targetScale;
    private Vector3 targetPosition;
    private Color   targetColor;
    private bool    isHovered   = false;

    private void Awake()
    {
        if (label == null) label = GetComponent<TextMeshProUGUI>();
        baseScale    = transform.localScale;
        basePosition = transform.localPosition;
        targetScale    = baseScale;
        targetPosition = basePosition;
        targetColor    = colorNormal;
        if (label) label.color = colorNormal;
    }

    private void Update()
    {
        // Interpola escala e posição suavemente
        transform.localScale    = Vector3.Lerp(transform.localScale,    targetScale,    Time.unscaledDeltaTime * scaleSpeed);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.unscaledDeltaTime * scaleSpeed);

        // Interpola cor
        if (label) label.color = Color.Lerp(label.color, targetColor, Time.unscaledDeltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered      = true;
        targetScale    = baseScale * hoverScale;
        targetPosition = basePosition + new Vector3(hoverOffsetX, 0f, 0f);
        targetColor    = colorHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered      = false;
        targetScale    = baseScale;
        targetPosition = basePosition;
        targetColor    = colorNormal;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = baseScale * clickScale;
        targetColor = colorClick;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Volta pro estado de hover se ainda estiver em cima
        targetScale = isHovered ? baseScale * hoverScale : baseScale;
        targetColor = isHovered ? colorHover : colorNormal;
    }
}