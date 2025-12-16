using TMPro;
using UnityEngine;
using Photon.Realtime;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Panel Principal")]
    [SerializeField] private GameObject lobbyPanelRoot;

    [Header("Panel Izquierdo (Crear)")]
    [SerializeField] private TMP_InputField roomNameInput;

    [Header("Panel Derecho (Lista)")]
    [SerializeField] private Transform roomListContainer; // El Content del ScrollView
    [SerializeField] private RoomItem roomItemPrefab;     // Prefab del botón de sala
    [SerializeField] private GameObject noRoomsMessage;   // Texto opcional "No hay salas"

    private void Start()
    {
        lobbyPanelRoot.SetActive(false); // Empezamos ocultos

        // Suscripciones
        LobbyRoomManager.Instance.OnLobbyReady += ShowLobby;
        LobbyRoomManager.Instance.OnJoinedGameRoom += HideLobby;
        LobbyRoomManager.Instance.OnRoomListUpdateEvent += UpdateRoomListUI;
    }

    private void OnDestroy()
    {
        if (LobbyRoomManager.Instance != null)
        {
            LobbyRoomManager.Instance.OnLobbyReady -= ShowLobby;
            LobbyRoomManager.Instance.OnJoinedGameRoom -= HideLobby;
            LobbyRoomManager.Instance.OnRoomListUpdateEvent -= UpdateRoomListUI;
        }
    }

    // --- MÉTODOS DE BOTONES (Asignar en Inspector) ---

    // Asignar al botón "Crear Sala" del Panel Izquierdo
    public void OnClickCreateRoom()
    {
        string name = roomNameInput.text;
        if (!string.IsNullOrEmpty(name))
        {
            LobbyRoomManager.Instance.CreateGameRoom(name);
        }
    }

    // --- VISUALIZACIÓN ---

    private void ShowLobby() => lobbyPanelRoot.SetActive(true);
    private void HideLobby() => lobbyPanelRoot.SetActive(false);

    private void UpdateRoomListUI(List<RoomInfo> roomList)
    {
        // 1. Limpiar lista visual actual
        foreach (Transform child in roomListContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Verificar si hay salas
        if (roomList.Count == 0)
        {
            if (noRoomsMessage != null) noRoomsMessage.SetActive(true);
            return;
        }

        if (noRoomsMessage != null) noRoomsMessage.SetActive(false);

        // 3. Crear botones para cada sala
        foreach (RoomInfo info in roomList)
        {
            RoomItem newItem = Instantiate(roomItemPrefab, roomListContainer);
            newItem.Setup(info);

            // Asignar listener del botón
            // (Aunque el propio RoomItem ya llama al manager, podemos gestionarlo allí)
            newItem.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(newItem.OnClickItem);
        }
    }
}