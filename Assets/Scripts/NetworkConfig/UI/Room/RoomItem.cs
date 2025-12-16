using TMPro;
using UnityEngine;
using Photon.Realtime; // Necesario para 'RoomInfo'

public class RoomItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;

    private RoomInfo info;

    public void Setup(RoomInfo roomInfo)
    {
        info = roomInfo;
        roomNameText.text = roomInfo.Name;
        // Ej: "2/4" Jugadores
        playerCountText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
    }

    public void OnClickItem()
    {
        // Al hacer clic, le decimos al Manager que nos una a ESTA sala
        LobbyRoomManager.Instance.JoinGameRoom(info.Name);
    }
}