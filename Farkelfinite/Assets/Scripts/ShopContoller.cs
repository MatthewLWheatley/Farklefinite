using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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



    public void Start()
    {
        playerData = PlayerData.Instance;

        List<DiceData> temp = FindObjectsByType<DiceData>(FindObjectsSortMode.InstanceID).ToList();


        DiceData a = new DiceData();
        DiceData b = new DiceData();
        DiceData c = new DiceData();
        DiceData d = new DiceData();
        DiceData e = new DiceData();
        DiceData f = new DiceData();

        foreach (var tem in temp) 
        {
            switch (tem.gameObject.name) 
            {
                case "Dice":
                    a = tem;
                    break;
                case "Dice (1)":
                    b = tem;
                    break;
                case "Dice (2)":
                    c = tem;
                    break;
                case "Dice (3)":
                    d = tem;
                    break;
                case "Dice (4)":
                    e = tem;
                    break;
                case "Dice (5)":
                    f = tem;
                    break;
            }
        }

        Dice.Add(a);
        Dice.Add(b);
        Dice.Add(c);
        Dice.Add(d);
        Dice.Add(e);
        Dice.Add(f);

    }

    public void LoadUnlockedDiceType() 
    { 
        
    }

    public void GenerateShopItems()
    {

    }

    private void GenerateDiceType()
    {

    }

    public List<ShopItemData> GetItemsByType(ShopItemType type)
    {
        return allShopItems.FindAll(item => item.itemType == type);
    }

    public void SpawnShopItem(ShopItemData data, Vector3 position)
    {
        GameObject itemObj = Instantiate(shopItemPrefab, position, Quaternion.identity);
        ShopItem item = itemObj.GetComponent<ShopItem>();
        item.Init(data); // pass the data to the actual item
    }
}
