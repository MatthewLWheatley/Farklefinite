using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    public GameObject moneyTextObject;
    public GameObject levelTextObject;
    public GameObject stageTextObject;
    public GameObject livesTextObject;
    public GameObject totalScoreTextObject;
    public GameObject runningScoreTextObject;
    public GameObject BankedScoreTextObject;

    public List<DiceData> Dice = new List<DiceData>();

    public List<ShopItemData> allShopItems;

    [Header("Dice Panel Swapping")]
    public GameObject DicePannel;
    public GameObject PipPannel;
    [SerializeField] private float activeDiceSpacing = 200f;
    private Canvas canvas;
    public GameObject buysellButton;

    [Header("Sell Settings")]
    public DiceConfig normalDiceConfig;
    public GameObject sellZone;

    public void Start()
    {
        canvas = GetComponentInParent<Canvas>();

        if (sellZone != null)
        {
            sellZone.SetActive(false);
        }

        playerData = PlayerData.Instance;
        positonDice();
        GenerateShopItems();
        SpawnPipBagDice();
        SpawnPipShopItems();
        ShowDiceDescription(0);
    }

    public void FixedUpdate()
    {
        moneyTextObject.GetComponent<TMP_Text>().text = $"Money: {playerData.money}";
        levelTextObject.GetComponent<TMP_Text>().text = $"Level: {playerData.currentLevel}";
        stageTextObject.GetComponent<TMP_Text>().text = $"Stage: {playerData.currentRound}";
        livesTextObject.GetComponent<TMP_Text>().text = $"Lives: {playerData.lives}";
        totalScoreTextObject.GetComponent<TMP_Text>().text = $"Next Level: {playerData.getNextLevelScoreThreshold(playerData.currentLevel + 1)}";
        runningScoreTextObject.GetComponent<TMP_Text>().text = $"";
        BankedScoreTextObject.GetComponent<TMP_Text>().text = $"Best Score: {playerData.bestScore}";
    }

    // ---------- shop item generation ----------

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

        if (DiceTypePosition1 != null)
        {
            SpawnShopItem(GenerateDiceTypeShopItem(), DiceTypePosition1);
        }

        if (DiceTypePosition2 != null)
        {
            SpawnShopItem(GenerateDiceTypeShopItem(), DiceTypePosition2);
        }

        if (DiceTypePosition3 != null)
        {
            SpawnShopItem(GenerateDiceTypeShopItem(), DiceTypePosition3);
        }
    }

    private ShopItemData GenerateDiceTypeShopItem()
    {
        List<ShopItemData> diceItems = GetItemsByType(ShopItemType.DiceType);


        playerData.dice.ForEach(diceData =>
        {
            diceItems.RemoveAll(item => item.diceConfig == diceData.diceConfig);
        });



        if (DiceTypePosition1.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == DiceTypePosition1.GetComponent<ShopItem>().itemData.diceConfig);
        if (DiceTypePosition2.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == DiceTypePosition2.GetComponent<ShopItem>().itemData.diceConfig);
        if (DiceTypePosition3.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == DiceTypePosition3.GetComponent<ShopItem>().itemData.diceConfig);
        if (PipTypePosition1.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == PipTypePosition1.GetComponent<ShopItem>().itemData.diceConfig);
        if (PipTypePosition2.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == PipTypePosition2.GetComponent<ShopItem>().itemData.diceConfig);
        if (PipTypePosition3.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == PipTypePosition3.GetComponent<ShopItem>().itemData.diceConfig);

        List<ShopItemData> diceItemsWeighted = new List<ShopItemData>();
        foreach (ShopItemData item in diceItems)
        {
            int weight = 6 - (int)item.weight;
            for (int i = 0; i < weight; i++)
            {
                diceItemsWeighted.Add(item);
            }

        }

        int randomIndex = Random.Range(0, diceItemsWeighted.Count);
        return diceItemsWeighted[randomIndex];
    }

    public List<ShopItemData> GetItemsByType(ShopItemType type)
    {
        return allShopItems.FindAll(item => item.itemType == type);
    }

    public void SpawnShopItem(ShopItemData data, GameObject itemObj)
    {
        ShopItem item = itemObj.GetComponent<ShopItem>();
        item.Init(data);
        SetUpShopTriggers(item.itemData, itemObj);
        SetupDraggableShopItem(item);
    }

    public void SetUpShopTriggers(ShopItemData shopData, GameObject itemObj)
    {
        EventTrigger trigger = itemObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = itemObj.transform.AddComponent<EventTrigger>();
        }

        //itemObj.GetComponent<Button>().onClick.AddListener(() => ShopDiceClicked(itemObj));

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { ShowDiceDescription(itemObj); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { HideDiceDescription(); });
        trigger.triggers.Add(entryExit);
    }

    public void ShowDiceDescription(GameObject itemObj)
    {
        if (!itemObj.activeSelf) return;
        ShopItemData data = itemObj.GetComponent<ShopItem>().itemData;
        if (data == null) return;

        string currentButtonText = buysellButton.transform.GetChild(0).GetComponent<TMP_Text>().text;
        if (currentButtonText != "Buy" && currentButtonText != "Sell")
        {
            buysellButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "";
        }

        descEnabled = false;
        DiceConfig die = data.diceConfig;

        if (nameObject != null)
            nameObject.GetComponent<TMP_Text>().text = die != null ? die.diceName : "Unknown Dice";

        if (descObject != null)
            descObject.GetComponent<TMP_Text>().text = die != null ? die.description : "No description";

        if (rarityObject != null)
            rarityObject.GetComponent<TMP_Text>().text = die != null ? data.weight.ToString() : "Rarity: ?";

        if (costObject != null)
            costObject.GetComponent<TMP_Text>().text = die != null ? data.cost.ToString() : "Cost: ?";

        if (data.itemType == ShopItemType.Pip)
        {
            nameObject.GetComponent<TMP_Text>().text = data.itemName;
            descObject.GetComponent<TMP_Text>().text = "Swap a face on a dice to pip: " + data.pipSprite.name.ToString().Substring(0, 1);
            rarityObject.GetComponent<TMP_Text>().text = data.weight.ToString();
            costObject.GetComponent<TMP_Text>().text = data.cost.ToString();

        }
    }

    public GameObject PipDicePrefab;
    public List<GameObject> pipDiceItems = new List<GameObject>();

    public void SpawnPipShopItems()
    {
        List<ShopItemData> pipItems = GetItemsByType(ShopItemType.Pip);


        if (PipTypePosition1 != null)
        {
            SpawnShopPipItem(GeneratePipTypeShopItem(), PipTypePosition1);
        }
        if (PipTypePosition2 != null)
        {
            SpawnShopPipItem(GeneratePipTypeShopItem(), PipTypePosition2);
        }
        if (PipTypePosition3 != null)
        {
            SpawnShopPipItem(GeneratePipTypeShopItem(), PipTypePosition3);
        }
    }

    public void SpawnShopPipItem(ShopItemData shopData, GameObject pipObject)
    {
        pipObject.transform.GetChild(0).gameObject.SetActive(true);
        pipObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = shopData.pipSprite;

        ShopItem shopItem = pipObject.GetComponent<ShopItem>();
        shopItem.itemData = shopData;
        shopItem.item = ShopItemType.Pip;

        EventTrigger trigger = pipObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = pipObject.transform.AddComponent<EventTrigger>();
        }

        //pipObject.GetComponent<Button>().onClick.AddListener(() => ShopPipDiceClicker(pipObject));

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { ShowDiceDescription(pipObject); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { HideDiceDescription(); });
        trigger.triggers.Add(entryExit);

        SetupDraggableShopItem(shopItem);
    }

    private ShopItemData GeneratePipTypeShopItem()
    {
        List<ShopItemData> PipItems = GetItemsByType(ShopItemType.Pip);

        if (PipTypePosition1.GetComponent<ShopItem>().itemData != null)
            PipItems.RemoveAll(item => item.pipSprite == PipTypePosition1.GetComponent<ShopItem>().itemData.pipSprite);
        if (PipTypePosition2.GetComponent<ShopItem>().itemData != null)
            PipItems.RemoveAll(item => item.pipSprite == PipTypePosition2.GetComponent<ShopItem>().itemData.pipSprite);
        if (PipTypePosition3.GetComponent<ShopItem>().itemData != null)
            PipItems.RemoveAll(item => item.pipSprite == PipTypePosition3.GetComponent<ShopItem>().itemData.pipSprite);

        List<ShopItemData> pipItemsWeighted = new List<ShopItemData>();
        foreach (ShopItemData item in PipItems)
        {
            int weight = 6 - (int)item.weight;
            for (int i = 0; i < weight; i++)
            {
                pipItemsWeighted.Add(item);
            }
        }

        int randomIndex = Random.Range(0, pipItemsWeighted.Count);
        return pipItemsWeighted[randomIndex];
    }

    public void SpawnPipBagDice()
    {
        RectTransform panelRect = PipPannel.GetComponent<RectTransform>();

        float width = panelRect.sizeDelta.x - activeDiceSpacing;
        float startX = -width / 2;

        for (int i = 0; i < 6; i++)
        {
            GameObject pipDice = Instantiate(PipDicePrefab, PipPannel.transform);
            pipDiceItems.Add(pipDice);
            pipDiceItems[i].transform.SetParent(PipPannel.transform, false);
            float xPos = startX + (i * (width / (6 - 1)));
            pipDiceItems[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, 0);
            pipDice.SetActive(false);

            int pipIndex = i;
            Button pipButton = pipDice.GetComponent<Button>();
            if (pipButton == null) pipButton = pipDice.AddComponent<Button>();
            //pipButton.onClick.AddListener(() => PipDiceClicked(pipIndex));

            PipDropTarget dropTarget = pipDice.GetComponent<PipDropTarget>();
            if (dropTarget == null) dropTarget = pipDice.AddComponent<PipDropTarget>();
            dropTarget.faceIndex = i;
        }
        DisplayPipDice();
    }

    public void DisplayPipDice()
    {
        foreach (GameObject pipDice in pipDiceItems)
        {
            pipDice.SetActive(true);
        }
    }

    /// ---------- dice bag description helper ----------
    
    [Header("Helper Settings")]
    public GameObject nameObject;
    public GameObject descObject;
    public GameObject rarityObject;
    public GameObject costObject;
    public GameObject itemSelected;
    public bool descEnabled = false;
    public int diceDescId = -1;

    void positonDice()
    {
        List<DiceData> temp = playerData.dice;
        RectTransform panelRect = DicePannel.GetComponent<RectTransform>();

        float width = panelRect.sizeDelta.x - activeDiceSpacing;
        float startX = -width / 2;

        for (int i = 0; i < temp.Count; i++)
        {
            int diceIndex = i;
            temp[i].transform.SetParent(DicePannel.transform, false);
            float xPos = startX + (i * (width / (temp.Count - 1)));
            temp[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, 0);



            EventTrigger trigger = playerData.dice[i].GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = playerData.dice[i].transform.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { ShowDiceDescription(diceIndex); });
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { HideDiceDescription(); });
            trigger.triggers.Add(entryExit);

            SetupDraggableDice(temp[i].gameObject);
        }
        highlightPanel.transform.position = temp[0].transform.position;
    }

    void SetupDraggableDice(GameObject diceObj)
    {
        DiceDrag dragScript = diceObj.GetComponent<DiceDrag>();
        if (dragScript == null)
        {
            dragScript = diceObj.AddComponent<DiceDrag>();
        }

        dragScript.OnDragStart += HandleDiceDragStart;
        dragScript.OnDragEnd += HandleDiceDragEnd;
        dragScript.OnDroppedOn += HandleDiceDropped;
    }

    void HandleDiceDragStart(DiceDrag draggedDice)
    {
        DiceData diceData = draggedDice.GetDiceData();
        if (diceData != null && diceData.diceConfig != null && sellZone != null)
        {
            sellZone.SetActive(true);
        }
    }

    void HandleDiceDragEnd(DiceDrag draggedDice)
    {
        if (sellZone != null)
        {
            sellZone.SetActive(false);
        }
    }

    void HandleDiceDropped(DiceDrag draggedDice, GameObject dropZone)
    {
        if (dropZone.CompareTag("SellZone"))
        {
            DiceData diceData = draggedDice.GetDiceData();
            if (diceData != null)
            {
                HandleDiceSell(diceData, dropZone);
            }
        }
    }

    public GameObject highlightPanel;
    public DiceData currentHighlightedDice;

    public void ShowDiceDescription(int diceIndex)
    {
        if (diceIndex < 0 || diceIndex >= playerData.dice.Count) return;

        DiceData die = playerData.dice[diceIndex];
        currentHighlightedDice = die;

        UpdatePipDropTargets(die);

        if (buysellButton.transform.GetChild(0).GetComponent<TMP_Text>().text == "Cancel")
        {
            if (itemSelected != null && itemSelected.GetComponent<ShopItem>() != null && itemSelected.GetComponent<ShopItem>().item == ShopItemType.Pip)
            {
                if (die != null)
                {
                    for (int i = 0; i < pipDiceItems.Count && i < die.pips.Count; i++)
                    {
                        pipDiceItems[i].SetActive(true);
                        Image pipImage = pipDiceItems[i].transform.GetChild(0).GetComponent<Image>();
                        int pipValue = die.pips[i];
                        pipImage.sprite = die.pipSprites[pipValue - 1].GetComponent<Image>().sprite;
                    }

                }
                if (nameObject != null)
                    nameObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? die.diceConfig.diceName : "Unknown Dice";
                if (descObject != null)
                    descObject.GetComponent<TMP_Text>().text = "Click a pip to replace";
            }
            return;
        }

        descEnabled = false;
        itemSelected = null;
        buysellButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "";

        if (die.diceConfig != null)
        {
            for (int i = 0; i < pipDiceItems.Count && i < die.pips.Count; i++)
            {
                pipDiceItems[i].SetActive(true);
                Image pipImage = pipDiceItems[i].transform.GetChild(0).GetComponent<Image>();
                int pipValue = die.pips[i];
                pipImage.sprite = die.diceConfig.pipSprites[pipValue - 1].GetComponent<Image>().sprite;
            }
        }
        highlightPanel.transform.position = die.transform.position;
        if (nameObject != null)
            nameObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? die.diceConfig.diceName : "Unknown Dice";

        if (descObject != null)
            descObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? die.diceConfig.description : "No description";

        List<ShopItemData> diceItems = GetItemsByType(ShopItemType.DiceType);
        diceItems.RemoveAll(item => item.diceConfig != null && die.diceConfig != null && item.diceConfig.diceName != die.diceConfig.diceName);
        ShopItemData diceShopData = diceItems[0];
        if (rarityObject != null && diceShopData != null)
            rarityObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? diceShopData.weight.ToString() : "Rarity: ?";

        if (costObject != null && diceShopData != null)
            costObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? diceShopData.cost.ToString() : "Cost: ?";


        if (die.diceConfig.diceName == normalDiceConfig.diceName)
        {
            rarityObject.GetComponent<TMP_Text>().text = "Common";
            costObject.GetComponent<TMP_Text>().text = "0";
        }

    }

    public void HideDiceDescription()
    {
        if (descEnabled) return;

        if (nameObject != null)
            nameObject.GetComponent<TMP_Text>().text = "";

        if (descObject != null)
            descObject.GetComponent<TMP_Text>().text = "";

        if (rarityObject != null)
            rarityObject.GetComponent<TMP_Text>().text = "";

        if (costObject != null)
            costObject.GetComponent<TMP_Text>().text = "";
    }

    private void OnDestroy()
    {
        for (int i = 0; i < DicePannel.transform.childCount; i++)
        {
            Transform child = DicePannel.transform.GetChild(0);

            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();

            EventTrigger trigger = child.GetComponent<EventTrigger>();
            if (trigger != null) trigger.triggers.Clear();

            DiceDrag dragScript = child.GetComponent<DiceDrag>();
            if (dragScript != null)
            {
                dragScript.OnDragStart -= HandleDiceDragStart;
                dragScript.OnDragEnd -= HandleDiceDragEnd;
                dragScript.OnDroppedOn -= HandleDiceDropped;
            }

            child.SetParent(playerData.transform.GetChild(0));
        }
    }

    // ---------- Sell Dice ----------

    public void HandleDiceSell(DiceData diceData, GameObject dropZone)
    {
        if (!dropZone.CompareTag("SellZone")) return;

        if (diceData.diceConfig == null || diceData.diceConfig.diceName == "Normal Dice")
        {
            Debug.Log("Cannot sell a normal dice!");
            return;
        }

        int sellPrice = GetSellPrice(diceData);

        playerData.AddMoney(sellPrice);
        ShowDiceDescription(diceData.ID);
        Debug.Log($"Sold dice for {sellPrice} coins!");

        ConvertToNormalDice(diceData);
    }

    public int GetSellPrice(DiceData diceData)
    {
        if (diceData.diceConfig == null) return 0;

        List<ShopItemData> diceItems = GetItemsByType(ShopItemType.DiceType);
        ShopItemData shopData = diceItems.Find(item => item.diceConfig == diceData.diceConfig);

        if (shopData == null || diceData.diceConfig.name == normalDiceConfig.diceName) return 0;

        return shopData.cost / 2;
    }

    public void ConvertToNormalDice(DiceData diceData)
    {
        if (normalDiceConfig == null)
        {
            Debug.LogError("Normal Dice Config not assigned in ShopController!");
            return;
        }

        diceData.diceConfig = normalDiceConfig;

        Image diceImage = diceData.GetComponent<Image>();
        if (diceImage != null && normalDiceConfig.diceSprite != null)
        {
            diceImage.sprite = normalDiceConfig.diceSprite;
        }

        if (normalDiceConfig.pipSprites != null && normalDiceConfig.pipSprites.Count >= 6)
        {
            diceData.pipSprites = new List<GameObject>(normalDiceConfig.pipSprites);
        }

        if (diceData.currentPip != null)
        {
            DestroyImmediate(diceData.currentPip);
        }

        //diceData.ChangePipNow(diceData.currentFace);

        Debug.Log("Dice converted to normal dice");
    }

    // ---------- Buy Dice ----------

    void SetupDraggableShopItem(ShopItem shopItem)
    {
        shopItem.OnDragStart += HandleShopItemDragStart;
        shopItem.OnDragEnd += HandleShopItemDragEnd;
        shopItem.OnDroppedOn += HandleShopItemDropped;
    }

    void HandleShopItemDragStart(ShopItem shopItem)
    {
        if (shopItem.itemData != null && shopItem.itemData.itemType == ShopItemType.DiceType)
        {
            HighlightNormalDice(true);
        }
        else if (shopItem.itemData != null && shopItem.itemData.itemType == ShopItemType.Pip)
        {
            HighlightPipFaces(true, shopItem);
        }
    }

    void HandleShopItemDragEnd(ShopItem shopItem)
    {
        HighlightNormalDice(false);
        HighlightPipFaces(false, shopItem);
    }

    void HandleShopItemDropped(ShopItem shopItem, GameObject dropTarget)
    {
        DiceData targetDice = dropTarget.GetComponent<DiceData>();
        if (targetDice != null)
        {
            if (shopItem.itemData.itemType == ShopItemType.DiceType)
            {
                HandleDiceBuy(shopItem, targetDice);
            }
            return;
        }

        PipDropTarget pipTarget = dropTarget.GetComponent<PipDropTarget>();
        if (pipTarget != null && shopItem.itemData.itemType == ShopItemType.Pip)
        {
            HandlePipBuy(shopItem, pipTarget);
        }
    }

    void UpdatePipDropTargets(DiceData dice)
    {
        for (int i = 0; i < pipDiceItems.Count; i++)
        {
            PipDropTarget dropTarget = pipDiceItems[i].GetComponent<PipDropTarget>();
            if (dropTarget != null)
            {
                dropTarget.associatedDice = dice;
                dropTarget.faceIndex = i;
            }
        }
    }

    void HandlePipBuy(ShopItem shopItem, PipDropTarget pipTarget)
    {
        ShopItemData itemData = shopItem.itemData;
        DiceData targetDice = pipTarget.associatedDice;

        if (targetDice == null)
        {
            Debug.Log("No dice selected! Hover over a dice first.");
            return;
        }

        if (!playerData.CanAfford(itemData.cost))
        {
            Debug.Log($"Cannot afford! Need {itemData.cost}, have {playerData.money}");
            return;
        }

        int newPipValue = int.Parse(itemData.pipSprite.name.Substring(0, 1));
        int faceIndex = pipTarget.faceIndex;

        // Prevent buying the same pip value
        if (targetDice.pips[faceIndex] == newPipValue)
        {
            Debug.Log($"They have the same face value. So can't be replaced.");
            return;
        }

        playerData.TrySpendMoney(itemData.cost);
        Debug.Log($"Bought pip {newPipValue} for face {faceIndex} for {itemData.cost} coins!");

        targetDice.pips[faceIndex] = newPipValue;

        UpdatePipDisplay(targetDice, faceIndex, newPipValue);

        RefreshPipShopSlot(shopItem);
    }

    void UpdatePipDisplay(DiceData dice, int faceIndex, int newPipValue)
    {
        if (faceIndex < pipDiceItems.Count)
        {
            Image pipImage = pipDiceItems[faceIndex].transform.GetChild(0).GetComponent<Image>();
            if (dice.pipSprites != null && newPipValue > 0 && newPipValue <= dice.pipSprites.Count)
            {
                pipImage.sprite = dice.pipSprites[newPipValue - 1].GetComponent<Image>().sprite;
            }
            else if (dice.diceConfig != null && dice.diceConfig.pipSprites != null &&
                     newPipValue > 0 && newPipValue <= dice.diceConfig.pipSprites.Count)
            {
                pipImage.sprite = dice.diceConfig.pipSprites[newPipValue - 1].GetComponent<Image>().sprite;
            }
        }
    }

    void RefreshPipShopSlot(ShopItem shopItem)
    {
        ShopItemData newData = GeneratePipTypeShopItem();
        GameObject pipObject = shopItem.gameObject;

        pipObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = newData.pipSprite;
        shopItem.itemData = newData;
    }

    void HighlightNormalDice(bool highlight)
    {
        foreach (DiceData dice in playerData.dice)
        {
            Image diceImage = dice.GetComponent<Image>();
            if (diceImage != null)
            {
                if (highlight)
                {
                    if (dice.diceConfig == null || dice.diceConfig == normalDiceConfig)
                    {
                        diceImage.color = new Color(0.5f, 1f, 0.5f, 1f);
                    }
                }
                else
                {
                    diceImage.color = Color.white;
                }
            }
        }
    }

    void HighlightPipFaces(bool highlight, ShopItem shopItem)
    {
        foreach (GameObject pipDice in pipDiceItems)
        {
            if (!pipDice.activeSelf) continue;

            Image pipImage = pipDice.GetComponent<Image>();

            if (highlight)
            {
                if (shopItem.itemData == null) continue;
                DiceData associatedDice = pipDice.GetComponent<PipDropTarget>().associatedDice;
                int faceIndex = pipDice.GetComponent<PipDropTarget>().faceIndex;
                int newPipValue = int.Parse(shopItem.itemData.pipSprite.name.Substring(0, 1));
                if (associatedDice != null && associatedDice.pips[faceIndex] == newPipValue)
                {
                    continue;
                }
            }

            if (pipImage != null)
            {
                if (highlight)
                {
                    pipImage.color = new Color(0.5f, 1f, 0.5f, 1f);
                }
                else
                {
                    pipImage.color = Color.white;
                }
            }
        }
    }

    public void HandleDiceBuy(ShopItem shopItem, DiceData targetDice)
    {
        ShopItemData itemData = shopItem.itemData;

        if (targetDice.diceConfig != null && targetDice.diceConfig != normalDiceConfig)
        {
            Debug.Log("Can only place new dice on normal dice slots!");
            return;
        }

        if (!playerData.CanAfford(itemData.cost))
        {
            Debug.Log($"Cannot afford! Need {itemData.cost}, have {playerData.money}");
            return;
        }

        playerData.TrySpendMoney(itemData.cost);
        Debug.Log($"Bought {itemData.diceConfig.diceName} for {itemData.cost} coins!");

        ApplyDiceConfig(targetDice, itemData.diceConfig);

        RefreshShopSlot(shopItem);
    }

    void ApplyDiceConfig(DiceData diceData, DiceConfig newConfig)
    {
        diceData.diceConfig = newConfig;

        Image diceImage = diceData.GetComponent<Image>();
        if (diceImage != null && newConfig.diceSprite != null)
        {
            diceImage.sprite = newConfig.diceSprite;
        }

        if (newConfig.pipSprites != null && newConfig.pipSprites.Count >= 6)
        {
            diceData.pipSprites = new List<GameObject>(newConfig.pipSprites);
        }

        if (newConfig.customPips != null && newConfig.customPips.Count > 0)
        {
            diceData.pips = new List<int>(newConfig.customPips);
        }
        else
        {
            diceData.pips = new List<int> { 1, 2, 3, 4, 5, 6 };
        }

        if (diceData.currentPip != null)
        {
            DestroyImmediate(diceData.currentPip);
        }
        //diceData.ChangePipNow(diceData.currentFace);

        Debug.Log($"Dice upgraded to {newConfig.diceName}");
    }

    void RefreshShopSlot(ShopItem shopItem)
    {
        shopItem.OnDragStart -= HandleShopItemDragStart;
        shopItem.OnDragEnd -= HandleShopItemDragEnd;
        shopItem.OnDroppedOn -= HandleShopItemDropped;

        ShopItemData newData = GenerateDiceTypeShopItem();
        shopItem.Init(newData);

        shopItem.OnDragStart += HandleShopItemDragStart;
        shopItem.OnDragEnd += HandleShopItemDragEnd;
        shopItem.OnDroppedOn += HandleShopItemDropped;
    }
}