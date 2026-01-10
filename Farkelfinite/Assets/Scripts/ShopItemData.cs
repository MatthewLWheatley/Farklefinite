using Unity.VisualScripting;
using UnityEngine;

public enum ShopItemWeight 
{ 
    common,
    uncommon,
    rare,
    epic,
    legendary
}


[CreateAssetMenu(fileName = "ShopItem", menuName = "Shop/Item")]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public ShopItemType itemType;
    public Sprite itemSprite;
    public Sprite pipSprite;
    public int cost;
    public ShopItemWeight weight;
    public DiceConfig diceConfig;

    public void OnPurchase()
    {
        Debug.Log($"Purchased {itemName} for {cost} coins.");
    }
}
