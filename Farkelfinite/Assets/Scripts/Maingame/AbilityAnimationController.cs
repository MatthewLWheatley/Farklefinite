using System.Collections;
using TMPro;
using UnityEngine;

public class AbilityAnimationController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Canvas uiCanvas;
    public AudioSource audioSource;

    [Header("Popup Settings")]
    public GameObject defaultPopupPrefab;

    private Vector3 originalCameraPos;

    [Header("Screen Shake")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private RectTransform canvasRect;
    private Vector3 originalCanvasPos;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (uiCanvas == null)
            uiCanvas = FindFirstObjectByType<Canvas>();

        if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            if (uiCanvas.worldCamera == null)
            {
                uiCanvas.worldCamera = mainCamera;
                Debug.Log("AbilityAnimationController: Assigned camera to canvas");
            }
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (mainCamera != null)
            originalCameraPos = mainCamera.transform.position;

        mainCanvas = PlayerData.Instance.gameObject.transform.GetChild(0).GetComponent<Canvas>();
        canvasRect = PlayerData.Instance.gameObject.transform.GetChild(0).GetComponent<RectTransform>();
        originalCanvasPos = canvasRect.anchoredPosition;

        if (mainCanvas != null)
        {
            
        }
    }

    public IEnumerator PlayAbilityAnimation(DiceAbility ability, GameObject sourceObject)
    {
        AbilityAnimation anim = ability.abilityAnimation;
        if (anim == null) yield break;

        if (anim.startDelay > 0)
            yield return new WaitForSeconds(anim.startDelay);

        Coroutine particleCoroutine = null;
        Coroutine shakeCoroutine = null;
        Coroutine popupCoroutine = null;

        if (anim.particlePrefab != null)
        {
            particleCoroutine = StartCoroutine(PlayParticles(anim, sourceObject.transform.position));
        }

        if (anim.enableScreenShake)
        {
            shakeCoroutine = StartCoroutine(ScreenShake(anim.shakeIntensity, anim.shakeDuration));
        }

        if (anim.popupTextPrefab != null || defaultPopupPrefab != null)
        {
            popupCoroutine = StartCoroutine(ShowPopup(anim, sourceObject));
        }

        if (anim.soundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(anim.soundEffect, anim.volume);
        }

        float waitTime = anim.totalDuration > 0 ? anim.totalDuration : anim.particleDuration;
        yield return new WaitForSeconds(waitTime);
    }

    private IEnumerator PlayParticles(AbilityAnimation anim, Vector3 position)
    {
        Vector3 spawnPos = position + anim.spawnOffset;
        GameObject particles = Instantiate(anim.particlePrefab, spawnPos, Quaternion.identity);

        ParticleSystem ps = particles.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }

        yield return new WaitForSeconds(anim.particleDuration);

        Destroy(particles);
    }

    private IEnumerator ScreenShake(float intensity, float duration)
    {
        if (mainCamera == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            mainCamera.transform.position = originalCameraPos + new Vector3(x, y, 0);
            canvasRect.anchoredPosition = originalCanvasPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalCameraPos;
    }

    private IEnumerator ShowPopup(AbilityAnimation anim, GameObject Dice)
    {
        GameObject popupPrefab = anim.popupTextPrefab != null ? anim.popupTextPrefab : defaultPopupPrefab;
        if (popupPrefab == null) yield break;

        RectTransform diceRect = Dice.GetComponent<RectTransform>();
        if (diceRect == null) yield break;

        Canvas diceCanvas = diceRect.GetComponentInParent<Canvas>();
        if (diceCanvas == null) yield break;

        GameObject popup = Instantiate(popupPrefab, diceCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();

        if (popupRect != null)
        {
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);

            Vector3 diceWorldPos = diceRect.position;

            Vector3 localPos = diceCanvas.transform.InverseTransformPoint(diceWorldPos);

            popupRect.localPosition = localPos + new Vector3(0, 100f, 0);

            Debug.Log($"Dice world: {diceWorldPos}, Popup local: {localPos}");
        }

        TMP_Text text = popup.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = anim.popupText;
            text.color = anim.popupColor;
        }

        yield return StartCoroutine(AnimatePopup(popup, 1f));

        Destroy(popup);
    }

    private IEnumerator AnimatePopup(GameObject popup, float duration)
    {
        RectTransform rect = popup.GetComponent<RectTransform>();
        TMP_Text text = popup.GetComponentInChildren<TMP_Text>();
        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = popup.AddComponent<CanvasGroup>();

        Vector2 startPos = rect.anchoredPosition;
        startPos.y -= 150.0f;
        Vector2 endPos = startPos + new Vector2(0, 100f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            if (rect != null)
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}