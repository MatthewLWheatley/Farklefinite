using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class DiceData : MonoBehaviour
{
    public int ID;
    public DiceConfig diceConfig;

    public List<GameObject> pipSprites = new List<GameObject>();
    public List<int> pips = new List<int>();

    SpriteRenderer spriteRenderer;
    public int currentFace = 0;
    public GameObject currentPip;
    public float swapSpeed = 0.1f;
    public int swapRounds = 5;

    [SerializeField] private AnimationCurve speedCurve;
    [SerializeField] private float fastSpeed = 0.1f;
    [SerializeField] private float slowSpeed = 0.65f;


    public bool rolling = false;

    void Awake()
    {
        if (diceConfig != null && diceConfig.customPips.Count > 0)
        {
            pips = new List<int>(diceConfig.customPips);
        }
        else
        {
            for (int i = 1; i <= 6; i++)
                pips.Add(i);
        }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (diceConfig != null)
        {
            spriteRenderer.sprite = diceConfig.diceSprite;
            pipSprites = diceConfig.pipSprites;
        }
    }

    public bool CanChangeFace()
    {
        return diceConfig != null && diceConfig.canChangeFaces;
    }

    public void SetFaceManually(int face)
    {
        if (!CanChangeFace()) return;
        if (face < 0 || face >= pips.Count) return;

        currentFace = face;
        ChangePipNow(face);
    }

    [ContextMenu("Debug Test")]
    void debugtest()
    {
        ChangePip(Random.Range(0, 6));
    }

    public void ChangePip(int Face)
    {
        if (rolling)
        {
            Debug.Log("no im already rolling");
            return;
        }
        rolling = true;
        int pastFace = currentFace;
        currentFace = Face;
        Debug.Log("Changing pip to face " + Face);
        Debug.Log("Past face was " + pastFace);
        StartCoroutine(SwapToFace(Face, pastFace));
    }

    public void ChangePipNow(int Face)
    {
        int pastFace = currentFace;
        currentFace = Face;
        Debug.Log("Changing pip to face " + Face);
        Debug.Log("Past face was " + pastFace);

        if (currentPip != null)
            DestroyImmediate(currentPip);
        currentPip = Instantiate(pipSprites[pips[Face] - 1], transform.position, Quaternion.identity, transform);
    }

    IEnumerator SwapToFace(int Face, int Start)
    {
        int current = Start;
        int diff = ((Face + 6) - Start) % 6;
        int totalSteps = (swapRounds * 6) + diff;

        for (int step = 0; step < totalSteps; step++)
        {
            current++;
            if (current >= 6) current = 0;

            if (currentPip != null)
                DestroyImmediate(currentPip);
            currentPip = Instantiate(pipSprites[pips[current] - 1], transform.position, Quaternion.identity, transform);

            float t = (float)step / totalSteps;
            float curveValue = speedCurve.Evaluate(t);
            float currentSpeed = Mathf.Lerp(fastSpeed, slowSpeed, curveValue);

            yield return new WaitForSeconds(currentSpeed);
        }

        if (currentPip != null)
            DestroyImmediate(currentPip);
        currentPip = Instantiate(pipSprites[pips[current] - 1], transform.position, Quaternion.identity, transform);

        rolling = false;
    }

    private void OnMouseDown()
    {
        Debug.Log("Sprite Clicked" + ID.ToString());
    }
}