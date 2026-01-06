using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelector : MonoBehaviour
{
    public enum Bag
    {
        DiceBag,        //1
        ChestBag,       //2
        ReusableBag,    //3
        SecondLifeBag,  //4
        BeggersBag,     //5
        ApexDiceBag,    //6
        CavemansSack,   //7
        PaperBag,       //8
        WetPaperBag,    //9
        Bag,            //10
        PerkyPurse,     //11
        DiceceptionBag  //12
    }

    public string[] bagNames = new string[]
    {
        "Dice Bag",
        "Chest Bag",
        "Reusable Bag",
        "Second Life Bag",
        "Begger's Bag",
        "Apex Dice Bag",
        "Caveman's Sack",
        "Paper Bag",
        "Wet Paper Bag",
        "Bag?",
        "Perky Purse",
        "Diceception Bag"
    };

    public string[] bagAbilityDescriptions = new string[]
    {
        "Just your plain old game.",
        "You gain +1 gold per bank.",
        "If 3 or more set aside groups when banking then multiply banked score by 1.5.",
        "You gain 1 life per Round.",
        "Dice only have 1, 2 and 3 pip faces.",
        "Scores are multiplied by 1.5 when banked but must be above 1500 to bank.",
        "Banking will add 50 to banked score per unused dice.",
        "You only have 1 Life but all scores are multipled by 2 when banked.",
        "You only have 1 Life.",
        "You only have 1 Life but all scores are multipled by 0.5 when banked.",
        "Dice only have 4, 5 and 6 pip faces",
        "When scoring a set of 3 or more, add one dice to the calculation."
    };

    public string[] bagVisualDescriptions = new string[]
    {
        "The classic dice bag made of leather.",
        "A sturdy chest to hold your dice.",
        "A reusable bag made from canvas.",
        "A plastic bag that was granted a second chance.",
        "Your jean pocket.",
        "A bag made of the finest dinosour leather.",
        "A primitive sack made of straw.",
        "A simple paper bag.",
        "A soggy paper bag, handle with care.",
        "A pile of water and paper???",
        "A fashionable purse.",
        "A bag that looks like a dice."
    };

    public Texture[] bagSprites;

    public List<GameObject> bagSpriteObjects = new List<GameObject>();
    public GameObject bagSpritePrefab;

    public int[] unlockedBags = new int[] { (int)Bag.DiceBag };

    public Bag selectedBag = Bag.DiceBag;
    public GameObject bagNameText;
    public GameObject bagAbilityDescriptionText;
    public GameObject bagVisualDescriptionText;

    public float bagScrollSpeed = 0.5f;

    public Coroutine playingCourtine;

    void Start()
    {
        // just make it a string of bits where 1 is unlocked and 0 is locked
        string defualt = "1".PadRight(System.Enum.GetNames(typeof(Bag)).Length, '0');
        string unlockedBagsString = PlayerPrefs.GetString("_unlockedBags", defualt);
        unlockedBags = new int[unlockedBagsString.Length];
        for (int i = 0; i < unlockedBagsString.Length; i++)
        {
            unlockedBags[i] = (unlockedBagsString[i] == '1') ? 1 : 0;
        }

        //create a visual representation of the bags
        // create a object for each bag
        // move them to the side, out of the screen
        // i want to make it so they scroll in and out of the screen from the side 
        for (int i = 0; i < System.Enum.GetNames(typeof(Bag)).Length; i++)
        {
            GameObject bagObj = Instantiate(bagSpritePrefab, transform);
            bagObj.GetComponent<RectTransform>().localScale = new Vector3(4.0f, 4.0f, 4.0f);
            bagObj.GetComponent<RawImage>().texture = bagSprites[i];
            float offset = this.GetComponent<RectTransform>().rect.width / 2 + bagSpritePrefab.GetComponent<RectTransform>().rect.width*2;
            bagObj.GetComponent<RectTransform>().anchoredPosition = new Vector2((i - (int)selectedBag) * offset, 0);
            if (unlockedBags[i] == 0)
            {
                bagObj.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.3f);
            }
            bagSpriteObjects.Add(bagObj);
        }

        //update the UI to show the selected bag
        bagNameText.GetComponent<TMP_Text>().text = bagNames[(int)selectedBag];
        bagAbilityDescriptionText.GetComponent<TMP_Text>().text = bagAbilityDescriptions[(int)selectedBag];
        bagVisualDescriptionText.GetComponent<TMP_Text>().text = bagVisualDescriptions[(int)selectedBag];
        PlayerPrefs.SetString("_unlockedBags", unlockedBagsString);
    }

    void Update()
    {
        
    }

    public void OpenSettings() 
    { 
        Setting.Instance.OpenSettingsMenu();
    }

    public void PlayGame() 
    { 
        //load game scene

    }

    public void nextBag()
    {
        if ((int)selectedBag == unlockedBags.Length - 1 || playingCourtine != null)
        {
            return;
        }
        float offset = this.GetComponent<RectTransform>().rect.width / 2 + bagSpritePrefab.GetComponent<RectTransform>().rect.width * 2;
        //incriment bag start a short coroutine that moves all bags left by the offset amount
        playingCourtine = StartCoroutine(MoveBag((int)offset));
        selectedBag = (Bag)(((int)selectedBag + 1) % System.Enum.GetNames(typeof(Bag)).Length);
        //update the UI to show the selected bag
        bagNameText.GetComponent<TMP_Text>().text = bagNames[(int)selectedBag];
        bagAbilityDescriptionText.GetComponent<TMP_Text>().text = bagAbilityDescriptions[(int)selectedBag];
        bagVisualDescriptionText.GetComponent<TMP_Text>().text = bagVisualDescriptions[(int)selectedBag];
    }

    IEnumerator MoveBag(int offset) 
    { 
        float elapsedTime = 0;
        float duration = bagScrollSpeed;
        Vector2[] startingPositions = new Vector2[bagSpriteObjects.Count];
        for (int i = 0; i < bagSpriteObjects.Count; i++)
        {
            startingPositions[i] = bagSpriteObjects[i].GetComponent<RectTransform>().anchoredPosition;
        }
        
        while (elapsedTime < duration)
        {
            for (int i = 0; i < bagSpriteObjects.Count; i++)
            {
                Vector2 newPos = Vector2.Lerp(startingPositions[i], startingPositions[i] + new Vector2(-offset, 0), (elapsedTime / duration));
                bagSpriteObjects[i].GetComponent<RectTransform>().anchoredPosition = newPos;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < bagSpriteObjects.Count; i++)
        {
            bagSpriteObjects[i].GetComponent<RectTransform>().anchoredPosition = startingPositions[i] + new Vector2(-offset, 0);
        }
        playingCourtine = null;
    }

    public void prevBag() 
    {
        if ((int)selectedBag == 0 || playingCourtine != null)
        {
            return;
        }
        float offset = this.GetComponent<RectTransform>().rect.width / 2 + bagSpritePrefab.GetComponent<RectTransform>().rect.width * 2;
        offset = -offset;
        //incriment bag start a short coroutine that moves all bags left by the offset amount
        playingCourtine = StartCoroutine(MoveBag((int)offset));
        selectedBag = (Bag)(((int)selectedBag - 1) % System.Enum.GetNames(typeof(Bag)).Length);
        //update the UI to show the selected bag
        bagNameText.GetComponent<TMP_Text>().text = bagNames[(int)selectedBag];
        bagAbilityDescriptionText.GetComponent<TMP_Text>().text = bagAbilityDescriptions[(int)selectedBag];
        bagVisualDescriptionText.GetComponent<TMP_Text>().text = bagVisualDescriptions[(int)selectedBag];
    }
}
