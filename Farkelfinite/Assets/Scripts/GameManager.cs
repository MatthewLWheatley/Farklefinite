using System.Collections;
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
    public TMP_Text SetAsideScoreText;
    public TMP_Text TotalScoreText;

    public List<bool> selectedDice = new List<bool>();
    public List<bool> setAsideDice = new List<bool>();

    public LayerMask diceMask;

    public int lives = 3;
    public GameObject Game;
    public GameObject Dead;

    public int currentScore = 0;
    public int setAsideScore = 0;
    public int totalScore = 0;

    private bool isRolling = false;
    private bool isMoving = false;

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float setAsideYOffset = -1f;

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
            setAsideDice.Add(false);
            diceDataList[count].ID = count + 1;
            count++;
        }
        diceMask = new LayerMask();
        diceMask = LayerMask.GetMask("Dice");

        UpdateScoreUI();
    }

    private void Update()
    {

    }

    public void OnLeftClick(InputAction.CallbackContext context)
    {
        if (isRolling || isMoving) return;

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

            if (setAsideDice[hitData.ID - 1]) return;

            selectedDice[hitData.ID - 1] = !selectedDice[hitData.ID - 1];
            StartCoroutine(MoveDiceToPosition(hitData.ID - 1));
            CalculateScore(selectedDice);
        }
        else
        {
            Debug.Log("hit literally nothing, check your life choices");
        }
    }

    private IEnumerator MoveDiceToPosition(int diceIndex)
    {
        isMoving = true;
        GameObject die = diceObjects[diceIndex];
        Vector3 startPos = die.transform.position;
        Vector3 targetPos = startPos;

        if (selectedDice[diceIndex])
        {
            targetPos.y = 1f;
        }
        else
        {
            targetPos.y = 0f;
        }

        float elapsed = 0f;
        float duration = 1f / moveSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            die.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        die.transform.position = targetPos;
        isMoving = false;
    }

    public void ToggleSelectedDice(int DiceID)
    {
        if (isRolling || isMoving || setAsideDice[DiceID]) return;
        selectedDice[DiceID] = !selectedDice[DiceID];
    }

    [ContextMenu("Debug Test")]
    void debugtest()
    {
        if (isRolling) return;

        for (int i = 0; i < selectedDice.Count; i++)
        {
            selectedDice[i] = false;
        }

        StartCoroutine(DebugRollCoroutine());
    }

    private IEnumerator DebugRollCoroutine()
    {
        isRolling = true;

        foreach (var die in diceObjects)
        {
            die.transform.position = new Vector3((diceObjects.IndexOf(die)) * 2.0f - 5, 0, 0);
        }

        List<Coroutine> rollCoroutines = new List<Coroutine>();
        foreach (var die in diceDataList)
        {
            int pip = Random.Range(0, 6);
            die.ChangePip(pip);
        }

        while (diceDataList.Any(d => d.rolling))
        {
            yield return null;
        }

        isRolling = false;
        CalculateScore(selectedDice);
    }

    public void RollDice()
    {
        if (isRolling) return;
        StartCoroutine(RollSpecificDice());
    }

    private IEnumerator RollSpecificDice()
    {
        isRolling = true;

        for (int i = 0; i < diceDataList.Count; i++)
        {
            if (!setAsideDice[i])
            {
                selectedDice[i] = false;
                int pip = Random.Range(0, 6);
                diceDataList[i].ChangePip(pip);
            }
        }

        while (diceDataList.Any(d => d.rolling))
        {
            yield return null;
        }

        isRolling = false;

        if (!HasValidScore())
        {
            Bust();
        }
    }

    public void SetAside()
    {
        if (currentScore == 0 || isRolling || isMoving) return;

        StartCoroutine(SetAsideDiceCoroutine());
    }

    private IEnumerator SetAsideDiceCoroutine()
    {
        isMoving = true;
        setAsideScore += currentScore;
        currentScore = 0;

        List<Coroutine> moveCoroutines = new List<Coroutine>();

        for (int i = 0; i < selectedDice.Count; i++)
        {
            if (selectedDice[i])
            {
                setAsideDice[i] = true;
                selectedDice[i] = false;
                StartCoroutine(MoveDiceToSetAside(i));
            }
        }

        yield return new WaitForSeconds(1f / moveSpeed);

        isMoving = false;
        UpdateScoreUI();

        if (setAsideDice.All(d => d))
        {
            ResetAllDice();
        }
    }

    private IEnumerator MoveDiceToSetAside(int diceIndex)
    {
        GameObject die = diceObjects[diceIndex];
        Vector3 startPos = die.transform.position;
        Vector3 targetPos = new Vector3(diceIndex * 2.0f - 5, setAsideYOffset, 0);

        SpriteRenderer sr = die.GetComponent<SpriteRenderer>();
        Color startColor = sr.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0.5f);

        float elapsed = 0f;
        float duration = 1f / moveSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            die.transform.position = Vector3.Lerp(startPos, targetPos, t);
            sr.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        die.transform.position = targetPos;
        sr.color = targetColor;
    }

    private void ResetAllDice()
    {
        for (int i = 0; i < setAsideDice.Count; i++)
        {
            setAsideDice[i] = false;
            diceObjects[i].GetComponent<SpriteRenderer>().color = Color.white;
            diceObjects[i].transform.position = new Vector3(i * 2.0f - 5, 0, 0);
        }
    }

    public void BankScore()
    {
        if (setAsideScore == 0) return;

        totalScore += setAsideScore;
        setAsideScore = 0;
        ResetAllDice();
        UpdateScoreUI();
        lives -= 1;

        if (lives <= 0)
        {
            Game.SetActive(false);
            Dead.SetActive(true);
        }
    }

    private bool HasValidScore()
    {
        List<int> activePips = new List<int>();
        Dictionary<int, int> pipCounts = new Dictionary<int, int>();

        for (int i = 0; i < diceDataList.Count; i++)
        {
            if (!setAsideDice[i])
            {
                int value = diceDataList[i].pips[diceDataList[i].currentFace];
                activePips.Add(value);
                if (pipCounts.ContainsKey(value))
                    pipCounts[value]++;
                else
                    pipCounts[value] = 1;
            }
        }

        if (activePips.Count == 0) return true;

        int score = CalculateBestScore(pipCounts, activePips);
        return score > 0;
    }

    public void CalculateScore(List<bool> ToCheck)
    {
        Dictionary<int, int> pipCounts = new Dictionary<int, int>();
        List<int> selectedPips = new List<int>();

        for (int i = 0; i < 6; i++)
        {
            if (ToCheck[i] && !setAsideDice[i])
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
            currentScore = 0;
            UpdateScoreUI();
            return;
        }

        currentScore = CalculateBestScore(pipCounts, selectedPips);
        UpdateScoreUI();
        Debug.Log("Best Score: " + currentScore);
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

    private void UpdateScoreUI()
    {
        ScoreText.text = "Current: " + currentScore;
        if (SetAsideScoreText != null)
            SetAsideScoreText.text = "Set Aside: " + setAsideScore;
        if (TotalScoreText != null)
            TotalScoreText.text = "Total: " + totalScore;
    }

    public void Bust()
    {
        setAsideScore = 0;
        ResetAllDice();
        UpdateScoreUI();
        lives -= 1;

        if (lives <= 0)
        {
            Game.SetActive(false);
            Dead.SetActive(true);
        }
    }
}