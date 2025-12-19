using Photon.Pun;
using UnityEngine;
using System.Collections;

// Cambiamos a MonoBehaviourPunCallbacks para escuchar eventos
public class GameSetupController : MonoBehaviourPunCallbacks
{
    [SerializeField] private string playerPrefabName = "Player";
    [Header("Scene UI")]
    [SerializeField] private SceneWaitingScreen sceneWaitingScreen;

    private void Start()
    {
        // Si ya estamos en la sala y Photon listo, esperamos al maze antes de spawnear
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Scene Cargada. Esperando que el Maze esté listo antes de instanciar jugador...");
            sceneWaitingScreen?.Show("Esperando que el laberinto esté listo...");
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
        sceneWaitingScreen?.Show("Esperando que el laberinto esté listo...");
        WaitAndSpawnWhenReady();
        StartCoroutine(PingLoop());
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
    }

    private void SpawnPlayer()
    {
        // Evitar doble spawn si se llama dos veces por accidente
        if (GameObject.Find(playerPrefabName + "(Clone)") != null) return;

        Vector3 randomPosition = new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
        PhotonNetwork.Instantiate(playerPrefabName, randomPosition, Quaternion.identity);

        Debug.Log("Jugador instanciado correctamente.");
        sceneWaitingScreen?.Hide();
        // Si usamos PlayerSpawnMarker, este marcará la propiedad 'spawned' automáticamente.
        // Nos desuscribimos por limpieza
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