using UnityEngine;
using TMPro;

public class SceneWaitingScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private string defaultMessage = "Cargando...";

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string msg = null)
    {
        if (tmpText != null)
        {
            tmpText.text = string.IsNullOrEmpty(msg) ? defaultMessage : msg;
        }

        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}