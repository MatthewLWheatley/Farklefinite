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
            popupCoroutine = StartCoroutine(ShowPopup(anim, sourceObject.transform.position));
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

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalCameraPos;
    }

    private IEnumerator ShowPopup(AbilityAnimation anim, Vector3 worldPosition)
    {
        GameObject popupPrefab = anim.popupTextPrefab != null ? anim.popupTextPrefab : defaultPopupPrefab;
        if (popupPrefab == null || uiCanvas == null) yield break;

        GameObject popup = Instantiate(popupPrefab, uiCanvas.transform);
        RectTransform rectTransform = popup.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);

            screenPos.x += rectTransform.sizeDelta.x / 2;

            rectTransform.anchoredPosition = screenPos; 
            Debug.Log($"World: {worldPosition}, Screen: {screenPos}");
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

        Vector3 startPos = rect.position;
        Vector3 endPos = startPos + new Vector3(0, 1, 0);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            if (rect != null)
                rect.position = Vector3.Lerp(startPos, endPos, t);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}