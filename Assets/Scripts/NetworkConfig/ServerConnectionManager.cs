using System;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using System.Collections.Generic;

public class ServerConnectionManager : MonoBehaviourPunCallbacks
{
    // --- SINGLETON ---
    public static ServerConnectionManager Instance;

    // --- EVENTOS (El puente hacia la UI) ---

    // 1. Evento para enviar la lista de regiones cuando Photon la recibe.
    public Action<List<Region>> OnRegionsUpdate;
    // 2. Evento para avisar que el proceso de conexión a una región específica inició.
    public Action OnStartRegionConnection;
    // Indica si el usuario ya hizo clic en un botón de región
    public bool HasUserSelectedRegion { get; private set; } = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- PASO 1: CONEXIÓN AL NAME SERVER ---

    /// Inicia la conexión sin región fija para obtener la lista de regiones disponibles.
    public void ConnectToNameServer()
    {
        Debug.Log("1. Conectando al Name Server...");

        // Al iniciar, asumimos que el usuario aún no elige nada.
        HasUserSelectedRegion = false;

        // IMPORTANTE: Limpiamos la región fija. 
        // Si esto tiene un valor (ej: "us"), Photon saltará la lista y conectará directo.
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = string.Empty;
        // Esto hace que si el Master carga el nivel 'Mapa1', todos carguen 'Mapa1'
        PhotonNetwork.AutomaticallySyncScene = true;
        // Conecta usando la configuración del AppID
        PhotonNetwork.ConnectUsingSettings();
    }

    // --- PASO 2: RECEPCIÓN DE DATA (Callback) ---

    /// Callback automático de Photon cuando recibe la lista de regiones del Name Server.
    public override void OnRegionListReceived(RegionHandler regionHandler)
    {
        Debug.Log($"2. Lista recibida. Se encontraron {regionHandler.EnabledRegions.Count} regiones.");

        // Verificamos si alguien (la UI) está escuchando este evento
        if (OnRegionsUpdate != null)
        {
            // Enviamos la lista pura de regiones a la UI
            OnRegionsUpdate.Invoke(regionHandler.EnabledRegions);
        }
    }

    // --- PASO 3: SELECCIÓN Y CONEXIÓN FINAL ---

    /// Método público para que el botón de la UI le diga a este Manager a dónde conectar.
    public void ConnectToSpecificRegion(string regionCode)
    {
        Debug.Log($"3. Usuario seleccionó conectar a: {regionCode}");

        HasUserSelectedRegion = true;

        // --- NUEVO: DISPARAR EVENTO DE INTERFAZ ---
        // Avisamos a los suscriptores (la UI) que ya se eligió una región.
        // Esto permite que el panel se cierre solo.
        if (OnStartRegionConnection != null) OnStartRegionConnection.Invoke();

        // Si ya estábamos conectados (en el Name Server), necesitamos desconectar
        // para iniciar una conexión limpia hacia el Master Server de la región elegida.
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();

        // Conecta directamente al Master Server de la región especificada
        PhotonNetwork.ConnectToRegion(regionCode);
    }

    // --- CALLBACKS DE ESTADO ---

    public override void OnConnectedToMaster()
    {
        Debug.Log($"4. ¡CONEXIÓN EXITOSA! Estamos en el Master Server de: {PhotonNetwork.CloudRegion}");
        Debug.Log("Listo para que LobbyRoomManager tome el control (Crear/Unir salas).");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Desconectado de Photon. Razón: {cause}");
    }
}