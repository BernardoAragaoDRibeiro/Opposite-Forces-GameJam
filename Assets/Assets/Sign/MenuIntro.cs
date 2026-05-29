using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MenuIntro : MonoBehaviour
{
    [Header("Fade In")]
    public Image fadeOverlay;           // Image preta cobrindo a tela inteira
    public float fadeDuration = 1.2f;

    [Header("Itens do Menu")]
    public List<RectTransform> menuItems = new List<RectTransform>(); // arraste os textos aqui em ordem
    public float slideOffsetX   = -80f;  // de onde vêm (negativo = vêm da esquerda)
    public float itemDelay      = 0.12f; // delay entre cada item
    public float slideDuration  = 0.4f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Delay inicial antes de tudo começar")]
    public float startDelay = 0.2f;

    // Posições originais dos itens (salvas no Awake)
    private List<Vector2> originalPositions = new List<Vector2>();

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        // Espera o Canvas finalizar o layout antes de salvar posições
        yield return null;
        yield return null;

        // Salva posições originais agora que o layout está correto
        originalPositions.Clear();
        foreach (RectTransform item in menuItems)
        {
            originalPositions.Add(item.anchoredPosition);

            CanvasGroup cg = item.GetComponent<CanvasGroup>();
            if (cg == null) cg = item.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            item.anchoredPosition += new Vector2(slideOffsetX, 0f);
        }

        // Overlay preto
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        yield return new WaitForSecondsRealtime(startDelay);

        // Fade in do overlay (preto dissolve)
        if (fadeOverlay != null)
            yield return StartCoroutine(FadeOverlay(1f, 0f, fadeDuration));

        // Itens entram em sequência
        for (int i = 0; i < menuItems.Count; i++)
        {
            StartCoroutine(SlideItem(menuItems[i], originalPositions[i], slideDuration));
            yield return new WaitForSecondsRealtime(itemDelay);
        }
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        float elapsed = 0f;
        Color c = fadeOverlay.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = to;
        fadeOverlay.color = c;

        // Desativa o overlay quando invisível para não bloquear cliques
        if (to <= 0f) fadeOverlay.gameObject.SetActive(false);
    }

    private IEnumerator SlideItem(RectTransform item, Vector2 targetPos, float duration)
    {
        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        Vector2 startPos = item.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = slideCurve.Evaluate(t);

            item.anchoredPosition = Vector2.Lerp(startPos, targetPos, curved);
            if (cg != null) cg.alpha = curved;

            yield return null;
        }

        item.anchoredPosition = targetPos;
        if (cg != null) cg.alpha = 1f;
    }
}