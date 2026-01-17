using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public GameObject PlayButton;
    public GameObject SettingsButton;
    public GameObject QuitButton;

    void Start()
    {
        PlayButton.GetComponent<Button>().onClick.AddListener(() => LoadGameScene());
        SettingsButton.GetComponent<Button>().onClick.AddListener(() => Setting.Instance.OpenSettingsMenu());
        QuitButton.GetComponent<Button>().onClick.AddListener(() => QUITTheGame());
    }

    public void QUITTheGame() 
    { 
        //should do some clean up but nahhhhhhh
        Debug.Log("QUIT!");
        Application.Quit();
    }

    public void LoadGameScene()
    {
        Debug.Log("LOAD!");
        //implement scene loading here
        // if i can be bothered
        SceneManager.LoadScene("PlayScene");
    }
}
