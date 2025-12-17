using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameplate : MonoBehaviourPun
{
    public TMP_Text nameTextTMP;
    public Text nameTextLegacy;

    void Start()
    {
        string nick = (photonView != null && photonView.Owner != null) ? photonView.Owner.NickName : "Unknown";

        if (nameTextTMP != null) nameTextTMP.text = nick;
        else if (nameTextLegacy != null) nameTextLegacy.text = nick;
    }

    // método público opcional para actualizar el texto desde otro script si hace falta
    public void SetName(string nick)
    {
        if (string.IsNullOrEmpty(nick)) nick = "Unknown";
        if (nameTextTMP != null) nameTextTMP.text = nick;
        else if (nameTextLegacy != null) nameTextLegacy.text = nick;
    }
}
