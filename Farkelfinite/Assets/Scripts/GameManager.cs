using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<GameObject> diceObjects = new List<GameObject>();
    public List<DiceData> diceDataList = new List<DiceData>();


    public TMP_Text ScoreText;

    public List<bool> selectedDice = new List<bool>();

    public LayerMask diceMask;

    void Start()
    {
        int count = 0;
        foreach (var die in diceObjects)
        {
            diceDataList.Add(die.GetComponent<DiceData>());
            die.transform.position = new Vector3(count * 2.0f - 5, 0, 0);
            int pip = Random.Range(0, 6);
            diceDataList[count].ChangePipNow(pip);
            selectedDice.Add(false);
            diceDataList[count].ID = count+1;
            count++;
        }
        diceMask = new LayerMask();
        diceMask = LayerMask.GetMask("Dice");
    }

    private void Update()
    {
    }

    public void OnLeftMouseDown() 
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 10, diceMask);

        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            DiceData hitData = hit.collider.gameObject.GetComponent<DiceData>();
            if (hitData)
            {
                hitData.ChangePip(Random.Range(0, 6));
            }
        }
    }

    public void ToggleSelectedDice(int DiceID) 
    {
        selectedDice[DiceID] = !selectedDice[DiceID];
    }

    [ContextMenu("Debug Test")]
    void debugtest()
    {
        foreach (var die in diceDataList)
        {
            int pip = Random.Range(0, 6);
            die.ChangePip(pip);
        }
    }


    public void CaculateScore(List<bool> ToCheck)
    {
        /*
        try to make a scoring system like Farkle or 10,000
        1 = 100
        5 = 50
        3-6 (n) of a 1 = 1000 * (n-2)
        3-6 (n) of a  2-6 (pip) = 100 * pip * 2^n-3
        straight 5 = 2500
        straight 6 = 5000
        3 pair = 1500
        */

        int score = 0;
        Dictionary<int, int> pipCounts = new Dictionary<int, int>();

        for(int i = 0; i < 6; i++) 
        {
            if (ToCheck[i])
            {
                int value = diceDataList[i].pips[diceDataList[i].currentFace];
                if (pipCounts.ContainsKey(value + 1))
                    pipCounts[value + 1]++;
                else
                    pipCounts[value + 1] = 1;
            }
        }

        //straight check
        bool isStraight = true;
        //check for 1-6 straight
        for (int i = 1; i <= 6; i++)
        {
            if (!pipCounts.ContainsKey(i) || pipCounts[i] != 1)
            {
                isStraight = false;
                break;
            }
        }
        if (isStraight)
        {
            score += 5000;
            Debug.Log("Straight 6! Score: " + score);
            return;
        }
        //check for 1-5 straight
        isStraight = !isStraight;
        for (int i = 1; i <= 5; i++)
        {
            if (!pipCounts.ContainsKey(i) || pipCounts[i] != 1)
            {
                isStraight = false;
                break;
            }
        }
        if (isStraight)
        {
            score += 1500;
            Debug.Log("Straight 5! Score: " + score);
        }



        ScoreText.text = "Score: " + score;
    }
}
