using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShopItemType
{
    DiceType,
    Pip,
    Constelation
}

public class ShopItem : MonoBehaviour
{
    public ShopItemType item;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCam;
    private float originalZ;
    private Collider2D col;
    private Vector3 originalPosition;

    [SerializeField] private float snapBackDuration = 0.125f;

    void Start()
    {
        mainCam = Camera.main;
        originalZ = transform.position.z;
        originalPosition = transform.position;
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPosition);
            worldPos.z = 0;

            if (col.OverlapPoint(worldPos))
            {
                offset = transform.position - worldPos;
                offset.z = 0;
                isDragging = true;
            }
        }

        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPosition);
            worldPos.z = originalZ;

            transform.position = worldPos + offset;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            StartCoroutine(SnapBackToOriginal());
        }
    }

    private IEnumerator SnapBackToOriginal()
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < snapBackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / snapBackDuration;
            transform.position = Vector3.Lerp(startPos, originalPosition, t);
            yield return null;
        }

        transform.position = originalPosition;
    }
}