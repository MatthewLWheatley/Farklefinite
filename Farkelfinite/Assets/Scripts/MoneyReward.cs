using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MoneyReward : MonoBehaviour
{
    public GameObject moneyCanvas;
    public Button closeButton;
    public int moneyAmount = 10;

    void Start()
    {
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.AddMoney(moneyAmount);
            Debug.Log($"free money time. got {moneyAmount} coins like a chump");
        }

        if (moneyCanvas != null)
        {
            moneyCanvas.SetActive(true);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseAndReturnToMap);
        }
    }

    public void CloseAndReturnToMap()
    {
        if (moneyCanvas != null)
        {
            moneyCanvas.SetActive(false);
        }

        MapGenerator mapController = FindFirstObjectByType<MapGenerator>();
        if (mapController != null)
        {
            for (int i = 0; i < mapController.transform.childCount; i++)
            {
                mapController.transform.GetChild(i).gameObject.SetActive(true);
            }

            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Map"));
        }

        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
    }
}