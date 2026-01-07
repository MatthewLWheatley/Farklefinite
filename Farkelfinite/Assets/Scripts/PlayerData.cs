using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Unity.VisualScripting;
using UnityEngine.UI;



public class PlayerData : MonoBehaviour
{
    public List<DiceConfig> diceConfigs;

    public static PlayerData _instance;
    public List<DiceData> dice;
    public int lives;
    public int money;
    public int lvl = 1;
    public Bag currentBag;

    public int currentLevel = 1;
    public int roundsPerLevel = 3;
    public int currentRound = 1;

    public static PlayerData Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
        dice = new List<DiceData>();
        int bagint = PlayerPrefs.GetInt("CurrentBag", (int)Bag.DiceBag);
        currentBag = (Bag)bagint;
        roundsPerLevel = 3 + (currentLevel * 2) - 2;
    }

    public RawImage normalRoundImage;
    public RawImage bossRoundImage;

    public void CreateMap() 
    { 
    }
}
