using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopContoller : MonoBehaviour
{
    public GameObject DiceTypePosition1;
    public GameObject DiceTypePosition2;
    public GameObject DiceTypePosition3;

    public GameObject PipTypePosition1;
    public GameObject PipTypePosition2;
    public GameObject PipTypePosition3;

    public GameObject ConstellationTypePos1;
    public GameObject ConstellationTypePos2;
    public GameObject ConstellationTypePos3;

    public GameObject RerollButton;
    public GameObject NextRoundButton;

    public PlayerData playerData;

    public List<DiceData> Dice = new List<DiceData>();

    public List<ShopItemData> allShopItems;
    public GameObject shopItemPrefab;

    [Header("Dice Panel Swapping")]
    public GameObject DicePannel;
    [SerializeField] private float activeDiceSpacing = 2.0f;
    [SerializeField] private float dragZOffset = -0.1f;
    [SerializeField] private float swapDuration = 0.3f;
    [SerializeField] private AnimationCurve swapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float snapDistance = 2f;

    private Camera mainCam;
    private DiceData currentlyDragging;
    private Vector3 dragOffset;
    private Vector3 originalPosition;
    private int originalSlotIndex;
    private bool isDragging = false;
    private List<Transform> diceSlots = new List<Transform>();

    public void Start()
    {
        mainCam = Camera.main;
        playerData = PlayerData.Instance;
        positonDice();

        //GenerateShopItems();
    }

    void Update()
    {
        HandleDragInput();
    }

    void positonDice()
    {
        List<DiceData> temp = playerData.dice;

        int count = 0;
        Vector3 activeDiceCenter = DicePannel.transform.position;
        float startX = activeDiceCenter.x - (6 - 1) * activeDiceSpacing / 2f;
        float starty = activeDiceCenter.y;

        foreach (var die in temp)
        {
            Dice.Add(die);
            die.gameObject.transform.position = new Vector3(startX + count * activeDiceSpacing, starty, -9.5f);
            count++;
        }

        SetupDiceSlots();
    }

    void SetupDiceSlots()
    {
        diceSlots.Clear();

        Vector3 activeDiceCenter = DicePannel.transform.position;
        float startX = activeDiceCenter.x - (6 - 1) * activeDiceSpacing / 2f;
        float starty = activeDiceCenter.y;

        for (int i = 0; i < Dice.Count; i++)
        {
            GameObject slotMarker = new GameObject($"DiceSlot{i}");
            slotMarker.transform.position = new Vector3(startX + i * activeDiceSpacing, starty, -9.5f);
            slotMarker.transform.parent = DicePannel.transform;
            diceSlots.Add(slotMarker.transform);
        }
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

    #region Dice Panel Swapping

    void HandleDragInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
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
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPosition);
        worldPos.z = 0;

        for (int i = 0; i < Dice.Count; i++)
        {
            DiceData dice = Dice[i];
            if (dice == null) continue;

            Collider2D col = dice.GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldPos))
            {
                if (dice.rolling)
                {
                    Debug.Log("Can't drag, dice is rolling");
                    return;
                }

                currentlyDragging = dice;
                originalPosition = dice.transform.position;
                originalSlotIndex = i;
                isDragging = true;

                dragOffset = dice.transform.position - worldPos;
                dragOffset.z = 0;

                Debug.Log($"Started dragging dice at index {i}");
                break;
            }
        }
    }

    void UpdateDragPosition()
    {
        if (currentlyDragging == null) return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPosition);
        worldPos.z = originalPosition.z + dragZOffset;

        currentlyDragging.transform.position = worldPos + dragOffset;
    }

    void EndDrag()
    {
        if (currentlyDragging == null)
        {
            isDragging = false;
            return;
        }

        int closestSlot = FindClosestSlot(currentlyDragging.transform.position);

        Debug.Log($"Dropped at closest slot: {closestSlot}, original slot: {originalSlotIndex}");

        if (closestSlot >= 0 && closestSlot != originalSlotIndex)
        {
            Debug.Log($"Shifting dice from {originalSlotIndex} to {closestSlot}");
            ShiftDice(originalSlotIndex, closestSlot);
        }
        else
        {
            Debug.Log("Snapping back to original position");
            StartCoroutine(MoveToPosition(currentlyDragging, originalPosition));
        }

        currentlyDragging = null;
        isDragging = false;
    }

    int FindClosestSlot(Vector3 position)
    {
        int closestIndex = -1;
        float closestDistance = snapDistance;

        for (int i = 0; i < diceSlots.Count; i++)
        {
            if (diceSlots[i] == null) continue;

            float distance = Vector3.Distance(position, diceSlots[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    void ShiftDice(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Dice.Count ||
            toIndex < 0 || toIndex >= Dice.Count)
            return;

        DiceData movingDice = Dice[fromIndex];

        // Moving left (to lower index)
        if (toIndex < fromIndex)
        {
            // Shift dice right: 0->1, 1->2, 2->3
            for (int i = fromIndex; i > toIndex; i--)
            {
                Dice[i] = Dice[i - 1];
                playerData.dice[i] = playerData.dice[i - 1];

                if (Dice[i] != null)
                    StartCoroutine(MoveToPosition(Dice[i], diceSlots[i].position));
            }
        }
        // Moving right (to higher index)
        else
        {
            // Shift dice left: 3->2, 2->1, 1->0
            for (int i = fromIndex; i < toIndex; i++)
            {
                Dice[i] = Dice[i + 1];
                playerData.dice[i] = playerData.dice[i + 1];

                if (Dice[i] != null)
                    StartCoroutine(MoveToPosition(Dice[i], diceSlots[i].position));
            }
        }

        // Place the dragged dice in the target slot
        Dice[toIndex] = movingDice;
        playerData.dice[toIndex] = movingDice;

        if (movingDice != null)
            StartCoroutine(MoveToPosition(movingDice, diceSlots[toIndex].position));
    }

    IEnumerator MoveToPosition(DiceData dice, Vector3 targetPosition)
    {
        if (dice == null) yield break;

        Vector3 startPosition = dice.transform.position;
        startPosition.z = targetPosition.z;

        float elapsed = 0f;

        while (elapsed < swapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swapDuration;
            float curveValue = swapCurve.Evaluate(t);

            Vector3 newPos = Vector3.Lerp(startPosition, targetPosition, curveValue);
            dice.transform.position = newPos;

            yield return null;
        }

        dice.transform.position = targetPosition;
    }

    public void MoveDiceToNextSlot(int diceIndex)
    {
        if (diceIndex < 0 || diceIndex >= Dice.Count - 1)
            return;

        ShiftDice(diceIndex, diceIndex + 1);
    }

    public void MoveDiceToPreviousSlot(int diceIndex)
    {
        if (diceIndex <= 0 || diceIndex >= Dice.Count)
            return;

        ShiftDice(diceIndex, diceIndex - 1);
    }

    public void MoveDiceToSlot(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;

        ShiftDice(fromIndex, toIndex);
    }

    #endregion
}
