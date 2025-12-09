using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class DiceData : MonoBehaviour
{
    public int ID;

    public List<Sprite> DiceSprites = new List<Sprite>();
    public List<string> DiceNames = new List<string>();
    public List<GameObject> pipSprites = new List<GameObject>();
    // pips will be changeable later so this will allow for e.g. 3 1s, 2 5s and 1 6
    public List<int> pips = new List<int>();

    SpriteRenderer spriteRenderer;
    public int currentFace = 0;
    public GameObject currentPip;
    public float swapSpeed = 0.1f;
    public int swapRounds = 5;

    [SerializeField] private AnimationCurve speedCurve;
    [SerializeField] private float fastSpeed = 0.1f; 
    [SerializeField] private float slowSpeed = 0.65f;

    public BoxCollider2D collider;

    public bool rolling = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = DiceSprites[0];

        for (int i = 1; i <= 6; i++)
            pips.Add(i);

        for (int i = 0; i < DiceNames.Count; i++)
        {
            DiceNames[i] = DiceNames[i].ToLower();
        }
        collider = GetComponent<BoxCollider2D>();
        currentPip = Instantiate(pipSprites[0], transform.position, Quaternion.identity, transform);
    }

    public bool ChangeSprite(string diceName)
    {
        diceName = diceName.ToLower();
        if (DiceNames.Contains(diceName))
        {
            int index = DiceNames.IndexOf(diceName);
            spriteRenderer.sprite = DiceSprites[index];
            return true;
        }
        return false;
    }

    public bool ChangeSprite(int diceID)
    {
        if (diceID >= 0 && diceID < DiceSprites.Count)
        {
            spriteRenderer.sprite = DiceSprites[diceID];
            return true;
        }
        return false;
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
        currentPip = Instantiate(pipSprites[pips[Face]-1], transform.position, Quaternion.identity, transform);
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