using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;

public class PlayerData : MonoBehaviour
{
    public List<DiceData> dice;
    public int lives;
    public int money;
    public int lvl = 1;

    private void Awake()
    {
        dice = new List<DiceData>();
    }
}
