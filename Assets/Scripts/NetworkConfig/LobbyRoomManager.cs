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

    public void CreateGameRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName)) return;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 4,
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

        // Cargamos la escena del juego.
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Soy Master Client. Cargando escena de juego...");
            // Asegúrate que "GameScene" sea el nombre EXACTO de tu archivo de escena
            PhotonNetwork.LoadLevel("02_GameScene");
        }

        // Los clientes no hacen nada, Photon los sincroniza automáticamente 
        // porque activamos AutomaticallySyncScene en el Manager anterior.
    }

    public override void OnCreateRoomFailed(short returnCode, string message) => Debug.LogError("Error Crear: " + message);
    public override void OnJoinRoomFailed(short returnCode, string message) => Debug.LogError("Error Unir: " + message);
}