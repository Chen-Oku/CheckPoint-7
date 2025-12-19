using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class WaitingScreenController : MonoBehaviour
{
    public static WaitingScreenController Instance;

    [Header("Assign a GameObject (panel) that contains the waiting image/text")]
    [SerializeField] private GameObject waitingScreen;
    [SerializeField] private Text uiText;
    [SerializeField] private TMP_Text tmpText;

    private GameObject createdFallback;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // If a waitingScreen was assigned in-editor, ensure it's parented under this persistent root
            if (waitingScreen != null)
            {
                waitingScreen.transform.SetParent(this.transform, false);
                waitingScreen.SetActive(false);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If we lost the reference to waitingScreen (e.g., different scene), try to find one in the new scene and reparent it
        if (waitingScreen == null)
        {
            var found = GameObject.Find("WaitingScreen");
            if (found != null)
            {
                waitingScreen = found;
                waitingScreen.transform.SetParent(this.transform, false);
                waitingScreen.SetActive(false);
                // try to find text components
                tmpText = waitingScreen.GetComponentInChildren<TMP_Text>();
                if (tmpText == null) uiText = waitingScreen.GetComponentInChildren<Text>();
            }
        }
    }

    public void Show(string message = null)
    {
        // If the assigned panel was destroyed during scene change, create a fallback persistent panel
        if (waitingScreen == null || waitingScreen.Equals(null))
        {
            if (createdFallback == null)
            {
                CreateFallbackWaitingScreen();
            }
            waitingScreen = createdFallback;
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
        if (waitingScreen != null && !waitingScreen.Equals(null)) waitingScreen.SetActive(false);
    }

    private void CreateFallbackWaitingScreen()
    {
        createdFallback = new GameObject("WaitingScreen");
        createdFallback.transform.SetParent(this.transform, false);

        var canvas = createdFallback.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        createdFallback.AddComponent<CanvasScaler>();
        createdFallback.AddComponent<GraphicRaycaster>();

        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(createdFallback.transform, false);
        var img = panelObj.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        var rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject textObj = null;
        TMP_Text textComp = null;
        Text textCompLegacy = null;

        // Try to create TMP text if TMP is available
        try
        {
            textObj = new GameObject("WaitingText");
            textObj.transform.SetParent(panelObj.transform, false);
            textComp = textObj.AddComponent<TMP_Text>();
        }
        catch
        {
            textObj = new GameObject("WaitingText");
            textObj.transform.SetParent(panelObj.transform, false);
            textCompLegacy = textObj.AddComponent<Text>();
            textCompLegacy.alignment = TextAnchor.MiddleCenter;
            textCompLegacy.color = Color.white;
            textCompLegacy.fontSize = 24;
        }

        var tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.1f, 0.45f);
        tr.anchorMax = new Vector2(0.9f, 0.55f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        if (textComp != null)
        {
            textComp.text = "Cargando...";
            tmpText = textComp;
        }
        else
        {
            textCompLegacy.text = "Cargando...";
            uiText = textCompLegacy;
        }

        createdFallback.SetActive(false);
    }
}
