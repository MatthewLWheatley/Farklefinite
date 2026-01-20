using UnityEngine;
using UnityEngine.EventSystems;

public class DiceDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private ShopContoller shopController;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvas = GetComponentInParent<Canvas>();
        shopController = FindObjectOfType<ShopContoller>();

        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;

        if (shopController != null)
        {
            shopController.OnDiceBeginDrag(GetComponent<DiceData>());
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        if (shopController != null)
        {
            shopController.OnDiceDragging(GetComponent<DiceData>());
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (shopController != null)
        {
            shopController.OnDiceEndDrag(GetComponent<DiceData>());
        }
    }
}