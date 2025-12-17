using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class PlayerNameInput : MonoBehaviour
{
    public InputField nameField; // legacy UI (opcional)
    public TMP_InputField tmpNameField; // TextMeshPro (opcional)
    private const string prefKey = "playerName";

    void Start()
    {
        string saved = PlayerPrefs.GetString(prefKey, "Player" + Random.Range(1000, 9999));
        if (tmpNameField != null) tmpNameField.text = saved;
        if (nameField != null) nameField.text = saved;
    }

    // Para InputField (sin parámetro)
    public void OnNameEndEdit()
    {
        string name = GetCurrentInputText();
        CommitName(name);
    }

    // Para TMP_InputField (OnEndEdit pasa string)
    public void OnNameEndEdit(string input)
    {
        string name = string.IsNullOrWhiteSpace(input) ? GetCurrentInputText() : input.Trim();
        CommitName(name);
    }

    private string GetCurrentInputText()
    {
        if (tmpNameField != null) return tmpNameField.text.Trim();
        if (nameField != null) return nameField.text.Trim();
        return "Player" + Random.Range(1000, 9999);
    }

    private void CommitName(string name)
    {
        if (string.IsNullOrEmpty(name)) name = "Player" + Random.Range(1000, 9999);
        PlayerPrefs.SetString(prefKey, name);
        PhotonNetwork.NickName = name; // debe estar antes de conectar/entrar en sala
    }
}
