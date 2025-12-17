using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class NickNameField : MonoBehaviour
{
    public InputField IdInput; // o TMP_InputField si tu demo usa TMP

    void OnEnable()
    {
        if (IdInput == null)
        {
            Debug.LogWarning("NickNameField: IdInput no asignado. Desactivando comportamiento de este componente.");
            enabled = false;
            return;
        }

        if (!string.IsNullOrEmpty(PhotonNetwork.NickName))
            IdInput.text = PhotonNetwork.NickName;
        else
            IdInput.text = "Player" + Random.Range(1000, 9999);
    }

    void Update()
    {
        if (IdInput == null) return;

        string current = IdInput.text ?? "";
        if (current != PhotonNetwork.NickName)
            PhotonNetwork.NickName = current;
    }
}