using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public List<GameObject> diceObjects = new List<GameObject>();
    public List<DiceData> diceDataList = new List<DiceData>();

   [Header("Dice Randomization")]
    [SerializeField] private List<DiceConfig> diceConfigPool = new List<DiceConfig>();
    [SerializeField] private bool randomizeOnStart = true;

    public TMP_Text RunningScoreText;
    public TMP_Text TotalScoreText;
    public TMP_Text LivesText;

    public Transform setAsidePositionAnchor;

    public List<bool> selectedDice = new List<bool>();
    public List<bool> setAsideDice = new List<bool>();

    public LayerMask diceMask;

    public int lives = 5;
    public GameObject Game;
    public GameObject Dead;

    public int selectedScore = 0;
    public int setAsideScore = 0;
    public int totalScore = 0;

    private bool isRolling = false;
    private List<bool> diceMoving = new List<bool>();

    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private Vector3 setAsideStartPosition = new Vector3(8f, 3f, 0f);
    [SerializeField] private float setAsideGroupSpacing = 1.5f;
    [SerializeField] private float setAsideDiceSpacing = 0.8f;
    [Header("Dice Spawn Settings")]
    [SerializeField] private Vector3 activeDiceCenter = new Vector3(0, 0, 0);
    [SerializeField] private float activeDiceSpacing = 2.0f;


    public List<List<int>> setAsideGroups = new List<List<int>>();
    public List<int> setAsideGroupScores = new List<int>();

    private AbilityProcessor abilityProcessor;

    public TMP_Text MoneyText;

    [Header("Helper Settings")]
    public GameObject nameObject;
    public GameObject descObject;
    public bool descEnabled = false;
    public int diceDescId = -1;

    void Start()
    {
        diceDataList = PlayerData.Instance.dice;
        diceObjects = new List<GameObject>();
        float startX = activeDiceCenter.x - (6 - 1) * activeDiceSpacing / 2f;
        List<Canvas> can = FindObjectsByType<Canvas>(FindObjectsSortMode.None).ToList();
        foreach (var c in can)
        {
            c.worldCamera = Camera.main;
        }
        foreach (var die in diceDataList)
        {
            diceObjects.Add(die.gameObject);
        }


        for (int i = 0; i < diceObjects.Count; i++)
        {
            int diceIndex = i;
            diceObjects[i].transform.position = new Vector3(startX + i * activeDiceSpacing, 0, 0);
            selectedDice.Add(false);
            setAsideDice.Add(false);
            diceMoving.Add(false);
            diceDataList[i].ID = i + 1;

            diceObjects[i].GetComponent<Button>().onClick.AddListener(() => DiceClicked(diceIndex));

            EventTrigger trigger = diceObjects[i].GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = diceObjects[i].AddComponent<EventTrigger>();
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

        diceMask = new LayerMask();
        diceMask = LayerMask.GetMask("Dice");

        if (setAsidePositionAnchor != null)
        {
            setAsideStartPosition = setAsidePositionAnchor.position;
        }

        if (randomizeOnStart)
        {
            RandomizeAllDiceConfigs();
        }

        bool validStart = false;
        while (!validStart)
        {
            RollDice(true);

            validStart = HasValidScore();
        }

        InitializeAbilitySystem();
        UpdateScoreUI();

    }

    private void Awake()
    {
        
    }

    private void RandomizeAllDiceConfigs()
    {
        if (diceConfigPool.Count == 0)
        {
            Debug.LogWarning("Dice config pool is empty. Did you forget to add configs to the list? Typical.");
            return;
        }

        foreach (var diceData in diceDataList)
        {
            diceData.diceConfig = diceConfigPool[Random.Range(0, diceConfigPool.Count)];

            Image sr = diceData.GetComponent<Image>();
            if (sr != null && diceData.diceConfig != null && diceData.diceConfig.diceSprite != null)
            {
                sr.sprite = diceData.diceConfig.diceSprite;
            }
            else
            {
                Debug.LogWarning($"Couldn't update sprite for dice {diceData.ID}. Missing sprite renderer or config sprite - enjoy your blank dice.");
            }
        }

        Debug.Log($"Randomized {diceDataList.Count} dice from a pool of {diceConfigPool.Count} configs. Good luck figuring out what abilities you have.");
    }

    private void Update()
    {
        UpdateMoneyUI();
    }

    public void ShowDiceDescription(int diceIndex)
    {
        if (diceIndex < 0 || diceIndex >= diceDataList.Count) return;

        DiceData die = diceDataList[diceIndex];

        if (nameObject != null)
            nameObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? die.diceConfig.diceName : "Unknown Dice";

        if (descObject != null)
            descObject.GetComponent<TMP_Text>().text = die.diceConfig != null ? die.diceConfig.description : "No description";
    }

    public void HideDiceDescription()
    {
        if (nameObject != null)
            nameObject.GetComponent<TMP_Text>().text = "";

        if (descObject != null)
            descObject.GetComponent<TMP_Text>().text = "";
    }

    void UpdateMoneyUI()
    {
        if (MoneyText != null && PlayerData.Instance != null)
        {
            MoneyText.text = $"Money: {PlayerData.Instance.money}";
        }
    }

    public void DiceClicked(int diceIndex)
    {
        if (isRolling) return;

        DiceData hitData = diceDataList[diceIndex];

        if (setAsideDice[diceIndex] || diceMoving[diceIndex]) return;

        selectedDice[diceIndex] = !selectedDice[diceIndex];
        StartCoroutine(MoveDiceToPosition(diceIndex));
        CalculateScore(selectedDice);
    }

    private IEnumerator MoveDiceToPosition(int diceIndex)
    {
        diceMoving[diceIndex] = true;
        GameObject die = diceObjects[diceIndex];
        Vector3 currentPos = die.transform.position;
        Vector3 targetPos = currentPos;

        if (selectedDice[diceIndex])
        {
            targetPos.y = 1f;
        }
        else
        {
            targetPos.y = 0f;
        }

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            die.transform.position = Vector3.Lerp(currentPos, targetPos, t);
            yield return null;
        }

        die.transform.position = targetPos;
        diceMoving[diceIndex] = false;
    }

    public void ToggleSelectedDice(int DiceID)
    {
        if (isRolling || diceMoving[DiceID] || setAsideDice[DiceID]) return;
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

    public void StartNewTurn()
    {
        if (isRolling) return;
        StartCoroutine(StartNewTurnAsync());
    }

    private IEnumerator StartNewTurnAsync()
    {
        yield return StartCoroutine(abilityProcessor.ProcessAbilitiesAsync(TriggerType.OnTurnStart));
        setAsideScore = 0;
        selectedScore = 0;
        ResetAllDice();
        RollDice(true);
    }

    private IEnumerator DebugRollCoroutine()
    {
        isRolling = true;

        for (int i = 0; i < diceDataList.Count; i++)
        {
            if (!setAsideDice[i])
            {
                diceObjects[i].transform.position = new Vector3(diceObjects[i].transform.position.x, 0, 0);
            }
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

    public void RollDice(bool guaranteeValid = false)
    {
        if (isRolling) return;
        StartCoroutine(RollSpecificDice(guaranteeValid));
    }

    private IEnumerator RollSpecificDice(bool guaranteeValid = false)
    {
        isRolling = true;

        bool validRoll = false;
        int attempts = 0;
        int maxAttempts = guaranteeValid ? 1000 : 1;

        while (!validRoll && attempts < maxAttempts)
        {
            for (int i = 0; i < diceDataList.Count; i++)
            {
                if (!setAsideDice[i])
                {
                    selectedDice[i] = false;
                    diceObjects[i].transform.position = new Vector3(diceObjects[i].transform.position.x, 0, 0);
                    int pip = Random.Range(0, 6);
                    diceDataList[i].ChangePipNow(pip);
                }
            }
            if (guaranteeValid)
            {
                validRoll = HasValidScore();
                attempts++;
            }
            else
            {
                validRoll = true;
            }
        }


        for (int i = 0; i < diceDataList.Count; i++)
        {
            if (!setAsideDice[i])
            {
                diceDataList[i].ChangePip(diceDataList[i].currentFace);
            }
        }

        while (diceDataList.Any(d => d.rolling))
        {
            yield return null;
        }

        yield return StartCoroutine(abilityProcessor.ProcessAbilitiesAsync(TriggerType.OnRoll));

        isRolling = false;

        if (!HasValidScore())
        {
            Debug.Log("FARKLE INCOMING...");
            yield return new WaitForSeconds(2f);
            yield return StartCoroutine(BustAsync());

            if (lives > 0)
            {
                yield return new WaitForSeconds(0.5f);
                StartNewTurn();
            }
            yield return StartCoroutine(abilityProcessor.ProcessAbilitiesAsync(TriggerType.OnTurnEnd));
        }
    }

    public void SetAside()
    {
        if (isRolling) return;

        RemoveNonScoringDice();

        if (selectedScore == 0)
        {
            Debug.Log("No valid scoring dice selected!");
            return;
        }

        StartCoroutine(SetAsideDiceCoroutine());
    }

    private void RemoveNonScoringDice()
    {
        if (selectedScore == 0) return;

        List<int> contributingDice = new List<int>();

        for (int i = 0; i < selectedDice.Count; i++)
        {
            if (selectedDice[i] && !setAsideDice[i])
            {
                selectedDice[i] = false;
                int scoreWithout = CalculateTestScore(selectedDice);
                selectedDice[i] = true;

                if (scoreWithout < selectedScore)
                {
                    contributingDice.Add(i);
                }
            }
        }

        for (int i = 0; i < selectedDice.Count; i++)
        {
            if (selectedDice[i] && !setAsideDice[i] && !contributingDice.Contains(i))
            {
                selectedDice[i] = false;
                StartCoroutine(MoveDiceToPosition(i));
            }
        }

        CalculateScore(selectedDice);
    }

    private int CalculateTestScore(List<bool> ToCheck)
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

        if (selectedPips.Count == 0) return 0;

        return CalculateBestScore(pipCounts, selectedPips);
    }

    private IEnumerator SetAsideDiceCoroutine()
    {
        setAsideScore += selectedScore;
        selectedScore = 0;

        List<int> newGroup = new List<int>();
        int groupScore = 0;

        for (int i = 0; i < selectedDice.Count; i++)
        {
            if (selectedDice[i])
            {
                setAsideDice[i] = true;
                selectedDice[i] = false;
                newGroup.Add(i);
            }
        }

        if (newGroup.Count > 0)
        {
            setAsideGroups.Add(newGroup);
            Dictionary<int, int> pipCounts = new Dictionary<int, int>();
            List<int> pips = new List<int>();
            foreach (int diceIndex in newGroup)
            {
                int value = diceDataList[diceIndex].pips[diceDataList[diceIndex].currentFace];
                pips.Add(value);
                if (pipCounts.ContainsKey(value))
                    pipCounts[value]++;
                else
                    pipCounts[value] = 1;
            }
            groupScore = CalculateBestScore(pipCounts, pips);
            setAsideGroupScores.Add(groupScore);

            StartCoroutine(MoveDiceToSetAsideGroup(newGroup, setAsideGroups.Count - 1));
        }
        yield return StartCoroutine(abilityProcessor.ProcessAbilitiesAsync(TriggerType.OnSetAside, -1, newGroup));

        yield return new WaitForSeconds(moveDuration);

        RepositionActiveDice();
        UpdateScoreUI();

        if (setAsideDice.All(d => d))
        {
            Debug.Log("Hot dice! All 6 set aside, banking score and resetting");
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(abilityProcessor.ProcessAbilitiesAsync(TriggerType.OnHotDice));
            totalScore += setAsideScore;
            setAsideScore = 0;
            selectedScore = 0;
            ResetAllDice();
            UpdateScoreUI();

            yield return new WaitForSeconds(0.5f);
            RollDice(true);
        }
        else
        {
            RollDice();
        }
    }

    private IEnumerator MoveDiceToSetAsideGroup(List<int> group, int groupIndex)
    {
        Vector3 basePosition = setAsideStartPosition - new Vector3(0, groupIndex * setAsideGroupSpacing, 0);

        for (int i = 0; i < group.Count; i++)
        {
            int diceIndex = group[i];
            GameObject die = diceObjects[diceIndex];
            Vector3 startPos = die.transform.position;
            Vector3 targetPos = basePosition + new Vector3(i * setAsideDiceSpacing, 0, 0);

            Image sr = die.GetComponent<Image>();
            Color startColor = sr.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0.6f);

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;
                die.transform.position = Vector3.Lerp(startPos, targetPos, t);
                sr.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            die.transform.position = targetPos;
            sr.color = targetColor;
        }
    }

    private void RepositionActiveDice()
    {
        List<int> activeDiceIndices = new List<int>();
        for (int i = 0; i < setAsideDice.Count; i++)
        {
            if (!setAsideDice[i])
            {
                activeDiceIndices.Add(i);
            }
        }

        int count = activeDiceIndices.Count;
        float startX = activeDiceCenter.x - (count - 1) * activeDiceSpacing / 2f;

        for (int i = 0; i < activeDiceIndices.Count; i++)
        {
            int diceIndex = activeDiceIndices[i];
            float yPos = selectedDice[diceIndex] ? 1f : 0f;
            Vector3 targetPos = new Vector3(startX + i * activeDiceSpacing, activeDiceCenter.y + yPos, activeDiceCenter.z);
            StartCoroutine(SmoothMoveToPosition(diceIndex, targetPos));
        }
    }

    private IEnumerator SmoothMoveToPosition(int diceIndex, Vector3 targetPos)
    {
        GameObject die = diceObjects[diceIndex];
        Vector3 startPos = die.transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            die.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        die.transform.position = targetPos;
    }

    private void ResetAllDice()
    {
        setAsideGroups.Clear();
        setAsideGroupScores.Clear();

        for (int i = 0; i < setAsideDice.Count; i++)
        {
            setAsideDice[i] = false;
            selectedDice[i] = false;
            diceObjects[i].GetComponent<Image>().color = Color.white;
        }

        RepositionActiveDice();
    }

    public void BankScore()
    {
        if (isRolling) return;

        RemoveNonScoringDice();

        if (setAsideScore == 0 && selectedScore == 0)
        {
            Debug.Log("Nothing to bank!");
            return;
        }

        StartCoroutine(BankScoreCoroutine());
    }

    private IEnumerator BankScoreCoroutine()
    {
        bool isHotDice = false;

        if (selectedScore > 0)
        {
            int selectedCount = selectedDice.Count(s => s);
            int alreadySetAsideCount = setAsideDice.Count(s => s);

            if (selectedCount + alreadySetAsideCount == 6)
            {
                isHotDice = true;
            }

            yield return StartCoroutine(SetAsideDiceForBanking());
        }

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(abilityProcessor.ProcessAbilitiesAsync(TriggerType.OnBank));

        totalScore += setAsideScore;
        setAsideScore = 0;
        selectedScore = 0;
        ResetAllDice();
        UpdateScoreUI();

        if (!isHotDice)
        {
            lives -= 1;
            UpdateScoreUI();

            if (lives <= 0)
            {
                Game.SetActive(false);
                Dead.SetActive(true);
                yield break;
            }

            yield return StartCoroutine(abilityProcessor.ProcessAbilitiesAsync(TriggerType.OnTurnEnd));
        }

        yield return new WaitForSeconds(0.5f);
        RollDice(true);
    }

    private IEnumerator SetAsideDiceForBanking()
    {
        setAsideScore += selectedScore;
        selectedScore = 0;

        List<int> newGroup = new List<int>();

        for (int i = 0; i < selectedDice.Count; i++)
        {
            if (selectedDice[i])
            {
                setAsideDice[i] = true;
                selectedDice[i] = false;
                newGroup.Add(i);
            }
        }

        if (newGroup.Count > 0)
        {
            setAsideGroups.Add(newGroup);
            Dictionary<int, int> pipCounts = new Dictionary<int, int>();
            List<int> pips = new List<int>();
            foreach (int diceIndex in newGroup)
            {
                int value = diceDataList[diceIndex].pips[diceDataList[diceIndex].currentFace];
                pips.Add(value);
                if (pipCounts.ContainsKey(value))
                    pipCounts[value]++;
                else
                    pipCounts[value] = 1;
            }
            int groupScore = CalculateBestScore(pipCounts, pips);
            setAsideGroupScores.Add(groupScore);

            StartCoroutine(MoveDiceToSetAsideGroup(newGroup, setAsideGroups.Count - 1));
        }

        yield return new WaitForSeconds(moveDuration);

        RepositionActiveDice();
        UpdateScoreUI();
    }

    private IEnumerator BankAndReroll()
    {
        yield return new WaitForSeconds(0.5f);
        RollDice();
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
            selectedScore = 0;
            UpdateScoreUI();
            return;
        }

        selectedScore = CalculateBestScore(pipCounts, selectedPips);
        UpdateScoreUI();
        Debug.Log("Selected Score: " + selectedScore);
    }

    private int CalculateBestScore(Dictionary<int, int> pipCounts, List<int> allPips)
    {
        int maxScore = 0;

        if (allPips.Count == 6 && pipCounts.Count == 6 &&
            pipCounts.All(kvp => kvp.Key >= 1 && kvp.Key <= 6 && kvp.Value == 1))
        {
            return 5000;
        }

        if (allPips.Count >= 5)
        {
            bool has1to5 = Enumerable.Range(1, 5).All(i => pipCounts.ContainsKey(i) && pipCounts[i] >= 1);
            bool has2to6 = Enumerable.Range(2, 5).All(i => pipCounts.ContainsKey(i) && pipCounts[i] >= 1);

            if (has1to5 || has2to6)
            {
                int straightScore = 2500;

                Dictionary<int, int> leftoverCounts = new Dictionary<int, int>(pipCounts);

                if (has1to5)
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        leftoverCounts[i]--;
                        if (leftoverCounts[i] == 0)
                            leftoverCounts.Remove(i);
                    }
                }
                else
                {
                    for (int i = 2; i <= 6; i++)
                    {
                        leftoverCounts[i]--;
                        if (leftoverCounts[i] == 0)
                            leftoverCounts.Remove(i);
                    }
                }

                int leftoverScore = CalculateComboScore(leftoverCounts);
                maxScore = Mathf.Max(maxScore, straightScore + leftoverScore);
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

    public void UpdateScoreUI()
    {
        int runningScore = setAsideScore + selectedScore;
        RunningScoreText.text = "Running: " + runningScore;
        if (TotalScoreText != null)
            TotalScoreText.text = "Total: " + totalScore;
        if (LivesText != null)
            LivesText.text = "Lives: " + lives;
    }

    public void Bust()
    {
        StartCoroutine(BustAsync());
    }

    private IEnumerator BustAsync()
    {
        Debug.Log("FARKLE! Lost running total.");
        yield return StartCoroutine(abilityProcessor.ProcessAbilitiesAsync(TriggerType.OnFarkle));
        setAsideScore = 0;
        selectedScore = 0;
        ResetAllDice();
        lives -= 1;
        UpdateScoreUI();

        if (lives <= 0)
        {
            Game.SetActive(false);
            Dead.SetActive(true);
        }
    }

    public void Restart()
    {
        RandomizeAllDiceConfigs();

        lives = 3;
        totalScore = 0;
        setAsideScore = 0;
        selectedScore = 0;

        setAsideGroups.Clear();
        setAsideGroupScores.Clear();

        for (int i = 0; i < setAsideDice.Count; i++)
        {
            setAsideDice[i] = false;
            selectedDice[i] = false;
            diceMoving[i] = false;
            diceObjects[i].GetComponent<Image>().color = Color.white;
        }

        Game.SetActive(true);
        Dead.SetActive(false);

        RepositionActiveDice();
        UpdateScoreUI();

        StartNewTurn();
    }

    private void InitializeAbilitySystem()
    {
        abilityProcessor = new AbilityProcessor(this);
    }

    public int GetSetAsideGroupCount()
    {
        return setAsideGroups.Count;
    }

    public void OnSpace(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Time.timeScale = 10f;
        }
        else if (context.canceled)
        {
            Time.timeScale = 1f;
        }
    }
}