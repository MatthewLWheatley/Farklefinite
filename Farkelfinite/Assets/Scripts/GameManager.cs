using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public List<GameObject> diceObjects = new List<GameObject>();
    public List<DiceData> diceDataList = new List<DiceData>();

    public TMP_Text ScoreText;

    public List<bool> selectedDice = new List<bool>();

    public LayerMask diceMask;

    public int lives = 3;
    public GameObject Game;
    public GameObject Dead;

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
            diceDataList[count].ID = count + 1;
            count++;
        }
        diceMask = new LayerMask();
        diceMask = LayerMask.GetMask("Dice");
    }

    private void Update()
    {

    }

    public void OnLeftClick(InputAction.CallbackContext context)
    {
        Vector3 mousepos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousepos);
        worldPos.z = 0;

        Debug.DrawLine(worldPos + Vector3.up * 0.5f, worldPos + Vector3.down * 0.5f, Color.red, 2f);
        Debug.DrawLine(worldPos + Vector3.left * 0.5f, worldPos + Vector3.right * 0.5f, Color.red, 2f);

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, diceMask);

        Debug.DrawRay(worldPos, Vector2.zero, Color.yellow, 2f);

        if (hit.collider != null)
        {
            Debug.Log($"HIT: {hit.collider.name}");
            DiceData hitData = hit.collider.GetComponent<DiceData>();


            selectedDice[hitData.ID - 1] = !selectedDice[hitData.ID - 1];
            if (selectedDice[hitData.ID - 1])
            {
                hit.collider.gameObject.transform.position = hit.collider.gameObject.transform.position + new Vector3(0, 1f, 0);
            }
            else
            {
                hit.collider.gameObject.transform.position = hit.collider.gameObject.transform.position + new Vector3(0, -1f, 0);
            }
            CalculateScore(selectedDice);
            //if (hitData)
            //{
            //    hitData.ChangePip(Random.Range(0, 6));
            //}
        }
        else
        {
            Debug.Log("hit literally nothing, check your life choices");
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

    public void CalculateScore(List<bool> ToCheck)
    {
        Dictionary<int, int> pipCounts = new Dictionary<int, int>();
        List<int> selectedPips = new List<int>();

        for (int i = 0; i < 6; i++)
        {
            if (ToCheck[i])
            {
                int value = diceDataList[i].pips[diceDataList[i].currentFace];
                selectedPips.Add(value);
                if (pipCounts.ContainsKey(value))
                    pipCounts[value]++;
                else
                    pipCounts[value] = 1;
            }
        }

        if (selectedPips.Count == 0)
        {
            ScoreText.text = "Score: 0";
            return;
        }

        int bestScore = CalculateBestScore(pipCounts, selectedPips);
        ScoreText.text = "Score: " + bestScore;
        Debug.Log("Best Score: " + bestScore);
    }

    private int CalculateBestScore(Dictionary<int, int> pipCounts, List<int> allPips)
    {
        int maxScore = 0;

        if (allPips.Count == 6 && pipCounts.Count == 6 &&
            pipCounts.All(kvp => kvp.Key >= 1 && kvp.Key <= 6 && kvp.Value == 1))
        {
            maxScore = Mathf.Max(maxScore, 5000);
        }

        if (allPips.Count >= 5)
        {
            bool has1to5 = Enumerable.Range(1, 5).All(i => pipCounts.ContainsKey(i) && pipCounts[i] >= 1);
            bool has2to6 = Enumerable.Range(2, 5).All(i => pipCounts.ContainsKey(i) && pipCounts[i] >= 1);

            if (has1to5 || has2to6)
            {
                maxScore = Mathf.Max(maxScore, 2500);
            }
        }

        if (allPips.Count == 6)
        {
            int pairCount = pipCounts.Count(kvp => kvp.Value == 2);
            if (pairCount == 3)
            {
                maxScore = Mathf.Max(maxScore, 1500);
            }
        }

        int comboScore = CalculateComboScore(new Dictionary<int, int>(pipCounts));
        maxScore = Mathf.Max(maxScore, comboScore);

        return maxScore;
    }

    private int CalculateComboScore(Dictionary<int, int> pipCounts)
    {
        int score = 0;

        foreach (var kvp in pipCounts.ToList())
        {
            int pip = kvp.Key;
            int count = kvp.Value;

            if (count >= 3)
            {
                if (pip == 1)
                {
                    score += 1000 * (count - 2);
                    pipCounts[pip] = 0;
                }
                else
                {
                    score += 100 * pip * (int)Mathf.Pow(2, count - 3);
                    pipCounts[pip] = 0;
                }
            }
        }

        if (pipCounts.ContainsKey(1) && pipCounts[1] > 0)
        {
            score += pipCounts[1] * 100;
        }
        if (pipCounts.ContainsKey(5) && pipCounts[5] > 0)
        {
            score += pipCounts[5] * 50;
        }

        return score;
    }

    public void setAside() 
    { 
        
    }

    public void CollectPoints() 
    { 
    
    }

    public void bust() 
    {
        lives -= 1;
        if (lives == 0) 
        {
            Game.SetActive(false);
            Dead.SetActive(true);
        }
    }

}