using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class DiceDrag : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float snapBackDuration = 0.3f;
    [SerializeField] private AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Drop Zone Detection")]
    [SerializeField] private float dropZoneCheckRadius = 50f;
    [SerializeField] private LayerMask dropZoneLayer;

    private Camera mainCam;
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector3 dragOffset;
    private Vector2 originalAnchoredPosition;
    private bool isDragging = false;
    private bool isSnapingBack = false;
    private int originalSiblingIndex;
    private Transform originalParent;

    public event Action<DiceDrag> OnDragStart;
    public event Action<DiceDrag> OnDragEnd;
    public event Action<DiceDrag, GameObject> OnDroppedOn;

    private DiceData diceData;

    void Awake()
    {
        mainCam = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        diceData = GetComponent<DiceData>();
    }

    void Start()
    {
        originalAnchoredPosition = rectTransform.anchoredPosition;
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

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, mousePos, mainCam, out Vector2 localPoint);

        if (rectTransform.rect.Contains(localPoint) && !isSnapingBack)
        {
            isDragging = true;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalParent = transform.parent;

            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, mousePos, mainCam, out Vector2 canvasPoint);

            dragOffset = rectTransform.anchoredPosition - canvasPoint;

            OnDragStart?.Invoke(this);
        }
    }

    void UpdateDragPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, mousePos, mainCam, out Vector2 canvasPoint);

        rectTransform.anchoredPosition = canvasPoint + (Vector2)dragOffset;
    }

    void EndDrag()
    {
        isDragging = false;

        GameObject dropZone = FindDropZoneAtPosition();

        if (dropZone != null)
        {
            OnDroppedOn?.Invoke(this, dropZone);
        }

        StartCoroutine(SnapBack());

        OnDragEnd?.Invoke(this);
    }

    GameObject FindDropZoneAtPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        var pointerEventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointerEventData.position = mousePos;

        var raycastResults = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject != this.gameObject && result.gameObject.CompareTag("SellZone"))
            {
                return result.gameObject;
            }
        }

        return null;
    }

    IEnumerator SnapBack()
    {
        isSnapingBack = true;

        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);

        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < snapBackDuration)
        {
            elapsed += Time.deltaTime;
            float t = snapCurve.Evaluate(elapsed / snapBackDuration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalAnchoredPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = originalAnchoredPosition;

        isSnapingBack = false;
    }

    public void SetOriginalPosition(Vector2 pos)
    {
        originalAnchoredPosition = pos;
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    public DiceData GetDiceData()
    {
        return diceData;
    }
}
