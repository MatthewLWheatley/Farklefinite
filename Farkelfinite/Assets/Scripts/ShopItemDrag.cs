using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class ShopItemDrag : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float dragZOffset = -0.1f;
    [SerializeField] private float snapBackDuration = 0.3f;
    [SerializeField] private AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Drop Zone Detection")]
    [SerializeField] private float dropZoneCheckRadius = 1f;
    [SerializeField] private LayerMask dropZoneLayer;

    private Camera mainCam;
    private Vector3 dragOffset;
    private Vector3 originalPosition;
    private bool isDragging = false;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private int originalSortingOrder;

    public event Action<ShopItemDrag> OnDragStart;
    public event Action<ShopItemDrag> OnDragEnd;
    public event Action<ShopItemDrag, GameObject> OnDroppedOn; 

    void Awake()
    {
        mainCam = Camera.main;
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (col == null)
        {
            Debug.LogError($"ShopItemDrag on {gameObject.name} needs a Collider2D, add one or your life will be pain");
        }
    }

    void Start()
    {
        originalPosition = transform.position;
        if (spriteRenderer != null)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
        }
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && !isDragging)
        {
            TryStartDrag();
        }

        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            UpdateDragPosition();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            EndDrag();
        }
    }

    void TryStartDrag()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;

        if (col.OverlapPoint(worldPos))
        {
            isDragging = true;
            originalPosition = transform.position;

            dragOffset = transform.position - worldPos;
            dragOffset.z = 0;

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 1000;
            }

            OnDragStart?.Invoke(this);
        }
    }

    void UpdateDragPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePos);
        worldPos.z = originalPosition.z + dragZOffset;

        transform.position = worldPos + dragOffset;
    }

    void EndDrag()
    {
        isDragging = false;

        GameObject dropZone = FindDropZoneAtPosition(transform.position);

        if (dropZone != null)
        {
            OnDroppedOn?.Invoke(this, dropZone);
        }
        else
        {
            StartCoroutine(SnapBack());
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
        }

        OnDragEnd?.Invoke(this);
    }

    GameObject FindDropZoneAtPosition(Vector3 position)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, dropZoneCheckRadius, dropZoneLayer);

        foreach (var hit in hits)
        {
            if (hit.gameObject != this.gameObject)
            {
                return hit.gameObject;
            }
        }

        return null;
    }

    IEnumerator SnapBack()
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < snapBackDuration)
        {
            elapsed += Time.deltaTime;
            float t = snapCurve.Evaluate(elapsed / snapBackDuration);
            transform.position = Vector3.Lerp(startPos, originalPosition, t);
            yield return null;
        }

        transform.position = originalPosition;
    }

    public void SetOriginalPosition(Vector3 pos)
    {
        originalPosition = pos;
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    public void ForceSnapTo(Vector3 targetPos)
    {
        StopAllCoroutines();
        StartCoroutine(SnapToPosition(targetPos));
    }

    IEnumerator SnapToPosition(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < snapBackDuration)
        {
            elapsed += Time.deltaTime;
            float t = snapCurve.Evaluate(elapsed / snapBackDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
        originalPosition = targetPos;
    }
}