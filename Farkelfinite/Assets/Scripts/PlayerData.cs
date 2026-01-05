using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;

public class PlayerData : MonoBehaviour
{
    public static PlayerData _instance;
    public List<DiceData> dice;
    public int lives;
    public int money;
    public int lvl = 1;

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
    }
}
