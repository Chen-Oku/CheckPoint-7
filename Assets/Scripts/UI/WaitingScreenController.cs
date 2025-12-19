using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaitingScreenController : MonoBehaviour
{
    public static WaitingScreenController Instance;

    [Header("Assign a GameObject (panel) that contains the waiting image/text")]
    [SerializeField] private GameObject waitingScreen;
    [SerializeField] private Text uiText;
    [SerializeField] private TMP_Text tmpText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (waitingScreen != null) waitingScreen.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Show(string message = null)
    {
        if (waitingScreen == null)
        {
            var found = GameObject.Find("WaitingScreen");
            if (found != null) waitingScreen = found;
        }

        if (waitingScreen == null)
        {
            Debug.LogWarning("WaitingScreenController: waitingScreen not assigned and no GameObject named 'WaitingScreen' found.");
            return;
        }

        if (!string.IsNullOrEmpty(message))
        {
            if (tmpText != null) tmpText.text = message;
            else if (uiText != null) uiText.text = message;
        }

        waitingScreen.SetActive(true);
    }

    public void Hide()
    {
        if (waitingScreen != null) waitingScreen.SetActive(false);
    }
}
