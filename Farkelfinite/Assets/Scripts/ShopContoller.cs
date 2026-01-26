using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
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

    public void Start()
    {
        canvas = GetComponentInParent<Canvas>();

        playerData = PlayerData.Instance;
        positonDice();
        GenerateShopItems();
        SpawnPipBagDice();
        SpawnPipShopItems();
    }

    public void FixedUpdate()
    {
        moneyTextObject.GetComponent<TMP_Text>().text = $"Money: {playerData.money}";
        levelTextObject.GetComponent<TMP_Text>().text = $"Level: {playerData.currentLevel}";
        stageTextObject.GetComponent<TMP_Text>().text = $"Stage: {playerData.currentRound}";
        livesTextObject.GetComponent<TMP_Text>().text = $"Lives: {playerData.lives}";
        totalScoreTextObject.GetComponent<TMP_Text>().text = $"Next Level: {playerData.getNextLevelScoreThreshold(playerData.currentLevel+1)}";
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

        //collect all dice types in player data and in shop item list
        //remove them from the list
        //then pick a random one from the remaining list

        playerData.dice.ForEach(diceData =>
        {
            diceItems.RemoveAll(item => item.diceConfig == diceData.diceConfig);
        });

        

        if (DiceTypePosition1.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == DiceTypePosition1.GetComponent<ShopItem>().itemData.diceConfig);
        if(DiceTypePosition2.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == DiceTypePosition2.GetComponent<ShopItem>().itemData.diceConfig);
        if(DiceTypePosition3.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == DiceTypePosition3.GetComponent<ShopItem>().itemData.diceConfig);
        if(PipTypePosition1.GetComponent<ShopItem>().itemData != null)
            diceItems.RemoveAll(item => item.diceConfig == PipTypePosition1.GetComponent<ShopItem>().itemData.diceConfig);
        if(PipTypePosition2.GetComponent<ShopItem>().itemData != null)
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
        entryEnter.callback.AddListener((data) => { ShowDiceDescription(shopData); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { HideDiceDescription(); });
        trigger.triggers.Add(entryExit);
    }

    public void ShowDiceDescription(ShopItemData data) 
    {
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
            descObject.GetComponent<TMP_Text>().text = "Swap a face on a dice to pip: " + data.pipSprite.name.ToString().Substring(0,1);
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
        pipObject.transform.GetComponent<ShopItem>().itemData = shopData;

        EventTrigger trigger = pipObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = pipObject.transform.AddComponent<EventTrigger>();
        }

        //pipObject.GetComponent<Button>().onClick.AddListener(() => ShopPipDiceClicker(pipObject));

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { ShowDiceDescription(shopData); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { HideDiceDescription(); });
        trigger.triggers.Add(entryExit);
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
        }
    }

    public void DisplayPipDice()
    {
        foreach (GameObject pipDice in pipDiceItems)
        {
            pipDice.SetActive(false);
        }
        if (itemSelected.GetComponent<ShopItem>() == null) return;
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


            //playerData.dice[i].GetComponent<Button>().onClick.AddListener(() => DiceClicked(diceIndex));

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
        }
    }

    public void ShowDiceDescription(int diceIndex)
    {
        if (diceIndex < 0 || diceIndex >= playerData.dice.Count) return;

        DiceData die = playerData.dice[diceIndex];

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

        foreach (GameObject pipDice in pipDiceItems)
        {
            pipDice.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < DicePannel.transform.childCount; i++) 
        { 
            DicePannel.transform.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
            DicePannel.transform.GetChild(0).GetComponent<EventTrigger>().triggers.Clear();
            DicePannel.transform.GetChild(0).SetParent(playerData.transform.GetChild(0));
        }
    }

    // ---------- Purchaseing/Selling Dice ----------
    
    public Sprite defualtDiceSprite;
}