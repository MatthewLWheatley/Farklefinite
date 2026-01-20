using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopContoller : MonoBehaviour
{
    public GameObject DiceTypePosition1;
    public GameObject DiceTypePosition2;
    public GameObject DiceTypePosition3;

    public GameObject PipTypePosition1;
    public GameObject PipTypePosition2;
    public GameObject PipTypePosition3;

    public GameObject RerollButton;
    public GameObject NextRoundButton;

    public PlayerData playerData;

    public List<DiceData> Dice = new List<DiceData>();

    public List<ShopItemData> allShopItems;
    public GameObject shopItemPrefab;

    [Header("Dice Panel Swapping")]
    public GameObject DicePannel;
    [SerializeField] private float activeDiceSpacing = 200f;
    private Canvas canvas;

    [Header("Dice Positioning")]
    public List<Vector2> dicePositions = new List<Vector2>();
    private DiceData currentlyDragging = null;
    private int lastHoveredIndex = -1;

    public void Start()
    {
        canvas = GetComponentInParent<Canvas>();

        playerData = PlayerData.Instance;
        positonDice();
    }

    void positonDice()
    {
        List<DiceData> temp = playerData.dice;
        RectTransform panelRect = DicePannel.GetComponent<RectTransform>();
        dicePositions.Clear();

        float width = panelRect.sizeDelta.x - activeDiceSpacing;
        float startX = -width / 2;

        for (int i = 0; i < temp.Count; i++)
        {
            float xPos = startX + (i * (width / (temp.Count - 1)));
            dicePositions.Add(new Vector2(xPos, 0));

            temp[i].transform.SetParent(DicePannel.transform, false);
            temp[i].GetComponent<RectTransform>().anchoredPosition = dicePositions[i];
        }
    }

    public void OnDiceBeginDrag(DiceData dice)
    {
        currentlyDragging = dice;
        lastHoveredIndex = playerData.dice.IndexOf(dice);
    }

    public void OnDiceDragging(DiceData dice)
    {
        if (currentlyDragging == null) return;

        int currentIndex = playerData.dice.IndexOf(dice);
        int hoveredIndex = GetHoveredSlotIndex(dice.GetComponent<RectTransform>());

        if (hoveredIndex != -1 && hoveredIndex != lastHoveredIndex)
        {
            // swap in the list
            playerData.dice.RemoveAt(currentIndex);
            playerData.dice.Insert(hoveredIndex, dice);

            lastHoveredIndex = hoveredIndex;

            // animate all OTHER dice to their new positions
            AnimateNonDraggedDice();
        }
    }

    public void OnDiceEndDrag(DiceData dice)
    {
        if (currentlyDragging == null) return;

        int finalIndex = playerData.dice.IndexOf(dice);
        StartCoroutine(SnapDiceToPosition(dice.GetComponent<RectTransform>(), dicePositions[finalIndex]));

        currentlyDragging = null;
        lastHoveredIndex = -1;
    }

    private int GetHoveredSlotIndex(RectTransform draggedRect)
    {
        float draggedX = draggedRect.anchoredPosition.x;
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < dicePositions.Count; i++)
        {
            float distance = Mathf.Abs(dicePositions[i].x - draggedX);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void AnimateNonDraggedDice()
    {
        for (int i = 0; i < playerData.dice.Count; i++)
        {
            if (playerData.dice[i] == currentlyDragging) continue;

            RectTransform rect = playerData.dice[i].GetComponent<RectTransform>();
            StartCoroutine(SmoothMove(rect, dicePositions[i], 0.2f));
        }
    }

    private IEnumerator SmoothMove(RectTransform rect, Vector2 targetPos, float duration)
    {
        Vector2 startPos = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        rect.anchoredPosition = targetPos;
    }

    private IEnumerator SnapDiceToPosition(RectTransform rect, Vector2 targetPos)
    {
        float duration = 0.15f;
        Vector2 startPos = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        rect.anchoredPosition = targetPos;
    }

    public void HandleDiceDropped(DiceData droppedDice, Vector3 originalPos)
    {
        int oldIndex = playerData.dice.IndexOf(droppedDice);
        int newIndex = CalculateInsertionIndex(droppedDice.GetComponent<RectTransform>());

        if (newIndex != oldIndex && newIndex >= 0)
        {
            playerData.dice.RemoveAt(oldIndex);
            playerData.dice.Insert(newIndex, droppedDice);
            positonDice();
            StartCoroutine(AnimateDiceSwap());
        }
        else
        {
            StartCoroutine(AnimateSingleDiceBack(droppedDice.GetComponent<RectTransform>(), originalPos));
        }
    }

    private int CalculateInsertionIndex(RectTransform draggedRect)
    {
        float draggedX = draggedRect.anchoredPosition.x;
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < dicePositions.Count; i++)
        {
            float distance = Mathf.Abs(dicePositions[i].x - draggedX);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private IEnumerator AnimateDiceSwap()
    {
        float duration = 0.3f;
        Dictionary<DiceData, Vector3> startPos = new Dictionary<DiceData, Vector3>();

        foreach (var dice in playerData.dice)
        {
            startPos[dice] = dice.transform.position; // world position
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            for (int i = 0; i < playerData.dice.Count; i++)
            {
                playerData.dice[i].transform.position = Vector3.Lerp(startPos[playerData.dice[i]], dicePositions[i], t);
            }

            yield return null;
        }
    }

    private IEnumerator AnimateSingleDiceBack(RectTransform diceRect, Vector2 originalPos)
    {
        float duration = 0.2f;
        Vector3 startPos = diceRect.position; // world position
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            diceRect.position = Vector3.Lerp(startPos, originalPos, elapsed / duration);
            yield return null;
        }

        diceRect.position = originalPos;
    }

    public void LoadUnlockedDiceType()
    {

    }

    public void GenerateShopItems()
    {
        GenerateDiceType();
    }

    private void GenerateDiceType()
    {
        List<ShopItemData> diceItems = GetItemsByType(ShopItemType.DiceType);

        if (diceItems.Count == 0)
        {
            Debug.LogWarning("No dice items found in allShopItems list");
            return;
        }

        List<ShopItemData> shuffledDice = new List<ShopItemData>(diceItems);
        for (int i = 0; i < shuffledDice.Count; i++)
        {
            ShopItemData temp = shuffledDice[i];
            int randomIndex = Random.Range(i, shuffledDice.Count);
            shuffledDice[i] = shuffledDice[randomIndex];
            shuffledDice[randomIndex] = temp;
        }

        if (DiceTypePosition1 != null && shuffledDice.Count > 0)
        {
            SpawnShopItem(shuffledDice[0], DiceTypePosition1.transform.position);
        }

        if (DiceTypePosition2 != null && shuffledDice.Count > 1)
        {
            SpawnShopItem(shuffledDice[1], DiceTypePosition2.transform.position);
        }

        if (DiceTypePosition3 != null && shuffledDice.Count > 2)
        {
            SpawnShopItem(shuffledDice[2], DiceTypePosition3.transform.position);
        }
    }

    public List<ShopItemData> GetItemsByType(ShopItemType type)
    {
        return allShopItems.FindAll(item => item.itemType == type);
    }

    public void SpawnShopItem(ShopItemData data, Vector3 position)
    {
        GameObject itemObj = Instantiate(shopItemPrefab, position, Quaternion.identity);
        ShopItem item = itemObj.GetComponent<ShopItem>();
        item.Init(data);
    }

}