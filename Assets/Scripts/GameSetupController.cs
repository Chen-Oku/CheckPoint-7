using Photon.Pun;
using UnityEngine;
using System.Collections;

// Cambiamos a MonoBehaviourPunCallbacks para escuchar eventos
public class GameSetupController : MonoBehaviourPunCallbacks
{
    [SerializeField] private string playerPrefabName = "Player";

    private void Start()
    {
        // VERIFICACIÓN DE SEGURIDAD
        // Si intentas dar Play directo a esta escena sin pasar por el Menú, esto evitará errores.
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Scene Cargada. Instanciando Jugador...");
            SpawnPlayer();
            StartCoroutine(PingLoop());
        }
        else
        {
            // Esperando a que el jugador entre en una sala
            Debug.LogWarning("Esperando a que el jugador entre en una sala...");
        }
    }

    // Este evento se activará si entramos a la escena antes de que la conexión termine de sincronizar
    public override void OnJoinedRoom()
    {
        Debug.Log("GameSetup: OnJoinedRoom disparado tardíamente. Instanciando ahora.");
        SpawnPlayer();
        StartCoroutine(PingLoop());
    }

    private void SpawnPlayer()
    {
        // Evitar doble spawn si se llama dos veces por accidente
        if (GameObject.Find(playerPrefabName + "(Clone)") != null) return;

        Vector3 randomPosition = new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
        PhotonNetwork.Instantiate(playerPrefabName, randomPosition, Quaternion.identity);

        Debug.Log("Jugador instanciado correctamente.");
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