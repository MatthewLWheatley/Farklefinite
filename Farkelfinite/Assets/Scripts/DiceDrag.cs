using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchPhase = UnityEngine.TouchPhase;

public class DiceDrag : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float snapBackDuration = 0.3f;
    [SerializeField] private AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Drop Zone Detection")]
    [SerializeField] private float dropZoneCheckRadius = 50f;
    [SerializeField] private LayerMask dropZoneLayer;

    private Camera mainCam;
    public Canvas canvas;
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
        var mouse = Mouse.current;
        var touch = Touchscreen.current?.primaryTouch;

        bool started = (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                       (touch != null && touch.press.wasPressedThisFrame);

        if ((started) && !isDragging)
        {
            TryStartDrag();
        }

        if (isDragging && (Mouse.current.leftButton.isPressed || touch.press.IsPressed()))
        {
            UpdateDragPosition();
        }

        if (Mouse.current != null && isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
        }

        if (Mouse.current != null && isDragging && !touch.press.isPressed)
        {
            EndDrag();
        }

        if ((Mouse.current.leftButton.wasReleasedThisFrame || touch.press.IsPressed()) && isDragging)
        {
            EndDrag();
        }
    }

    void TryStartDrag()
    {
        var mouse = Mouse.current;
        var touch = Touchscreen.current?.primaryTouch;

        bool mousetouched = (mouse != null && mouse.leftButton.wasPressedThisFrame);
        bool touchPrssed = (touch != null && touch.press.wasPressedThisFrame);

        Vector2 mousePos;
        if (mousetouched)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        else if (touchPrssed)
        {
            mousePos = touch.position.ReadValue();
        }
        else
        {
            return;
        }

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
        var mouse = Mouse.current;
        var touch = Touchscreen.current?.primaryTouch;

        bool mousetouched = (mouse != null && mouse.leftButton.wasPressedThisFrame);
        bool touchPrssed = (touch != null && touch.press.wasPressedThisFrame);

        Vector2 mousePos;
        if (mousetouched)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        else if (touchPrssed)
        {
            mousePos = touch.position.ReadValue();
        }
        else
        {
            return;
        }

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

            if (this == null || gameObject == null)
            {
                OnDragEnd?.Invoke(this);
                return;
            }
        }

        StartCoroutine(SnapBack());
        OnDragEnd?.Invoke(this);
    }

    GameObject FindDropZoneAtPosition()
    {
        var mouse = Mouse.current;
        var touch = Touchscreen.current?.primaryTouch;

        bool mousetouched = (mouse != null && mouse.leftButton.wasPressedThisFrame);
        bool touchPrssed = (touch != null && touch.press.wasPressedThisFrame);

        Vector2 mousePos;
        if (mousetouched)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        else if (touchPrssed)
        {
            mousePos = touch.position.ReadValue();
        }
        else
        {
            return null;
        }

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
