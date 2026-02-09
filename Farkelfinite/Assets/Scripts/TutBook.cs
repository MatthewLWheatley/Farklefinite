using System.Collections.Generic;
using UnityEngine;

public class TutBook : MonoBehaviour
{
    public List<GameObject> objects = new List<GameObject>();
    public int currentIndex = 0;

    private void Start()
    {
        for (int i = 0; i < objects.Count; i++)
        {
            objects[i].SetActive(i == 0);
        }
    }

    public void Next()
    {
        if (objects.Count == 0) return;

        objects[currentIndex].SetActive(false);
        currentIndex = (currentIndex + 1) % objects.Count;
        objects[currentIndex].SetActive(true);
    }

    public void Previous()
    {
        if (objects.Count == 0) return;

        objects[currentIndex].SetActive(false);
        currentIndex--;
        if (currentIndex < 0) currentIndex = objects.Count - 1;
        objects[currentIndex].SetActive(true);
    }
}