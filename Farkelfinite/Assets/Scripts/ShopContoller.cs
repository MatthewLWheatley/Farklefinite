using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public List<DiceData> Dice = new List<DiceData>();

    public List<ShopItemData> allShopItems;

    [Header("Dice Panel Swapping")]
    public GameObject DicePannel;
    [SerializeField] private float activeDiceSpacing = 200f;
    private Canvas canvas;

    public void Start()
    {
        canvas = GetComponentInParent<Canvas>();

        playerData = PlayerData.Instance;
        positonDice();
        GenerateShopItems();
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

        itemObj.GetComponent<Button>().onClick.AddListener(() => ShopDiceClicked(itemObj));

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
    }

    public void ShopDiceClicked(GameObject itemObj) 
    {
        descEnabled = true;
        Debug.Log($"Dice {itemObj.name} clicked.");
    }

    /// ---------- dice bag description helper ----------
    [Header("Helper Settings")]
    public GameObject nameObject;
    public GameObject descObject;
    public GameObject rarityObject;
    public GameObject costObject;
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


            playerData.dice[i].GetComponent<Button>().onClick.AddListener(() => DiceClicked(diceIndex));

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
        descEnabled = false;
        DiceData die = playerData.dice[diceIndex];

        if (nameObject != null)
            nameObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? die.diceConfig.diceName : "Unknown Dice";

        if (descObject != null)
            descObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? die.diceConfig.description : "No description";


        List<ShopItemData> diceItems = GetItemsByType(ShopItemType.DiceType);
        diceItems.RemoveAll(item => item.diceConfig != null && die.diceConfig != null && item.diceConfig.diceName != die.diceConfig.diceName);
        ShopItemData diceShopData = diceItems[0];
        if (rarityObject != null && diceShopData != null)
            rarityObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? diceShopData.weight.ToString(): "Rarity: ?";

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

        
    }

    public void DiceClicked(int diceIndex)
    {
        descEnabled = true;
        Debug.Log($"Dice {diceIndex} clicked.");
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

}