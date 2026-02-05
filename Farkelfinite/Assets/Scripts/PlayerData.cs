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
    public int money = 0;
    public int lvl = 1;
    public Bag currentBag;

    public int currentLevel = 1;
    public int roundsPerLevel = 3;
    public int currentRound = 1;
    public int bestScore = 0;
    public int HighScore = 0;
    public int HighLevel = 0;
    public int HighStage = 0;

    public bool EliteLevel = false;
    public bool BossLevel = false;

    public static PlayerData Instance { get { return _instance; } }

    public void SetMoney(int amount)
    {
        money = Mathf.Max(0, amount);
        Debug.Log($"Money set to: {money}");
    }

    public void AddMoney(int amount)
    {
        money += amount;
        money = Mathf.Max(0, money);
        Debug.Log($"Money: {money}");
    }

    public bool CanAfford(int cost)
    {
        return money >= cost;
    }

    public bool TrySpendMoney(int cost)
    {
        if (CanAfford(cost))
        {
            AddMoney(-cost);
            return true;
        }
        Debug.Log($"Can't afford! Need {cost}, have {money}");
        return false;
    }

    public int getNextLevelScoreThreshold(int level, int stage)
    {
        int sofarlevels = 0;
        if (stage > 1) sofarlevels = 5 + (stage - 1)*2;

        int mod = 1;
        if (BossLevel)
        {
            mod += 2;
        }
        if (EliteLevel)
        {
            mod += 1;
        }
        // 1 * 100 + (1) * 100 * mod = 100 + 100 * mod
        // 5 * 100 + (5) * 100 * mod = 500 + 500 * mod
        // 5 * 100 + (5) * 100 * 3 = 500 + 1500 = 2000

        return (level + sofarlevels) * 100 + (sofarlevels + level) * (100 * mod);
    }

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
        transform.GetChild(0).GetComponent<Canvas>().worldCamera = Camera.main;
        //dice = new List<DiceData>();
        int bagint = PlayerPrefs.GetInt("CurrentBag", (int)Bag.DiceBag);
        currentBag = (Bag)bagint;
        roundsPerLevel = 3 + (currentLevel * 2) - 2;
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
        HighLevel = PlayerPrefs.GetInt("BestLevel", 0);
        HighStage = PlayerPrefs.GetInt("BestStage", 0);
    }
}
