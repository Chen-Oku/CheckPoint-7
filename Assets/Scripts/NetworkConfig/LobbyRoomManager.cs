using System;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using System.Collections.Generic;

public class LobbyRoomManager : MonoBehaviourPunCallbacks
{
    public static LobbyRoomManager Instance;

    // EVENTOS
    public Action OnLobbyReady;
    public Action OnJoinedGameRoom;
    public Action<List<RoomInfo>> OnRoomListUpdateEvent;

    [Header("Scene UI")]
    [SerializeField] private SceneWaitingScreen sceneWaitingScreen;

    // CACHÉ DE SALAS
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // --- CONEXIÓN ---

    public override void OnConnectedToMaster()
    {
        // Si el usuario NO ha seleccionado región todavía, ignoramos esta conexión.
        // Esto evita que el panel de Lobby salte automáticamente.
        if (!ServerConnectionManager.Instance.HasUserSelectedRegion)
        {
            Debug.Log("Conexión automática al Master detectada. Esperando selección manual de región...");
            return;
        }

        Debug.Log("Conexión legítima al Master. Entrando al Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Entramos al Lobby. Esperando lista de salas...");
        cachedRoomList.Clear();
        if (OnLobbyReady != null) OnLobbyReady.Invoke();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList || !info.IsVisible || !info.IsOpen)
            {
                if (cachedRoomList.ContainsKey(info.Name)) cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }

        List<RoomInfo> finalRoomList = new List<RoomInfo>(cachedRoomList.Values);
        if (OnRoomListUpdateEvent != null) OnRoomListUpdateEvent.Invoke(finalRoomList);
    }

    // --- CREAR / UNIR ---

    public void CreateGameRoom(string roomName, int maxPlayers)
    {
        if (string.IsNullOrEmpty(roomName)) return;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)Mathf.Clamp(maxPlayers, 2, 255),
            IsVisible = true,
            IsOpen = true,
            EmptyRoomTtl = 0
        };

        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void JoinGameRoom(string roomName)
    {
        // PROTECCIÓN: Si ya estamos uniéndonos, no hacer nada.
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joining) return;

        // Solo llamamos a Photon. NO cargamos escena aquí.
        PhotonNetwork.JoinRoom(roomName);
    }

    // --- ENTRADA A SALA Y CAMBIO DE ESCENA ---

    public override void OnJoinedRoom()
    {
        Debug.Log($"¡Dentro de la sala: {PhotonNetwork.CurrentRoom.Name}!");

        // Avisamos a la UI para que oculte el panel del Lobby
        if (OnJoinedGameRoom != null) OnJoinedGameRoom.Invoke();

        // No cargamos la escena aquí al entrar: el MasterClient iniciará la partida
        // cuando la sala esté completa (ver OnPlayerEnteredRoom).
        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int capacity = PhotonNetwork.CurrentRoom.MaxPlayers;
        Debug.Log($"Sala MaxPlayers={capacity}, PlayerCount={current}");

        // Mostrar pantalla de espera si la sala no está completa (panel por escena)
        if (sceneWaitingScreen != null)
        {
            if (current < capacity) sceneWaitingScreen.Show("Esperando que se unan más jugadores...");
            else sceneWaitingScreen.Hide();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        // Solo el MasterClient decide cuándo iniciar la partida
        if (!PhotonNetwork.IsMasterClient) return;

        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int capacity = PhotonNetwork.CurrentRoom.MaxPlayers;

        Debug.Log($"OnPlayerEnteredRoom: now {current}/{capacity}");

        if (current >= capacity)
        {
            Debug.Log("Room completa — MasterClient cargando escena de juego");
            sceneWaitingScreen?.Show("Sala completa — iniciando partida...");
            PhotonNetwork.LoadLevel("02_GameScene");
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message) => Debug.LogError("Error Crear: " + message);
    public override void OnJoinRoomFailed(short returnCode, string message) => Debug.LogError("Error Unir: " + message);
}