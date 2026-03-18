using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum ShopItemType
{
    DiceType,
    Pip,
    Constelation
}

public class ShopItem : MonoBehaviour
{
    public ShopItemType item;
    public ShopItemData itemData;

    [SerializeField] private float snapBackDuration = 0.125f;

    private bool isDragging = false;
    private Vector2 dragOffset;
    private Vector2 originalAnchoredPosition;
    private int originalSiblingIndex;
    private Transform originalParent;
    private Camera mainCam;
    private Canvas canvas;
    private RectTransform rectTransform;

    public event Action<ShopItem> OnDragStart;
    public event Action<ShopItem> OnDragEnd;
    public event Action<ShopItem, GameObject> OnDroppedOn;

    void Awake()
    {
        mainCam = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
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
        Touch touch = Input.GetTouch(0);

        if ((Mouse.current.leftButton.wasPressedThisFrame || touch.phase == UnityEngine.TouchPhase.Began) && !isDragging)
        {
            TryStartDrag();
        }

        if (isDragging && (Mouse.current.leftButton.isPressed || touch.phase == UnityEngine.TouchPhase.Moved))
        {
            UpdateDragPosition();
        }

        if ((Mouse.current.leftButton.wasReleasedThisFrame || touch.phase == UnityEngine.TouchPhase.Ended) && isDragging)
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

        if (rectTransform.rect.Contains(localPoint))
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

        rectTransform.anchoredPosition = canvasPoint + dragOffset;
    }

    void EndDrag()
    {
        isDragging = false;

        GameObject dropTarget = FindDropTargetAtPosition();

        if (dropTarget != null)
        {
            OnDroppedOn?.Invoke(this, dropTarget);
        }

        StartCoroutine(SnapBack());

        OnDragEnd?.Invoke(this);
    }

    GameObject FindDropTargetAtPosition()
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

        var pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = mousePos;

        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject == this.gameObject) continue;

            DiceData diceData = result.gameObject.GetComponent<DiceData>();
            if (diceData != null)
            {
                return result.gameObject;
            }

            PipDropTarget pipTarget = result.gameObject.GetComponent<PipDropTarget>();
            if (pipTarget != null)
            {
                return result.gameObject;
            }
        }

        return null;
    }

    IEnumerator SnapBack()
    {
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);

        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < snapBackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / snapBackDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalAnchoredPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    public void Init(ShopItemData data)
    {
        itemData = data;
        item = data.itemType;
        GetComponent<Image>().sprite = data.itemSprite;
    }

    public void SetOriginalPosition(Vector2 pos)
    {
        originalAnchoredPosition = pos;
    }

    public bool IsDragging()
    {
        return isDragging;
    }
}
