using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class DiceData : MonoBehaviour
{

    public List<Sprite> DiceSprites = new List<Sprite>();
    public List<string> DiceNames = new List<string>();
    public List<GameObject> pipSprites = new List<GameObject>();
    public List<int> pips = new List<int>();

    SpriteRenderer spriteRenderer;

    public int currentFace = 0;
    public GameObject currentPip;

    public float swapSpeed = 0.1f;
    public int swapRounds = 3;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = DiceSprites[0];
        for (int i = 1; i <= 6; i++) pips.Add(i);
        for(int i = 0; i < DiceNames.Count; i++) 
        {
            DiceNames[i] = DiceNames[i].ToLower();
        }
        currentPip = Instantiate(pipSprites[0], transform.position, Quaternion.identity, transform);
    }

    public bool ChangeSprite(string diceName) 
    {
        diceName = diceName.ToLower();
        if (diceName.Contains(diceName)) 
        { 
            int index = DiceNames.IndexOf(diceName);
            spriteRenderer.sprite = DiceSprites[index];
            return true;
        }
        return false;
    }

    public bool ChangeSprite(int diceID)
    {
        if (DiceNames.Count < diceID)
        {
            spriteRenderer.sprite = DiceSprites[diceID];
            return true;
        }
        return false;
    }

    void Update()
    {

    }

    [ContextMenu("Debug Test")]
    void debugtest() 
    { 
        ChangePip(Random.Range(1, 7));
    }

    void ChangePip(int Face) 
    { 
        int pastace = currentFace;
        currentFace = Face;
        Coroutine coroutine = StartCoroutine(SwapToFace(Face, pastace));
    }

    IEnumerator SwapToFace(int Face, int Start)
    {
        for (int i = 0; i < swapRounds; i++) 
        {
            while (Start != 6) 
            {
                Start++;
                if (Start > 6) Start = 1;
                DestroyImmediate(currentPip);
                currentPip = Instantiate(pipSprites[0], transform.position, Quaternion.identity, transform);
                yield return new WaitForSeconds(swapSpeed);
            }
            Start = 1;
        }

        yield return new WaitForSeconds(0.0f);
    }
}
