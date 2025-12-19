using Photon.Pun;
using UnityEngine;
using System.Collections;

// Cambiamos a MonoBehaviourPunCallbacks para escuchar eventos
public class GameSetupController : MonoBehaviourPunCallbacks
{
    [SerializeField] private string playerPrefabName = "Player";
    [Header("UI")]
    [SerializeField] private GameObject loadingScreen; // assign an image/canvas that says "Loading"

    // Track the local player's instantiated GameObject so we can avoid duplicates and clean up on disconnect
    private GameObject localPlayerInstance;

    private void Start()
    {
        // Si ya estamos en la sala y Photon listo, esperamos al maze antes de spawnear
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Scene Cargada. Esperando que el Maze esté listo antes de instanciar jugador...");

            if (loadingScreen != null)
            {
                // Mostrar pantalla de carga mientras esperamos al maze / spawn
                loadingScreen.SetActive(!MazeGenerator.MazeIsGenerated);
            }

            WaitAndSpawnWhenReady();
            StartCoroutine(PingLoop());
        }
        else
        {
            Debug.LogWarning("Esperando a que el jugador entre en una sala...");
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("GameSetup: OnJoinedRoom disparado tardíamente. Esperando que Maze esté listo antes de instanciar.");
        if (loadingScreen != null) loadingScreen.SetActive(!MazeGenerator.MazeIsGenerated);
        WaitAndSpawnWhenReady();
        StartCoroutine(PingLoop());
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        // Limpiar cualquier instancia local que quede
        if (localPlayerInstance != null)
        {
            // Si es una instancia Photon (tiene PhotonView), pedir su destrucción de red
            var pv = localPlayerInstance.GetComponent<Photon.Pun.PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Photon.Pun.PhotonNetwork.Destroy(localPlayerInstance);
            }
            else
            {
                Destroy(localPlayerInstance);
            }
            localPlayerInstance = null;
        }
        MazeGenerator.OnMazeGenerated -= SpawnPlayer;
        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        // Asegurarnos de limpiar referencias locales también al desconectar
        if (localPlayerInstance != null)
        {
            Destroy(localPlayerInstance);
            localPlayerInstance = null;
        }
        MazeGenerator.OnMazeGenerated -= SpawnPlayer;
        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    void WaitAndSpawnWhenReady()
    {
        // Si el maze ya está generado, spawneamos de inmediato
        if (MazeGenerator.MazeIsGenerated)
        {
            SpawnPlayer();
            return;
        }

        // Si no, suscribirnos al evento y spawnear cuando llegue
        MazeGenerator.OnMazeGenerated -= SpawnPlayer; // defensivo: evitar múltiples subs
        MazeGenerator.OnMazeGenerated += SpawnPlayer;

        if (loadingScreen != null) loadingScreen.SetActive(true);
    }

    private void SpawnPlayer()
    {
        Debug.Log($"SpawnPlayer called. MazeIsGenerated={MazeGenerator.MazeIsGenerated}, localPlayerInstance={(localPlayerInstance==null?"null":localPlayerInstance.name)}, InRoom={PhotonNetwork.InRoom}, IsConnectedAndReady={PhotonNetwork.IsConnectedAndReady}");
        // Evitar doble spawn si ya tenemos la instancia local
        if (localPlayerInstance != null)
        {
            // Si la instancia local sigue, no hacemos nada
            return;
        }

        // También asegurarnos de que no exista ya un PhotonView 'IsMine' (por seguridad)
        var existingOwned = UnityEngine.Object.FindObjectsByType<Photon.Pun.PhotonView>(UnityEngine.FindObjectsSortMode.None);
        foreach (var pv in existingOwned)
        {
            if (!pv.IsMine) continue;

            // Filtrar solo si parece nuestro prefab de jugador (evitar tomar cualquier PV que sea IsMine por otra razón)
            string goName = pv.gameObject.name != null ? pv.gameObject.name : string.Empty;
            if (goName.StartsWith(playerPrefabName))
            {
                Debug.Log($"SpawnPlayer: found existing owned player object: {goName}");
                localPlayerInstance = pv.gameObject;
                return;
            }
            else
            {
                Debug.Log($"SpawnPlayer: ignoring owned PhotonView '{goName}' (not player prefab)");
            }
        }

        Vector3 randomPosition = new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
        var go = PhotonNetwork.Instantiate(playerPrefabName, randomPosition, Quaternion.identity);
        localPlayerInstance = go;

        Debug.Log("Jugador instanciado correctamente.");
        // Si usamos PlayerSpawnMarker, este marcará la propiedad 'spawned' automáticamente.
        // Nos desuscribimos por limpieza
        MazeGenerator.OnMazeGenerated -= SpawnPlayer;

        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    private void OnDestroy()
    {
        MazeGenerator.OnMazeGenerated -= SpawnPlayer;
    }

    private IEnumerator PingLoop()
    {
        // Este bucle se ejecuta mientras sigamos conectados y en la sala
        while (PhotonNetwork.InRoom)
        {
            yield return new WaitForSeconds(2f); // Actualiza cada 2 segundos para no saturar la consola
            Debug.Log($"Ping: {PhotonNetwork.GetPing()} ms");
        }
    }
}